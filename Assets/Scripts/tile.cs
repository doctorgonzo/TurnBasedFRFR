using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class tile : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnLandTypeChanged))]
    public LandType landType;
    [SyncVar(hook = nameof(OnUnitTypeChanged))]
    public UnitType occupyingUnit;
    // 0 = unowned; otherwise the player number of whoever placed the occupying unit.
    [SyncVar]
    public int owningPlayer;

    void OnLandTypeChanged(LandType oldType, LandType newType) => RefreshColor();
    void OnUnitTypeChanged(UnitType oldType, UnitType newType) => RefreshColor();

    // A unit's color takes priority over the land under it; an empty tile is white.
    // Recomputing from both SyncVars (instead of per-hook color switches) means removing a
    // unit correctly restores the land color underneath rather than leaving a stale color.
    void RefreshColor()
    {
        Image image = GetComponent<Image>();
        if (image == null) return;
        image.color = occupyingUnit != UnitType.None
            ? GameTypes.ColorFor(occupyingUnit)
            : GameTypes.ColorFor(landType);
    }
}
