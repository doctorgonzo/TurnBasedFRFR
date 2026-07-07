using System.Collections.Generic;
using UnityEngine;

// Collects every UnitDef into one asset the gameplay scripts can look up by UnitType.
// Create via Assets > Create > TurnBased > Unit Database, then drag your UnitDef assets
// into the list. Assign this single asset on the clicker component in the inspector.
[CreateAssetMenu(fileName = "Units", menuName = "TurnBased/Unit Database")]
public class UnitDatabase : ScriptableObject
{
    [Tooltip("One entry per unit type. Duplicates or gaps are reported in the console.")]
    [SerializeField] List<UnitDef> units = new List<UnitDef>();

    // Built on first access (and rebuilt if the asset changes in the editor).
    Dictionary<UnitType, UnitDef> byType;

    public UnitDef Get(UnitType type)
    {
        if (byType == null) Rebuild();
        return byType.TryGetValue(type, out UnitDef def) ? def : null;
    }

    public IReadOnlyList<UnitDef> All => units;

    void Rebuild()
    {
        byType = new Dictionary<UnitType, UnitDef>();
        foreach (UnitDef def in units)
        {
            if (def == null) continue;
            if (byType.ContainsKey(def.unitType))
            {
                Debug.LogWarning($"UnitDatabase '{name}' has two entries for {def.unitType}; " +
                                 $"'{def.name}' is ignored.");
                continue;
            }
            byType[def.unitType] = def;
        }
    }

    // Editor edits invalidate the cache so the next Get() picks up changes.
    void OnValidate() => byType = null;
}
