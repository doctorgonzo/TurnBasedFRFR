using UnityEngine;

// Server-side game rules. Plain C# (no MonoBehaviour, no networking) so it stays easy to
// reason about and unit-test. Every "how far can a unit move / attack" question is answered
// here, driven entirely by the numbers on the UnitDef assets — so you tune balance in the
// inspector, not in code.
//
// Tiles are addressed by their sibling index in the board grid (the same index the placement
// commands already use). The board is a fixed-width grid, so an index maps to (row, col).
public class UnitRules
{
    readonly UnitDatabase units;
    readonly int boardWidth;

    public UnitRules(UnitDatabase units, int boardWidth)
    {
        this.units = units;
        this.boardWidth = Mathf.Max(1, boardWidth);
    }

    public int RowOf(int tileIndex) => tileIndex / boardWidth;
    public int ColOf(int tileIndex) => tileIndex % boardWidth;

    // Orthogonal step count (no diagonals). Switch callers to ChebyshevDistance if you decide
    // diagonal moves should cost a single step instead.
    public int ManhattanDistance(int fromIndex, int toIndex) =>
        Mathf.Abs(RowOf(fromIndex) - RowOf(toIndex)) +
        Mathf.Abs(ColOf(fromIndex) - ColOf(toIndex));

    public int ChebyshevDistance(int fromIndex, int toIndex) =>
        Mathf.Max(Mathf.Abs(RowOf(fromIndex) - RowOf(toIndex)),
                  Mathf.Abs(ColOf(fromIndex) - ColOf(toIndex)));

    // Can this unit reach `toIndex` from `fromIndex` in one turn? Distance-only for now;
    // this is the spot to add terrain cost / blocked-tile checks as the board rules grow.
    public bool CanMove(UnitType unit, int fromIndex, int toIndex)
    {
        UnitDef def = DefOf(unit);
        return def != null && ManhattanDistance(fromIndex, toIndex) <= def.moveRange;
    }

    // Can this unit hit `targetIndex` from `fromIndex`?
    public bool CanAttack(UnitType unit, int fromIndex, int targetIndex)
    {
        UnitDef def = DefOf(unit);
        return def != null && ManhattanDistance(fromIndex, targetIndex) <= def.attackRange;
    }

    public int StartingCount(UnitType unit) => DefOf(unit)?.startingCount ?? 0;
    public int AttackDamage(UnitType unit) => DefOf(unit)?.attackDamage ?? 0;
    public int MaxHealth(UnitType unit) => DefOf(unit)?.maxHealth ?? 0;

    UnitDef DefOf(UnitType unit) => units != null ? units.Get(unit) : null;
}
