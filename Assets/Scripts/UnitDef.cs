using UnityEngine;

// One asset per unit type (Infantry / Armor / Machinegun). Create via
// Assets > Create > TurnBased > Unit Definition, then tune the numbers in the inspector.
// These values are the single source of truth for per-unit rules — nothing is hardcoded
// in the gameplay scripts anymore.
[CreateAssetMenu(fileName = "UnitDef", menuName = "TurnBased/Unit Definition")]
public class UnitDef : ScriptableObject
{
    [Tooltip("Which unit these stats apply to. Must be unique within a Unit Database.")]
    public UnitType unitType;

    [Tooltip("Name shown in UI / logs.")]
    public string displayName;

    [Header("Placement")]
    [Min(0), Tooltip("How many of this unit each player starts with.")]
    public int startingCount = 1;

    [Header("Movement")]
    [Min(0), Tooltip("How far the unit can move in one turn, in grid steps.")]
    public int moveRange = 1;

    [Header("Combat")]
    [Min(0), Tooltip("How far away the unit can attack, in grid steps.")]
    public int attackRange = 1;

    [Min(0), Tooltip("Damage dealt per attack.")]
    public int attackDamage = 1;

    [Min(1), Tooltip("Starting / maximum health.")]
    public int maxHealth = 1;
}
