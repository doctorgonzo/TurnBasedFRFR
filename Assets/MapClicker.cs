using System.Collections.Generic;
using Mirror;
using UnityEngine;

// Client-side interaction for units already on the board: select one of your units, then
// click again to move it (to an empty tile in range) or attack (an enemy in range). Lives on
// the scene-root Logix object, one per client, and drives the local player only. All actual
// state changes go through server commands on clicker, which re-validate everything — this
// class just reads synced tile state to decide what a click means and to preview validity.
public class MapClicker : NetworkBehaviour
{
    [SerializeField] Turns turns;

    // The local player's currently selected unit tile (null = nothing selected).
    private tile selectedTile;
    // Empty tiles currently green-outlined as valid move destinations for the selection.
    private readonly List<tile> moveHighlights = new List<tile>();

    private void Update()
    {
        clicker local = clicker.Local;
        if (local == null) return;                       // local player not spawned yet
        if (!Input.GetMouseButtonDown(0)) return;
        // Selection/movement only apply with an empty hand; a palette selection means the
        // player is placing, which clicker handles. Off-turn, nothing is selectable.
        if (local.selectedCursor != CursorType.Default || !local.IsMyTurn()) { Deselect(); return; }

        RaycastHit2D hit = Physics2D.Raycast(Input.mousePosition, Vector3.forward);
        if (hit.collider == null || !hit.collider.CompareTag("tile")) { Deselect(); return; }
        tile clicked = hit.collider.GetComponent<tile>();
        if (clicked == null) { Deselect(); return; }

        int me = local.LocalPlayerNumber;

        // Clicking one of your own units selects it (or deselects if it's already selected).
        if (clicked.occupyingUnit != UnitType.None && clicked.owningPlayer == me)
        {
            if (clicked == selectedTile) Deselect();
            else Select(clicked);
            return;
        }

        // With a unit selected, the next click is a move or an attack. Either way the unit is
        // deselected afterwards; to move AND attack, reselect the unit at its new position.
        if (selectedTile != null)
        {
            TryAct(local, clicked, me);
        }
        Deselect();
    }

    // Issues a move or attack command if the click is a valid action for the selected unit.
    // These checks mirror the server's in clicker; the server is still the authority.
    void TryAct(clicker local, tile clicked, int me)
    {
        UnitRules rules = local.Rules;
        if (rules == null) return;

        int fromIndex = selectedTile.transform.GetSiblingIndex();
        int toIndex = clicked.transform.GetSiblingIndex();
        UnitType unit = selectedTile.occupyingUnit;
        int turn = turns != null ? turns.TurnIndex : 0;

        // A unit placed this turn can't act yet.
        if (selectedTile.placedOnTurn == turn) return;

        // Attack: target is an enemy unit within attack range, and we haven't attacked yet.
        if (clicked.occupyingUnit != UnitType.None && clicked.owningPlayer != me && clicked.owningPlayer != 0)
        {
            if (selectedTile.attackedOnTurn != turn && rules.CanAttack(unit, fromIndex, toIndex))
                local.CmdAttackUnit(fromIndex, toIndex);
            return;
        }

        // Move: target is an empty tile within move range, and we haven't moved yet.
        if (clicked.occupyingUnit == UnitType.None)
        {
            if (selectedTile.movedOnTurn != turn && rules.CanMove(unit, fromIndex, toIndex))
                local.CmdMoveUnit(fromIndex, toIndex);
        }
    }

    void Select(tile t)
    {
        Deselect();
        selectedTile = t;
        t.SetSelected(true);
        ShowMoveRange(t);
    }

    // Green-outlines every empty tile the selected unit could move to — but only when the
    // unit can actually still move this turn (not just placed, hasn't already moved).
    void ShowMoveRange(tile from)
    {
        clicker local = clicker.Local;
        if (local == null || local.Rules == null || local.uiCanvas == null) return;

        int turn = turns != null ? turns.TurnIndex : 0;
        if (from.placedOnTurn == turn || from.movedOnTurn == turn) return; // can't move now

        int fromIndex = from.transform.GetSiblingIndex();
        UnitType unit = from.occupyingUnit;
        int count = local.uiCanvas.transform.childCount;
        for (int i = 0; i < count; i++)
        {
            tile candidate = local.TileAt(i);
            if (candidate == null || candidate == from) continue;
            if (candidate.occupyingUnit != UnitType.None) continue; // occupied
            if (!local.Rules.CanMove(unit, fromIndex, i)) continue;  // out of range
            candidate.SetMoveHighlight(true);
            moveHighlights.Add(candidate);
        }
    }

    void Deselect()
    {
        if (selectedTile != null) selectedTile.SetSelected(false);
        selectedTile = null;
        foreach (tile t in moveHighlights)
            if (t != null) t.SetMoveHighlight(false);
        moveHighlights.Clear();
    }
}
