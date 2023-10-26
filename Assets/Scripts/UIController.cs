using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class UIController : MonoBehaviour
{
    public static UIController instance;
    

    private void Awake()
    {
        instance = this;

    }

    public TMP_Text asiriIsinmisMessage;

    public Slider heatSlider;

    public GameObject deathScreen;
    public TMP_Text deathText;

    public Slider healthSlider;

    public TMP_Text killsText;
    public TMP_Text deathsText;

    public GameObject leaderboard;
    public Leaderboard leaderboardDisplay;

    public GameObject endScreen;

    public TMP_Text timerText;

    public GameObject optionsScreen;
    public

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            optionsScreen.SetActive(true);
        }

        if (optionsScreen.activeInHierarchy && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ShowHideOptions()
    {
        if (!optionsScreen)
        {
            optionsScreen.SetActive(true);
        }
        else
        {
            optionsScreen.SetActive(false);
        }
    }
    public void ReturnToMainMenu()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();

    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
