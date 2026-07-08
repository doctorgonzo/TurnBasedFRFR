using System;
using System.Collections.Generic;
using UnityEngine;

// Server-side game rules. Plain C# (no MonoBehaviour, no networking) so it stays easy to
// reason about and unit-test. Every "how far can a unit move / attack" question is answered
// here, driven by the numbers on the UnitDef assets plus the terrain costs below.
//
// Tiles are addressed by their sibling index in the board grid (the same index the placement
// commands already use). The board is a fixed-width grid, so an index maps to (row, col).
public class UnitRules
{
    readonly UnitDatabase units;
    readonly TerrainConfig terrain;
    readonly int boardWidth;

    // Tiny slack so a path that sums to exactly the move budget isn't rejected by float rounding.
    const float CostEpsilon = 1e-4f;

    public UnitRules(UnitDatabase units, TerrainConfig terrain, int boardWidth)
    {
        this.units = units;
        this.terrain = terrain;
        this.boardWidth = Mathf.Max(1, boardWidth);
    }

    public int RowOf(int tileIndex) => tileIndex / boardWidth;
    public int ColOf(int tileIndex) => tileIndex % boardWidth;

    // Orthogonal step count (no diagonals). Used for attack range, which ignores terrain.
    public int ManhattanDistance(int fromIndex, int toIndex) =>
        Mathf.Abs(RowOf(fromIndex) - RowOf(toIndex)) +
        Mathf.Abs(ColOf(fromIndex) - ColOf(toIndex));

    // Movement-point cost to ENTER a tile of this terrain (the tile you leave doesn't charge).
    // A unit's UnitDef.moveRange is its budget of these points per turn. Costs come from the
    // TerrainConfig asset (tune in the inspector); the fallback only applies if none is wired.
    public float EnterCost(LandType land) =>
        terrain != null ? terrain.EnterCost(land) : DefaultEnterCost(land);

    static float DefaultEnterCost(LandType land)
    {
        switch (land)
        {
            case LandType.Road: return 1f / 1.5f;   // roads: 1.5x movement rate -> cheaper to enter
            case LandType.Water: return 1f + 1f;    // water: costs 1 extra move
            case LandType.Mountain: return 1f + 2f; // mountains: cost 2 extra move
            default: return 1f;                     // bare tile: normal cost
        }
    }

    // Cheapest movement cost to reach each tile from `fromIndex`, staying within the unit's
    // move budget. Dijkstra over 4-connected neighbors; occupied tiles are impassable (you
    // can't move through or onto a unit). The origin is not included in the result.
    //   landAt(i)    -> terrain of tile i
    //   isOccupied(i)-> whether tile i currently holds a unit
    public Dictionary<int, float> ReachableTiles(UnitType unit, int fromIndex, int tileCount,
                                                 Func<int, LandType> landAt, Func<int, bool> isOccupied)
    {
        var best = new Dictionary<int, float>();
        UnitDef def = DefOf(unit);
        if (def == null) return best;
        float budget = def.moveRange;

        best[fromIndex] = 0f;
        var frontier = new List<int> { fromIndex };
        while (frontier.Count > 0)
        {
            // Pop the lowest-cost frontier tile (grid is tiny, so a linear scan is fine).
            int pick = 0;
            for (int k = 1; k < frontier.Count; k++)
                if (best[frontier[k]] < best[frontier[pick]]) pick = k;
            int current = frontier[pick];
            frontier.RemoveAt(pick);
            float currentCost = best[current];

            foreach (int n in Neighbors(current, tileCount))
            {
                if (isOccupied(n)) continue;                 // can't path through a unit
                float next = currentCost + EnterCost(landAt(n));
                if (next > budget + CostEpsilon) continue;   // out of range
                if (best.TryGetValue(n, out float prev) && prev <= next) continue;
                best[n] = next;
                frontier.Add(n);
            }
        }
        best.Remove(fromIndex); // the tile you're standing on isn't a move destination
        return best;
    }

    // Can this unit hit `targetIndex` from `fromIndex`? Attack range ignores terrain.
    public bool CanAttack(UnitType unit, int fromIndex, int targetIndex)
    {
        UnitDef def = DefOf(unit);
        return def != null && ManhattanDistance(fromIndex, targetIndex) <= def.attackRange;
    }

    public int StartingCount(UnitType unit) => DefOf(unit)?.startingCount ?? 0;
    public int AttackDamage(UnitType unit) => DefOf(unit)?.attackDamage ?? 0;
    public int MaxHealth(UnitType unit) => DefOf(unit)?.maxHealth ?? 0;

    // The up-to-4 orthogonal neighbors of a tile that stay on the board.
    IEnumerable<int> Neighbors(int index, int tileCount)
    {
        int col = index % boardWidth;
        if (col > 0) yield return index - 1;                       // left
        if (col < boardWidth - 1) yield return index + 1;         // right
        if (index - boardWidth >= 0) yield return index - boardWidth;      // up
        if (index + boardWidth < tileCount) yield return index + boardWidth; // down
    }

    UnitDef DefOf(UnitType unit) => units != null ? units.Get(unit) : null;
}
