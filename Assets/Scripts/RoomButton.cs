using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;

public class RoomButton : MonoBehaviour
{
    public TMP_Text buttonText;

    public RoomInfo info;

    //temel olarak hangi bilgiyi �ekmek istedi�imiz (input bilgisi)
    public void SetButtonDetails(RoomInfo inputInfo)
    {
        //saklan�lan bilginin input bilgisine e�it olmas�n� istiyoruz
        info = inputInfo;

        //odan�n ad�n� ��reniyoruz ve butonun ad�na aktar�yoruz
        buttonText.text = info.Name;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenRoom()
    {
        Launcher.instance.JoinRoom(info);
    }
}
