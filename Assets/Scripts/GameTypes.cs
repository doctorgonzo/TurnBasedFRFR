using UnityEngine;

// Shared game-state types. These used to be magic strings ("road", "infantry", "default")
// scattered across clicker/tile/MapClicker; enums sync over Mirror just as well and can't typo.
// Note: values sync as bytes, so keep each member's position (Road stays at 1) to avoid
// remapping already-placed tiles.
public enum LandType : byte { None, Road, Water, Mountain }

public enum UnitType : byte { None, Infantry, Armor, Machinegun }

// What the player currently has "in hand" (drives the mouse cursor and what a tile click does).
public enum CursorType : byte { Default, Road, Water, Mountain, Infantry, Armor, Machinegun }

public static class GameTypes
{
    // Single source of truth for tile colors — previously duplicated between
    // clicker.ClickedTile and the tile SyncVar hooks, which could drift apart.
    public static Color ColorFor(LandType land)
    {
        switch (land)
        {
            case LandType.Road: return Color.green;
            case LandType.Water: return Color.blue;
            case LandType.Mountain: return new Color(150f / 255f, 75f / 255f, 0f);
            default: return Color.white;
        }
    }

    public static Color ColorFor(UnitType unit)
    {
        switch (unit)
        {
            // Matched to the palette square colors in PlayerInfoCanvas.prefab, so a placed
            // unit looks the same on the board as it does in the palette.
            case UnitType.Infantry: return Color.red;
            case UnitType.Armor: return new Color(0.9396226f, 0.6009351f, 0.10459942f);   // orange
            case UnitType.Machinegun: return new Color(0.7129441f, 0.07636161f, 0.735849f); // purple
            default: return Color.white;
        }
    }

    public static LandType LandFor(CursorType cursor)
    {
        switch (cursor)
        {
            case CursorType.Road: return LandType.Road;
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
