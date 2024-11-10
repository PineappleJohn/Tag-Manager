using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using System;
using System.Collections;
using UnityEngine;

public class JohnTagManager : MonoBehaviourPunCallbacks
{
    [Header("Configured for Photon (Latest)")]
    public Gamemodes gamemodes;
    public GameObject LeftTrigger, RightTrigger;
    public SkinnedMeshRenderer[] meshes;
    public Material tagged, untagged;
    [HideInInspector] public CurrentGamemode currentGamemode;
    [HideInInspector] public Player me;
    public string gorillaRig = "GorillaPlayer";
    GameObject gorilla;
    public SceneStuff[] sceneManagement;

    bool tage = false;

    PhotonView ptb;

    [System.Serializable]
    public class SceneStuff
    {
        public string sceneName = "Example Scene";
        public bool cooldown = true;
        public bool tagFreeze = true;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        if (PhotonNetwork.LocalPlayer.IsMasterClient) ptb.RPC("RunMasterFunctions", me, "plr joined", newPlayer);
    }

    TagTrigger r, l;

    private void Start() //configurs everything
    {
        ptb = GetComponent<PhotonView>();

        l = LeftTrigger.GetComponent<TagTrigger>();
        r = RightTrigger.GetComponent<TagTrigger>();

        if (LeftTrigger.GetComponent<TagTrigger>() == null)
        LeftTrigger.AddComponent<TagTrigger>();
        if (RightTrigger.GetComponent<TagTrigger>() == null)
            RightTrigger.AddComponent<TagTrigger>();

        RightTrigger.GetComponent<TagTrigger>().tagManager = this;
        LeftTrigger.GetComponent<TagTrigger>().tagManager = this;
        RightTrigger.GetComponent<TagTrigger>().view = ptb;
        LeftTrigger.GetComponent<TagTrigger>().view = ptb;
        currentGamemode = (CurrentGamemode)gamemodes;

        gorilla = GameObject.Find("Gorilla Rig/" + gorillaRig);
    }

    public void TagSomeone(Player ptPlayer, bool untagme = false)
    {
        ptb.RPC("Tagger", RpcTarget.All, ptPlayer, false);
        ptb.RPC("RunMasterFunctions", RpcTarget.MasterClient, "inc");
        if (untagme)
        {
            SwapTextures(untagged);
            tage = true;
        }
        RunTaggedFunctions();
    }

    public void RunTaggedFunctions()
    {
        if (sceneManagement.Length > 0)
            foreach (var a in sceneManagement)
            {
                if (a.sceneName == gorilla.scene.name)
                {
                    if (a.cooldown)
                    {
                        StartCoroutine(delay());
                    }
                    if (a.tagFreeze)
                        StartCoroutine(freeze());
                }
            }
        else
        {
            StartCoroutine(delay());
            StartCoroutine(freeze());
        }
    }

    IEnumerator freeze(float delay = 1.3f)
    {
        gorilla.GetComponent<GorillaLocomotion.Player>().disableMovement = true;
        yield return new WaitForSeconds(delay);
        gorilla.GetComponent<GorillaLocomotion.Player>().disableMovement = false;
    }

    IEnumerator delay(float delay = 1.3f)
    {
        r.ToggleTagAbility();
        l.ToggleTagAbility();
        yield return new WaitForSeconds(delay);
        r.ToggleTagAbility();
        l.ToggleTagAbility();
    }

    [PunRPC]
    private void Tagger(Player taggedPlayer, bool reset = false) //it will send to all players to create sounds and be more compatible on older versions of photon.
    {
        if (!reset)
        {
            if (PhotonNetwork.LocalPlayer != taggedPlayer) return;
            if (tage) return;
            SwapTextures(tagged);
            tage = true;
        }
        else
        {
            SwapTextures(untagged);
            tage = false;
        }
    }

    int tc = 0;

    [PunRPC]
    private void RunMasterFunctions(string function, Player joined = null)
    {
        switch (function)
        {
            case "inc":
                tc++;
                if (tc >= PhotonNetwork.CurrentRoom.PlayerCount)
                    ptb.RPC("Tagger", RpcTarget.All, null, true);
                else if (tc <= 3)
                {
                    currentGamemode = CurrentGamemode.Tag;
                }
                break;
            case "plr joined":
                switch (currentGamemode)
                {
                    case CurrentGamemode.Tag: //the tagger would be multiplied
                        break;
                    case CurrentGamemode.Infection:
                        ptb.RPC("Tagger", RpcTarget.All, joined);
                        break;
                    case CurrentGamemode.Casual: break;
                }
                break;

        }
    }

    public void SwapTextures(Material tex)
    {
        foreach (SkinnedMeshRenderer a in meshes)
        {
            a.material = tex;
        }
    }

    public enum Gamemodes
    {
        Casual,
        Tag,
        Infection
    }

    public enum CurrentGamemode
    {
        Casual, //disable everything
        Tag,
        Infection
    }

    public enum AnticheatPunishments
    {
        Banning,
        Kicking,
        DisablingMultiplayer,
        Disconnecting
    }

    public AnticheatPunishments punishments = AnticheatPunishments.DisablingMultiplayer;

    public void Flag()
    {
        if (!PlayerPrefs.HasKey("AnticheatStrikes"))
        {
            Debug.Log("First strike!");
        }

        int strikes = PlayerPrefs.GetInt("AnticheatStrikes");

        //get the players id and give it one strike

        if (strikes < 3)
            PlayerPrefs.SetInt("AnticheatStrikes", strikes + 1);
        else
        {
            switch (punishments)
            {
                case AnticheatPunishments.Banning:
                    PlayFabAdminAPI.BanUsers(new PlayFab.AdminModels.BanUsersRequest { Bans = new System.Collections.Generic.List<PlayFab.AdminModels.BanRequest> 
                    {
                        new() { PlayFabId = SystemInfo.deviceUniqueIdentifier, Reason = "Cheating", DurationInHours = 8 * 24 }
                    } }, errorCallback => { Debug.Log("error :("); }, resultCallback => ResetStrikes());
                    break;
                case AnticheatPunishments.Kicking:
                    ResetStrikes();
                    Environment.Exit(0);
                    break;
                case AnticheatPunishments.DisablingMultiplayer:
                    ResetStrikes();
                    PhotonNetwork.DestroyAll();
                    break;
                case AnticheatPunishments.Disconnecting:
                    ResetStrikes();
                    PhotonNetwork.Disconnect();
                    break;
            }
        }

        PlayerPrefs.Save();
    }

    protected void ResetStrikes() => PlayerPrefs.SetInt("AnticheatStrikes", 0); //idk what to do with this
}