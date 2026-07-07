using UnityEngine;

public class EndTurnButton : MonoBehaviour
{
    [SerializeField] Turns turnScript;
    [SerializeField] clicker Clicker;

    public void OnClick()
    {
        if (turnScript == null)
        {
            Debug.LogError("Turns script not found!");
            return;
        }
        turnScript.CmdEndTurn();
        // Reset the LOCAL player's selection. The serialized Clicker is the inert scene
        // instance — resetting it would change the OS cursor but leave the local player's
        // selectedCursor stale, so the next tile click would still place the old selection.
        if (clicker.Local != null) clicker.Local.ChangeCursorBack();
    }
}
