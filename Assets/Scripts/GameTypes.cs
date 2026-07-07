using UnityEngine;

// Shared game-state types. These used to be magic strings ("grass", "infantry", "default")
// scattered across clicker/tile/MapClicker; enums sync over Mirror just as well and can't typo.
public enum LandType : byte { None, Grass, Water, Mountain }

public enum UnitType : byte { None, Infantry, Armor, Machinegun }

// What the player currently has "in hand" (drives the mouse cursor and what a tile click does).
public enum CursorType : byte { Default, Grass, Water, Mountain, Infantry, Armor, Machinegun }

public static class GameTypes
{
    // Single source of truth for tile colors — previously duplicated between
    // clicker.ClickedTile and the tile SyncVar hooks, which could drift apart.
    public static Color ColorFor(LandType land)
    {
        switch (land)
        {
            case LandType.Grass: return Color.green;
            case LandType.Water: return Color.blue;
            case LandType.Mountain: return new Color(150f / 255f, 75f / 255f, 0f);
            default: return Color.white;
        }
    }

    public static Color ColorFor(UnitType unit)
    {
        switch (unit)
        {
            case UnitType.Infantry: return Color.red;
            case UnitType.Armor: return Color.gray;
            case UnitType.Machinegun: return new Color(1f, 0.64f, 0f);
            default: return Color.white;
        }
    }

    public static LandType LandFor(CursorType cursor)
    {
        switch (cursor)
        {
            case CursorType.Grass: return LandType.Grass;
            case CursorType.Water: return LandType.Water;
            case CursorType.Mountain: return LandType.Mountain;
            default: return LandType.None;
        }
    }

    public static UnitType UnitFor(CursorType cursor)
    {
        switch (cursor)
        {
            case CursorType.Infantry: return UnitType.Infantry;
            case CursorType.Armor: return UnitType.Armor;
            case CursorType.Machinegun: return UnitType.Machinegun;
            default: return UnitType.None;
        }
    }
}
