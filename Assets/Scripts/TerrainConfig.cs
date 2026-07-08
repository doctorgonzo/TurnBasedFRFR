using UnityEngine;

// Movement-cost tuning for each terrain type, editable in the inspector. Create via
// Assets > Create > TurnBased > Terrain Config and assign it on the clicker component
// (next to the Unit Database). A unit's UnitDef.moveRange is a budget of movement points,
// and entering a tile spends the cost below for that tile's terrain.
[CreateAssetMenu(fileName = "TerrainConfig", menuName = "TurnBased/Terrain Config")]
public class TerrainConfig : ScriptableObject
{
    [Min(0f), Tooltip("Cost to enter a bare tile with no terrain placed.")]
    public float bareCost = 1f;

    [Min(0f), Tooltip("Cost to enter a road tile. Lower = faster; 0.667 gives ~1.5x range.")]
    public float roadCost = 1f / 1.5f;

    [Min(0f), Tooltip("Cost to enter a water tile. 2 = one extra move point vs. bare.")]
    public float waterCost = 2f;

    [Min(0f), Tooltip("Cost to enter a mountain tile. 3 = two extra move points vs. bare.")]
    public float mountainCost = 3f;

    // Movement-point cost to enter a tile of the given terrain.
    public float EnterCost(LandType land)
    {
        switch (land)
        {
            case LandType.Road: return roadCost;
            case LandType.Water: return waterCost;
            case LandType.Mountain: return mountainCost;
            default: return bareCost;
        }
    }
}
