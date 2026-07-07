using Mirror;
using TMPro;
using UnityEngine;

public class clicker : NetworkBehaviour
{
    // The clicker instance owned by the local player on this machine, set in
    // OnStartLocalPlayer. The scene-root Logix object also carries a clicker (which never
    // becomes a local player), so scene-level scripts (MapClicker, EndTurnButton) must go
    // through this instead of a serialized reference to be sure they touch the right instance.
    public static clicker Local;

    [SerializeField] Texture2D cursorSprite;
    [SerializeField] Texture2D grassSprite;
    [SerializeField] Texture2D waterSprite;
    [SerializeField] Texture2D mountainSprite;
    [SerializeField] Texture2D infantrySprite;
    [SerializeField] Texture2D armorSprite;
    [SerializeField] Texture2D machinegunSprite;
    [SerializeField] Turns turnsScript;

    [SerializeField] TMP_Text gCount;
    [SerializeField] TMP_Text wCount;
    [SerializeField] TMP_Text mCount;
    [SerializeField] TMP_Text iCount;
    [SerializeField] TMP_Text aCount;
    [SerializeField] TMP_Text mgCount;
    [SerializeField] public GameObject uiCanvas;

    // Stock counts are server-authoritative: only server code changes them (place/pick-up
    // commands), and the SyncVar hook refreshes the owning player's HUD when they arrive.
    [SyncVar(hook = nameof(OnCountChanged))] int grassCount = 9;
    [SyncVar(hook = nameof(OnCountChanged))] int waterCount = 3;
    [SyncVar(hook = nameof(OnCountChanged))] int mountainCount = 3;
    [SyncVar(hook = nameof(OnCountChanged))] public int infantryCount = 2;
    [SyncVar(hook = nameof(OnCountChanged))] public int armorCount = 4;
    [SyncVar(hook = nameof(OnCountChanged))] public int machinegunCount = 6;

    public CursorType selectedCursor = CursorType.Default;
    private Vector2 cursorOffset;

    // ---------------------------------------------------------------- server

    [Command(requiresAuthority = false)]
    public void CmdPlaceLand(int tileIndex, LandType land, NetworkConnectionToClient sender = null)
    {
        // Authoritative checks. Mirror fills in `sender`; a modified client that bypasses
        // the local guards in ClickedTile is still rejected here on the server.
        int senderPlayer = PlayerNumberOf(sender);
        if (!IsTurnOf(senderPlayer))
        {
            Debug.LogWarning($"Rejected off-turn land placement from player {senderPlayer}.");
            return;
        }
        tile target = TileAt(tileIndex);
        if (target == null || land == LandType.None) return;
        // Land can only go on a bare tile (no land and no unit yet).
        if (target.landType != LandType.None || target.occupyingUnit != UnitType.None) return;
        if (GetCount(land) <= 0) return;

        AddCount(land, -1);
        target.landType = land; // SyncVar hook recolors the tile on every client
    }

    [Command(requiresAuthority = false)]
    public void CmdPlaceUnit(int tileIndex, UnitType unit, NetworkConnectionToClient sender = null)
    {
        int senderPlayer = PlayerNumberOf(sender);
        if (!IsTurnOf(senderPlayer))
        {
            Debug.LogWarning($"Rejected off-turn unit placement from player {senderPlayer}.");
            return;
        }
        tile target = TileAt(tileIndex);
        if (target == null || unit == UnitType.None) return;
        if (target.occupyingUnit != UnitType.None) return;
        if (GetCount(unit) <= 0) return;

        AddCount(unit, -1);
        target.occupyingUnit = unit;
        // The unit belongs to whoever actually placed it.
        target.owningPlayer = senderPlayer;
    }

    // Credits a unit back to this player's stock (used by MapClicker's pick-up command).
    [Server]
    public void ServerRefund(UnitType unit) => AddCount(unit, 1);

    // The host owns the local connection and is player 1; any other connection is player 2.
    public static int PlayerNumberOf(NetworkConnectionToClient sender) =>
        (sender == null || sender == NetworkServer.localConnection) ? 1 : 2;

    public bool IsTurnOf(int player)
    {
        if (turnsScript == null) return true; // fail open if unwired (e.g. solo testing)
        return (turnsScript.isPlayer1Turn ? 1 : 2) == player;
    }

    tile TileAt(int tileIndex)
    {
        if (uiCanvas == null || tileIndex < 0 || tileIndex >= uiCanvas.transform.childCount)
        {
            Debug.LogWarning("Invalid tile index received on the server.");
            return null;
        }
        return uiCanvas.transform.GetChild(tileIndex).GetComponent<tile>();
    }

    // ---------------------------------------------------------------- setup

    void Start()
    {
        // uiCanvas is used by the server inside the place commands (it indexes tiles via
        // GetChild), so it must be resolved on every instance, not just the local player.
        // The serialized reference points at PlayerInfoCanvas (the palette), not the tile
        // grid, so resolve it to UICanvas (parent of the 56 tiles) here.
        uiCanvas = GameObject.Find("UICanvas");
        // Turn state lives on the single scene Turns manager (a root object in the scene).
        // The prefab's serialized turnsScript pointed at a per-player Turns nested under this
        // player, which is redundant and doesn't drive the shared HUD's End Turn button.
        // Bind to the scene manager: the only Turns that sits at the scene root.
        foreach (Turns t in FindObjectsOfType<Turns>())
        {
            if (t.transform.parent == null) { turnsScript = t; break; }
        }
    }

    // Runs only on the client that owns this player object. The palette counts and the
    // cursor are per-player, so only the local player wires them up.
    public override void OnStartLocalPlayer()
    {
        Local = this;
        // Resolve each palette count RELATIVE to its palette square, so the number and the
        // square are guaranteed to be on the same canvas. Looking counts up independently by
        // name is unreliable here: the HUD is instantiated in more than one canvas, and
        // GameObject.Find can return the number from one canvas and the square from another.
        gCount = CountUnder("GrassImage", "GrassCount");
        wCount = CountUnder("WaterImage", "WaterCount");
        mCount = CountUnder("MountainImage", "MountainCount");
        // Infantry has its own dedicated number child (InfantryCount), like the land types.
        iCount = CountUnder("InfantryImage", "InfantryCount");
        aCount = CountUnder("ArmorImage", "ArmorCount");
        mgCount = CountUnder("MachinegunImage", "MachinegunCount");
        RefreshCounts();
        // Default cursor is the yellow square, set once when the local player spawns.
        SetCursorTo(CursorType.Default, cursorSprite);
    }

    public override void OnStopLocalPlayer()
    {
        if (Local == this) Local = null;
    }

    // grass/water/mountain show their remaining count in a child object of the palette square.
    static TMP_Text CountUnder(string imageName, string countName)
    {
        GameObject img = GameObject.Find(imageName);
        if (img == null) return null;
        Transform t = img.transform.Find(countName);
        return t != null ? t.GetComponent<TMP_Text>() : img.GetComponentInChildren<TMP_Text>();
    }

    // ---------------------------------------------------------------- counts

    // Shared hook for every count SyncVar: refresh the HUD (instead of doing it every frame
    // in Update) and drop back to the default cursor when the selected type runs out.
    void OnCountChanged(int oldValue, int newValue)
    {
        if (this != Local) return; // only the owning player's HUD shows these counts
        RefreshCounts();
        if (selectedCursor != CursorType.Default && GetCount(selectedCursor) <= 0)
            SetCursorTo(CursorType.Default, cursorSprite);
    }

    // Pushes the current stock numbers to the HUD. Guarded so a missing label can't spam NREs.
    void RefreshCounts()
    {
        SetText(gCount, grassCount);
        SetText(wCount, waterCount);
        SetText(mCount, mountainCount);
        SetText(iCount, infantryCount);
        SetText(aCount, armorCount);
        SetText(mgCount, machinegunCount);
    }

    static void SetText(TMP_Text label, int value)
    {
        if (label != null) label.text = value.ToString();
    }

    public int GetCount(LandType land)
    {
        switch (land)
        {
            case LandType.Grass: return grassCount;
            case LandType.Water: return waterCount;
            case LandType.Mountain: return mountainCount;
            default: return 0;
        }
    }

    public int GetCount(UnitType unit)
    {
        switch (unit)
        {
            case UnitType.Infantry: return infantryCount;
            case UnitType.Armor: return armorCount;
            case UnitType.Machinegun: return machinegunCount;
            default: return 0;
        }
    }

    int GetCount(CursorType cursor)
    {
        LandType land = GameTypes.LandFor(cursor);
        if (land != LandType.None) return GetCount(land);
        UnitType unit = GameTypes.UnitFor(cursor);
        if (unit != UnitType.None) return GetCount(unit);
        return int.MaxValue; // the default cursor never "runs out"
    }

    [Server]
    void AddCount(LandType land, int delta)
    {
        switch (land)
        {
            case LandType.Grass: grassCount += delta; break;
            case LandType.Water: waterCount += delta; break;
            case LandType.Mountain: mountainCount += delta; break;
        }
    }

    [Server]
    void AddCount(UnitType unit, int delta)
    {
        switch (unit)
        {
            case UnitType.Infantry: infantryCount += delta; break;
            case UnitType.Armor: armorCount += delta; break;
            case UnitType.Machinegun: machinegunCount += delta; break;
        }
    }

    // ---------------------------------------------------------------- input

    // This peer's player number: the host is player 1, a joining client is player 2.
    // Matches how Turns derives its own playerNumber (isServer ? 1 : 2).
    public int LocalPlayerNumber => isServer ? 1 : 2;

    // True when it is this player's turn to act.
    public bool IsMyTurn() => IsTurnOf(LocalPlayerNumber);

    void Update()
    {
        // Only the owning client reads the mouse and drives the palette UI. Every player
        // object runs Update, so without this guard input is processed twice.
        if (!isLocalPlayer) return;
        if (!Input.GetMouseButtonDown(0)) return;

        RaycastHit2D hit = Physics2D.Raycast(Input.mousePosition, Vector3.forward);
        if (hit.collider == null) return;

        if (hit.collider.CompareTag("tile")) ClickedTile(hit.collider.gameObject);
        // Palette selection is turn-gated too: off-turn you can't pick anything up,
        // so there's nothing "in hand" to place the moment your turn starts.
        else if (!IsMyTurn()) return;
        else if (hit.collider.CompareTag("grass")) SelectedGrass();
        else if (hit.collider.CompareTag("water")) SelectedWater();
        else if (hit.collider.CompareTag("mountain")) SelectedMountain();
        else if (hit.collider.CompareTag("infantry")) SelectedInfantry();
        else if (hit.collider.CompareTag("armor")) SelectedArmor();
        else if (hit.collider.CompareTag("machinegun")) SelectedMachinegun();
    }

    public void SelectedGrass() => SetCursorTo(CursorType.Grass, grassSprite);
    public void SelectedWater() => SetCursorTo(CursorType.Water, waterSprite);
    public void SelectedMountain() => SetCursorTo(CursorType.Mountain, mountainSprite);
    public void SelectedInfantry() => SetCursorTo(CursorType.Infantry, infantrySprite);
    public void SelectedArmor() => SetCursorTo(CursorType.Armor, armorSprite);
    public void SelectedMachinegun() => SetCursorTo(CursorType.Machinegun, machinegunSprite);

    // Puts a just-picked-up unit "in hand" (used by MapClicker after the server confirms).
    public void SelectCursorFor(UnitType unit)
    {
        switch (unit)
        {
            case UnitType.Infantry: SelectedInfantry(); break;
            case UnitType.Armor: SelectedArmor(); break;
            case UnitType.Machinegun: SelectedMachinegun(); break;
        }
    }

    public void ClickedTile(GameObject tileObject)
    {
        // Turn enforcement: you can only place on your own turn. This (and the count/target
        // checks below) are for responsiveness only — the server re-validates everything.
        if (!IsMyTurn()) return;
        tile target = tileObject.GetComponent<tile>();
        if (target == null) return;
        int tileIndex = tileObject.transform.GetSiblingIndex();

        LandType land = GameTypes.LandFor(selectedCursor);
        if (land != LandType.None)
        {
            if (GetCount(land) > 0 && target.landType == LandType.None
                                   && target.occupyingUnit == UnitType.None)
                CmdPlaceLand(tileIndex, land);
            return;
        }

        UnitType unit = GameTypes.UnitFor(selectedCursor);
        if (unit != UnitType.None && GetCount(unit) > 0
                                  && target.occupyingUnit == UnitType.None)
            CmdPlaceUnit(tileIndex, unit);
    }

    public void ChangeCursorBack() => SetCursorTo(CursorType.Default, cursorSprite);

    // Applies a cursor texture (centered) and records which one is active.
    void SetCursorTo(CursorType type, Texture2D tex)
    {
        selectedCursor = type;
        cursorOffset = tex != null ? new Vector2(tex.width / 2f, tex.height / 2f) : Vector2.zero;
        Cursor.SetCursor(tex, cursorOffset, CursorMode.Auto);
    }
}
