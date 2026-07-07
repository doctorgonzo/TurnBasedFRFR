using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Turns : NetworkBehaviour
{
    [SerializeField] Button endTurnButton;
    [SerializeField] GameObject uiPrefab;
    [SyncVar]
    [SerializeField] public bool isPlayer1Turn = true;
    [SyncVar]
    [SerializeField] int turnNumber = 1;
    [SerializeField] public int playerNumber = 1;
    [SerializeField] public TMP_Text turnText;
    [SerializeField] public clicker Clicker;

    public override void OnStartServer()
    {
        playerNumber = 1;
        UpdateTurnText();
        base.OnStartServer();
    }

    public override void OnStartClient()
    {
        playerNumber = isServer ? 1 : 2;
        // The End Turn button's inspector onClick referenced a missing "EndTurn" method with no
        // target, so turns never advanced. Wire it here (RemoveListener first so repeated
        // OnStartClient calls can't stack duplicates). Guarded because this also runs on the
        // per-player Turns instances nested in the Player prefab, which have no button wired.
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
            // Show the End Turn button only on the player whose turn it currently is. Placement
            // is enforced by clicker.IsMyTurn() + the server checks, so we no longer disable the
            // clicker here (doing so used to permanently lock player 2 out).
            endTurnButton.gameObject.SetActive(playerNumber == (isPlayer1Turn ? 1 : 2));
        }
        UpdateTurnText();
        base.OnStartClient();
    }

    // Monotonic index of the current half-turn (increments every time either player ends
    // their turn). Lets tiles record WHEN a unit was placed, e.g. to block same-turn pick-up.
    public int TurnIndex => turnNumber * 2 + (isPlayer1Turn ? 0 : 1);

    // onClick listeners must be parameterless, and CmdEndTurn now takes the injected sender.
    void OnEndTurnClicked() => CmdEndTurn();

    [Command(requiresAuthority = false)]
    public void CmdEndTurn(NetworkConnectionToClient sender = null)
    {
        // Authoritative check: only the player whose turn it is can end it. (This also makes
        // an accidental double-wired button harmless — the second call is off-turn.)
        int senderPlayer = clicker.PlayerNumberOf(sender);
        if (senderPlayer != (isPlayer1Turn ? 1 : 2))
        {
            Debug.LogWarning($"Rejected end-turn from player {senderPlayer}: not their turn.");
            return;
        }
        isPlayer1Turn = !isPlayer1Turn;
        if (isPlayer1Turn)
        {
            turnNumber++;
        }
        RpcTurnEnded(isPlayer1Turn, turnNumber);
    }

    [ClientRpc]
    public void RpcTurnEnded(bool newIsPlayer1Turn, int newTurnNumber)
    {
        isPlayer1Turn = newIsPlayer1Turn;
        turnNumber = newTurnNumber;
        UpdateTurnText();
        // Only the active player sees the End Turn button; the clicker itself is gated by
        // IsMyTurn()/server checks rather than by enabling/disabling the component.
        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(playerNumber == (isPlayer1Turn ? 1 : 2));
    }

    void UpdateTurnText()
    {
        if (turnText == null) return;
        int currentPlayer = isPlayer1Turn ? 1 : 2;
        turnText.text = $"Player {currentPlayer} : Turn# {turnNumber} ";
    }
}
