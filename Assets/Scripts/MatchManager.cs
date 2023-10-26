using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;


//IOnEventCallback meydana gelecek olaylarý kontrol etmemize izin verecek sistem
public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager instance;



    void Awake()
    {
        instance = this;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // Oyuncunun çýkýþýný takip etmek için actor numarasýný kullanarak oyuncuyu bul
        PlayerInfo disconnectedPlayer = allPlayers.Find(player => player.actor == otherPlayer.ActorNumber);

        if (disconnectedPlayer != null)
        {
            // Oyuncunun çýkýþ durumunu güncelle
            disconnectedPlayer.isDisconnected = true;

            // Oyuncu listesini güncelle
            ListPlayersSend();
        }
    }


    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStats,
        NextMatch,
        TimerSync

    }


    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int index;

    private List<Leaderboard> lboardplayers = new List<Leaderboard>();

    public enum GameState
    {
        Waitinig,
        Playing,
        Ending
    }

    public int killsTowin = 3;
    public Transform mapCamPoint;
    public GameState state = GameState.Waitinig;
    public float waitAfterEnding = 5f;

    public bool perpetual;

    public float matchLength = 240f;
    private float currentMatchTime;

    private float sendTimer;

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            NewPlayerSend(PhotonNetwork.NickName);
        }
        state = GameState.Playing;

        SetupTimer();

        if (!PhotonNetwork.IsMasterClient)
        {
            UIController.instance.timerText.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Tab) && state != GameState.Ending)
        {
            if (UIController.instance.leaderboard.activeInHierarchy)
            {
                UIController.instance.leaderboard.SetActive(false);
            }
            else
            {
                ShowLeaderboard();
            }
        }

        if (PhotonNetwork.IsMasterClient)
        {

        

            if (currentMatchTime > 0f && state == GameState.Playing)
            {
            currentMatchTime -= Time.deltaTime;
                if (currentMatchTime <= 0f)
                {
                currentMatchTime = 0f;

                state = GameState.Ending;

                ListPlayersSend();

                StateCheck();
                

                }
                UpdateTimerDisplay();

                sendTimer -= Time.deltaTime;
                if (sendTimer <= 0)
                {
                    sendTimer += 1f;
                    TimerSend();
                }



            }

        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code < 200)
        {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            //Debug.Log("received event " + theEvent);

            switch (theEvent)
            {
                case EventCodes.NewPlayer:

                    NewPlayerReceive(data);

                    break;

                case EventCodes.ListPlayers:

                    ListPlayersReceive(data);

                    break;

                case EventCodes.UpdateStats:

                    UpdateStatsReceive(data);

                    break;

                case EventCodes.NextMatch:

                    NextMatchReceive();

                    break;

                case EventCodes.TimerSync:

                    TimerReceive(data);

                    break;

               

            }
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void NewPlayerSend(string username)
    {
        object[] package = new object[4];
        package[0] = username;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
            );


    }

    public void NewPlayerReceive(object[] dataReceived)
    {
        string name = (string)dataReceived[0];
        int actor = (int)dataReceived[1];
        int kills = (int)dataReceived[2];
        int deaths = (int)dataReceived[3];

        PlayerInfo player = new PlayerInfo(name, actor, kills, deaths);
        allPlayers.Add(player);

        ListPlayersSend();
    }

    public void ListPlayersSend()
    {
        // Çýkýþ yapan oyuncuyu hariç tutmak için aktif oyuncularý seç
        List<PlayerInfo> activePlayers = allPlayers.FindAll(player => !player.isDisconnected);

        object[] package = new object[activePlayers.Count + 1];
        package[0] = state;

        for (int i = 0; i < activePlayers.Count; i++)
        {
            object[] piece = new object[4];

            piece[0] = activePlayers[i].name;
            piece[1] = activePlayers[i].actor;
            piece[2] = activePlayers[i].kills;
            piece[3] = activePlayers[i].deaths;

            package[i+1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }


    public void ListPlayersReceive(object[] dataReceived)
    {
        allPlayers.Clear();

        state = (GameState)dataReceived[0];

        for (int i = 1; i < dataReceived.Length; i++)
        {
            object[] piece = (object[]) dataReceived[i];

            PlayerInfo player = new PlayerInfo(
                (string)piece[0],
                (int)piece[1],
                (int)piece[2],
                (int)piece[3]
                );

            allPlayers.Add(player);

            if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
            {
                index = i-1;
            }
        }
        StateCheck();
    }

    public void UpdateStatsSend(int actorSending, int statToUpdate, int amountToChange)
    {
        object[] package = new object[] { actorSending, statToUpdate, amountToChange };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStats,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );

    }

    public void UpdateStatsReceive(object[] dataReceived)
    {
        int actor = (int)dataReceived[0];
        int statType = (int)dataReceived[1];
        int amount = (int)dataReceived[2];

        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].actor == actor)
            {
                switch (statType)
                {
                    case 0: //kills
                        allPlayers[i].kills += amount;
                        Debug.Log("Player " + allPlayers[i].name + " : kills " + allPlayers[i].kills);
                        break;

                    case 1: //deaths
                        allPlayers[i].deaths += amount;
                        Debug.Log("Player " + allPlayers[i].name + " : deaths " + allPlayers[i].deaths);
                        break;
                }
                if (i == index)
                {
                    UpdateStatsDisplay();
                }

                if (UIController.instance.leaderboard.activeInHierarchy)
                {
                    ShowLeaderboard();
                }

                break;
            }
        }
        ScoreCheck();

    }

    public void UpdateStatsDisplay()
    {
        if (allPlayers.Count > index)
        {
            UIController.instance.killsText.text = "Öldürmeler : " + allPlayers[index].kills;
            UIController.instance.deathsText.text = "Ölümler : " + allPlayers[index].deaths;
        }
        else
        {
            UIController.instance.killsText.text = "Öldürmeler : 0";
            UIController.instance.deathsText.text = "Ölümler : 0 ";
        }

    }

    void ShowLeaderboard()
    {
        UIController.instance.leaderboard.SetActive(true);
        foreach (Leaderboard lp in lboardplayers)
        {
            Destroy(lp.gameObject);

        }
        lboardplayers.Clear();

        UIController.instance.leaderboardDisplay.gameObject.SetActive(false);

        List<PlayerInfo> sorted = SortPlayers(allPlayers);

        foreach (PlayerInfo player in sorted)
        {
            Leaderboard newPlayerDisplay = Instantiate(UIController.instance.leaderboardDisplay, UIController.instance.leaderboardDisplay.transform.parent);

            newPlayerDisplay.SetDetails(player.name, player.kills, player.deaths);

            newPlayerDisplay.gameObject.SetActive(true);

            lboardplayers.Add(newPlayerDisplay);

        }
    }
    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while (sorted.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];
            foreach (PlayerInfo player in players)
            {
                if (!sorted.Contains(player))
                {
                    if (player.kills > highest)
                    {
                        selectedPlayer = player;
                        highest = player.kills;
                    }
                }
            }
            sorted.Add(selectedPlayer);
        }
        return sorted;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        SceneManager.LoadScene(0);
    }

    void ScoreCheck()
    {
        bool winnerFound = false;
        foreach (PlayerInfo player in allPlayers)
        {
            if (player.kills >=killsTowin && killsTowin > 0)
            {
                winnerFound = true;
                break;
            }
        }
        if (winnerFound)
        {
            if (PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                state = GameState.Ending;
                ListPlayersSend();
            }
        }
    }
    
    void StateCheck()
    {
        if (state == GameState.Ending)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        state = GameState.Ending;
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        UIController.instance.endScreen.SetActive(true);
        ShowLeaderboard();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera.main.transform.position = mapCamPoint.position;
        Camera.main.transform.rotation = mapCamPoint.rotation;

        StartCoroutine(EndCo());
    }

    private IEnumerator EndCo()
    {
        yield return new WaitForSeconds(waitAfterEnding);

        if (!perpetual)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NextMatchSend();
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void NextMatchSend()
    {

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NextMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void NextMatchReceive()
    {
        state = GameState.Playing;
        UIController.instance.endScreen.SetActive(false);
        UIController.instance.leaderboard.SetActive(false);

        foreach (PlayerInfo player in allPlayers)
        {
            player.kills = 0;
            player.deaths = 0;
        }

        UpdateStatsDisplay();

        PlayerSpawner.instance.SpawnPlayer();

        SetupTimer();
    }

    public void SetupTimer()
    {
        if (matchLength > 0)
        {
            currentMatchTime = matchLength;
            UpdateTimerDisplay(); 
        }
    }

    public void UpdateTimerDisplay()
    {
        var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);

        UIController.instance.timerText.text = timeToDisplay.Minutes.ToString("00") + " : " + timeToDisplay.Seconds.ToString("00");
    }

    public void TimerSend()
    {
        object[] package = new object[] {(int) currentMatchTime, state };

        PhotonNetwork.RaiseEvent( 
            (byte)EventCodes.TimerSync,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void TimerReceive(object[]dataReceived)
    {
        currentMatchTime = (int)dataReceived[0];
        state = (GameState)dataReceived[1];

        UpdateTimerDisplay();

        UIController.instance.timerText.gameObject.SetActive(true);

    }


    [System.Serializable]
    public class PlayerInfo
    {
        public string name;
        public int actor;
        public int kills;
        public int deaths;
        public bool isDisconnected;

        public PlayerInfo(string _name, int _actor, int _kills, int _deaths)
        {
            name = _name;
            actor = _actor;
            kills = _kills;
            deaths = _deaths;
        }
    }
}
