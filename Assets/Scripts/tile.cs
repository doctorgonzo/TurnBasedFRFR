using Mirror;
using TMPro;
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
    // Turns.TurnIndex when the occupying unit was placed (-1 = never). A unit can't act on
    // the turn it was placed.
    [SyncVar]
    public int placedOnTurn = -1;
    // Turns.TurnIndex when the occupying unit last moved / attacked (-1 = not yet this unit).
    // Each is allowed once per turn; the stamp naturally "expires" when the turn advances.
    [SyncVar]
    public int movedOnTurn = -1;
    [SyncVar]
    public int attackedOnTurn = -1;
    // Current health of the occupying unit (0 when the tile is empty). Server sets it from
    // UnitDef.maxHealth on placement; syncs to every client and drives the health label.
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health;

    // A TextMeshPro label centered on the tile showing the unit's health. Created lazily the
    // first time a unit occupies this tile (empty tiles never spawn one), so it costs nothing
    // on the ~half of the board that stays bare.
    TMP_Text healthLabel;

    void OnLandTypeChanged(LandType oldType, LandType newType) => RefreshColor();
    void OnUnitTypeChanged(UnitType oldType, UnitType newType)
    {
        RefreshColor();
        RefreshHealthLabel();
    }
    void OnHealthChanged(int oldHealth, int newHealth) => RefreshHealthLabel();

    // SyncVar hooks are not guaranteed to fire for the values a late-joining client receives
    // on spawn, so apply the current state explicitly here too.
    public override void OnStartClient()
    {
        RefreshColor();
        RefreshHealthLabel();
    }

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

    // Shows the current health centered on the unit, or hides the label when the tile is empty.
    void RefreshHealthLabel()
    {
        bool show = occupyingUnit != UnitType.None && health > 0;
        if (!show)
        {
            if (healthLabel != null) healthLabel.gameObject.SetActive(false);
            return;
        }
        EnsureHealthLabel();
        healthLabel.gameObject.SetActive(true);
        healthLabel.text = health.ToString();
    }

    // Client-only selection highlight: a yellow outline around the tile while it's the
    // player's currently selected unit. Purely visual, driven by MapClicker.
    public void SetSelected(bool selected)
    {
        Outline outline = GetComponent<Outline>();
        if (selected)
        {
            if (outline == null) outline = gameObject.AddComponent<Outline>();
            outline.effectColor = Color.yellow;
            outline.effectDistance = new Vector2(5f, 5f);
            outline.enabled = true;
        }
        else if (outline != null)
        {
            outline.enabled = false;
        }
    }

    // Builds the TMP label as a child stretched over the whole tile, so its centered text
    // sits in the middle of the unit. Uses the project's default TMP font.
    void EnsureHealthLabel()
    {
        if (healthLabel != null) return;

        GameObject go = new GameObject("HealthLabel", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        healthLabel = go.AddComponent<TextMeshProUGUI>();
        healthLabel.alignment = TextAlignmentOptions.Center;
        healthLabel.fontSize = 36;
        healthLabel.fontStyle = FontStyles.Bold;
        healthLabel.color = Color.white;
        healthLabel.raycastTarget = false; // never intercept clicks meant for the tile
        // Thin dark outline so the number stays legible on any unit color.
        healthLabel.outlineWidth = 0.15f;
        healthLabel.outlineColor = new Color32(0, 0, 0, 255);
    }
}
