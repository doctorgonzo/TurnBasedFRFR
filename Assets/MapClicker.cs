using Mirror;
using UnityEngine;

// Lives on the scene-root Logix object (one instance per client), so it handles pick-up
// clicks for whichever player is local on this machine. It must NOT use its serialized
// Clicker reference for per-player state — that points at the scene clicker, which never
// becomes a local player — so all cursor/turn state goes through clicker.Local instead.
public class MapClicker : NetworkBehaviour
{
    [SerializeField] Turns turns;
    [SerializeField] clicker Clicker;

    private void Update()
    {
        clicker local = clicker.Local;
        if (local == null) return; // local player not spawned yet
        if (!Input.GetMouseButtonDown(0)) return;
        // Pick-up only applies with an empty hand; with something selected, clicker places.
        if (local.selectedCursor != CursorType.Default) return;

        RaycastHit2D hit = Physics2D.Raycast(Input.mousePosition, Vector3.forward);
        if (hit.collider == null || !hit.collider.CompareTag("tile")) return;

        tile target = hit.collider.GetComponent<tile>();
        if (target == null || target.occupyingUnit == UnitType.None) return;
        // Only your own units, and only on your turn (the server re-checks both).
        if (target.owningPlayer != local.LocalPlayerNumber || !local.IsMyTurn()) return;
        // A unit placed this turn is locked in until next turn.
        if (turns != null && target.placedOnTurn == turns.TurnIndex) return;

        CmdPickUpUnit(target);
    }

    [Command(requiresAuthority = false)]
    void CmdPickUpUnit(tile target, NetworkConnectionToClient sender = null)
    {
        if (target == null || target.occupyingUnit == UnitType.None) return;
        int senderPlayer = clicker.PlayerNumberOf(sender);
        if (turns != null && (turns.isPlayer1Turn ? 1 : 2) != senderPlayer)
        {
            Debug.LogWarning($"Rejected off-turn pick-up from player {senderPlayer}.");
            return;
        }
        if (target.owningPlayer != senderPlayer)
        {
            Debug.LogWarning($"Rejected pick-up of a unit player {senderPlayer} doesn't own.");
            return;
        }
        if (turns != null && target.placedOnTurn == turns.TurnIndex)
        {
            Debug.LogWarning($"Rejected pick-up: unit was placed this turn.");
            return;
        }

        // Remember what was here before clearing, so the right stock gets the refund
        // (this used to always credit infantry, whatever the unit was).
        UnitType unit = target.occupyingUnit;
        target.occupyingUnit = UnitType.None; // SyncVar hooks restore the tile color everywhere
        target.owningPlayer = 0;

        // Refund goes to the clicker owned by whoever picked the unit up, not the scene one.
        clicker senderClicker = sender != null && sender.identity != null
            ? sender.identity.GetComponentInChildren<clicker>()
            : null;
        if (senderClicker != null) senderClicker.ServerRefund(unit);

        if (sender != null) TargetUnitPickedUp(sender, unit);
    }

    // Runs only on the client that picked the unit up: the unit goes "into their hand".
    [TargetRpc]
    void TargetUnitPickedUp(NetworkConnectionToClient target, UnitType unit)
    {
        if (clicker.Local != null) clicker.Local.SelectCursorFor(unit);
    }
}
