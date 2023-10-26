using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MiniMap : MonoBehaviourPunCallbacks
{
    private Dictionary<int, Transform> players = new Dictionary<int, Transform>();

    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            foreach (KeyValuePair<int, Transform> player in players)
            {
                int playerId = player.Key;
                Transform playerTransform = player.Value;

                if (playerTransform != null)
                {
                    if (PhotonView.Find(playerId) != null && PhotonView.Find(playerId).IsMine)
                    {
                        Vector3 newPosition = playerTransform.position;
                        newPosition.y = transform.position.y;
                        transform.position = newPosition;

                        transform.rotation = Quaternion.Euler(90f, playerTransform.eulerAngles.y, 0f);
                    }
                }
            }
        }
    }

    public void AddPlayer(int playerId, Transform playerTransform)
    {
        if (!players.ContainsKey(playerId))
        {
            players.Add(playerId, playerTransform);
        }
    }

    public void RemovePlayer(int playerId)
    {
        if (players.ContainsKey(playerId))
        {
            players.Remove(playerId);
        }
    }
}


