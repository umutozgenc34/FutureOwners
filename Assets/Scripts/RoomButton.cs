using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;

public class RoomButton : MonoBehaviour
{
    public TMP_Text buttonText;

    public RoomInfo info;

    //temel olarak hangi bilgiyi çekmek istediðimiz (input bilgisi)
    public void SetButtonDetails(RoomInfo inputInfo)
    {
        //saklanýlan bilginin input bilgisine eþit olmasýný istiyoruz
        info = inputInfo;

        //odanýn adýný öðreniyoruz ve butonun adýna aktarýyoruz
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
