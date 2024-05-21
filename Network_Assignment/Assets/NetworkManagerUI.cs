using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{

    [SerializeField] private Button clientBtn;
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;

    private void Awake()
    {
        clientBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            TurnOffButtons();
            
        });
        serverBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            TurnOffButtons();
        });
        hostBtn.onClick.AddListener(() =>
        { 
            NetworkManager.Singleton.StartHost();
           TurnOffButtons();
        });
    }

    private void TurnOffButtons()
    {
        clientBtn.gameObject.SetActive(false);
        serverBtn.gameObject.SetActive(false);
        hostBtn.gameObject.SetActive(false);
    }
}
