using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyNetworkManager : NetworkManager
{
    //[SerializeField] public GameObject uiPrefab;
    //[SyncVar(hook = nameof(OnTurnChanged))]
    //public int currentPlayerId;
    //public void HostButton()
    //{
    //    StartHost();
    //    SceneManager.LoadScene(1);
    //    NetworkManager.singleton.ServerChangeScene("Main");
    //}
    //public void JoinButton()
    //{
    //    StartClient();
    //}
    //public override void OnServerSceneChanged(string sceneName)
    //{
    //    base.OnServerSceneChanged(sceneName);
    //    if (sceneName == "Main") // Or any other scene where you want to spawn the UI
    //    {
    //        GameObject uiInstance = Instantiate(uiPrefab);
    //        NetworkServer.Spawn(uiInstance); // Spawn for all clients
    //    }
    //}
    //[ClientRpc]
    //private void OnTurnChanged(int oldPlayerId, int newPlayerId)
    //{
    //    //update ui here based on newPlayerId

    //}
    //[Server]
    //public void ChangeTurn(int nextPlayerId)
    //{
    //    currentPlayerId = nextPlayerId;
    //}
}
