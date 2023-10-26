using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;


public class Launcher : MonoBehaviourPunCallbacks
{

    public static Launcher instance;

    private void Awake()
    {
        instance = this;
    }

    public GameObject loadingScreen;
    public TMP_Text loadingText;

    public GameObject menuButtons;

    public GameObject createRoomScreen;
    public TMP_InputField roomNameInput;

    public GameObject roomScreen;
    public TMP_Text roomNameText;

    public TMP_Text playerName;
    public List<TMP_Text> allPlayernames = new List<TMP_Text>();

    public GameObject errorScreen;
    public TMP_Text errorText;

    public GameObject roomBrowserScreen;
    public RoomButton theRoomButton;
    private List<RoomButton> allRoomButtons = new List<RoomButton>();

    public GameObject nickNameInputScreen;
    public TMP_InputField nickNameInput;
    public static bool hasSetNickName;

    public string levelThePlay;

    public GameObject startButton;

    public GameObject roomTestButton;


    // Start is called before the first frame update
    void Start()
    {
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Aða Baðlanýlýyor";

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        
#if UNITY_EDITOR
        roomTestButton.SetActive(true);
#endif
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

    }




    void CloseMenus()
    {
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nickNameInputScreen.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {

        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
        loadingText.text = "Lobiye giriliyor...";


    }
    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);

        PhotonNetwork.NickName = Random.Range(0, 100).ToString();
        if (!hasSetNickName)
        {
            CloseMenus();
            nickNameInputScreen.SetActive(true);

            if (PlayerPrefs.HasKey("playerName"))
            {
                nickNameInput.text = PlayerPrefs.GetString("playerName");
            }

        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenRoomCreate()
    {
        CloseMenus();
        createRoomScreen.SetActive(true);
    }

    public void CreateRoom()
    {
        //oda ismi yazýldý mý kontrolü
        if (!string.IsNullOrEmpty(roomNameInput.text))
        {
            // oyuncu sayýsýný sýnýrlama
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 10;

            PhotonNetwork.CreateRoom(roomNameInput.text,options);

            CloseMenus();
            loadingText.text = "Oda Kuruluyor...";
            loadingScreen.SetActive(true);
        }
    }

    public override void OnJoinedRoom()
    {

        CloseMenus();
        roomScreen.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        ListAllPlayers();

        if (PhotonNetwork.IsMasterClient)
        {
            // Sadece ana istemciye oyunu baþlatma yetkisi verilir
            startButton.SetActive(true);
        }

        else
        {
            // Diðer oyuncular için baþlatma düðmesini devre dýþý býrak
            startButton.SetActive(false);

        }

        
    }


    private void ListAllPlayers()
    {
        foreach (TMP_Text player in allPlayernames)
        {
            Destroy(player.gameObject);

        }
        allPlayernames.Clear();

        Player[] players = PhotonNetwork.PlayerList;

        for (int i= 0; i< players.Length; i++)
        {
            TMP_Text newPlayerName = Instantiate(playerName, playerName.transform.parent);
            newPlayerName.text = players[i].NickName;
            newPlayerName.gameObject.SetActive(true);

            allPlayernames.Add(newPlayerName);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text newPlayerName = Instantiate(playerName, playerName.transform.parent);
        newPlayerName.text = newPlayer.NickName;
        newPlayerName.gameObject.SetActive(true);

        allPlayernames.Add(newPlayerName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListAllPlayers();
        
    }



    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Oda Oluþturma Baþarýsýz : " + message;
        CloseMenus();
        errorScreen.SetActive(true);
    }
    public void CloseErrorScreen()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingText.text = "Odadan Ayrýlýnýyor...";
        loadingScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void OpenBrowserScreen()
    {
        CloseMenus();
        roomBrowserScreen.SetActive(true);

    }

    public void CloseBrowserScreen()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    // ne zaman bi deðiþiklik olsa bilgiyi alacaðýz
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomButton rb in allRoomButtons)
        {
            Destroy(rb.gameObject);
        }

        allRoomButtons.Clear();

        theRoomButton.gameObject.SetActive(false);

        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
            {
                RoomButton newButton = Instantiate(theRoomButton, theRoomButton.transform.parent);
                newButton.SetButtonDetails(roomList[i]);
                newButton.gameObject.SetActive(true);

                allRoomButtons.Add(newButton);
            }
        }
    }

    public void JoinRoom(RoomInfo inputInfo)
    {
        PhotonNetwork.JoinRoom(inputInfo.Name);
        CloseMenus();

        loadingText.text = " Odaya Giriliyor...";
        loadingScreen.SetActive(true);
    }

    public void SetNickName()
    {
        if(!string.IsNullOrEmpty(nickNameInput.text))
        {
            PhotonNetwork.NickName = nickNameInput.text;
            CloseMenus();
            // oyuncunun ismini kadetmek için
            PlayerPrefs.SetString("playerName", nickNameInput.text);

            menuButtons.SetActive(true);

            hasSetNickName = true;
        }
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(levelThePlay);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }
    
    public void QuickMenu()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 10;

        PhotonNetwork.CreateRoom("Test", options);
        CloseMenus();
        loadingText.text = "Oda Kuruluyor...";
        loadingScreen.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
