using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class PlayerNetwork : NetworkBehaviour
{
    private TMP_InputField nameField;

    public static NetworkVariable<bool> start = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
    private NetworkVariable<int> ammo = new NetworkVariable<int>(5, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> health = new NetworkVariable<int>(6, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> life = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Camera m_camera;

    [SerializeField] private Transform _bulletPrefab;
    [SerializeField] private Transform _ammoPrefab;
    [SerializeField] private Transform _healthPrefab;

    private void Start()
    {
        if (start.Value)
        {
            for (int i = 0; i < 4; i++)
            {
                Transform spawnedObject = Instantiate(_ammoPrefab);
                spawnedObject.GetComponent<NetworkObject>().Spawn(true);
                spawnedObject.position = new Vector3(Random.Range(-27, 27), -0.5f, Random.Range(-26, 26));
            }
            for (int i = 0; i < 4; i++)
            {
                Transform spawnedObject = Instantiate(_healthPrefab);
                spawnedObject.GetComponent<NetworkObject>().Spawn(true);
                spawnedObject.position = new Vector3(Random.Range(-27, 27), -0.5f, Random.Range(-26, 26));
            }

            start.Value = false;
        }
        
    }

    public override void OnNetworkSpawn()
    {
        m_camera = Camera.main;
        base.OnNetworkSpawn();
        
        gameObject.name = "Player " + (OwnerClientId + 1);
         
    }

    void Update()
    {
        if (!IsOwner) return;
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (ammo.Value >= 1)
            {
                ammo.Value -= 1;
                ShootServerRpc();
            }
            else
            {
                Debug.Log("Player " + (OwnerClientId + 1) + " is out off ammo");
            }
        }

       

        Vector3 moveDir = new Vector3(0, 0, 0);
        float moveSpeed = 15f;

        if (Input.GetKey(KeyCode.W)) 
            moveDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) 
            moveDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) 
            moveDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) 
            moveDir.x = +1f;

        transform.position += moveDir * (moveSpeed * Time.deltaTime);

        if (Application.isFocused)
        {
            
            var lookAtPos = Input.mousePosition;
            lookAtPos.z = m_camera.transform.position.y - transform.position.y;
            lookAtPos = m_camera.ScreenToWorldPoint(lookAtPos);
            transform.forward = lookAtPos - transform.position;
        }
    }
    
    
    [ServerRpc(RequireOwnership = false)]
    private void ShootServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        SpawnBulletClientRpc(clientId);
    }


    [ClientRpc]
    private void SpawnBulletClientRpc(ulong id,ClientRpcParams clientRpcParams = default)
    {
        
        Transform spawnedObject = Instantiate(_bulletPrefab);
        Transform referenceObject = GetNetworkBehaviour((ushort)id).gameObject.transform;
        spawnedObject.GetComponent<NetworkObject>().Spawn(true);
        spawnedObject.position = referenceObject.position + (referenceObject.forward);
        spawnedObject.rotation = referenceObject.rotation;
        spawnedObject.tag = id.ToString();

    }
    
    
    [ServerRpc(RequireOwnership = false)]
    private void DestroyObjectServerRpc(ulong obj)
    {
        
        DestroyObjectClientRpc(obj);
    }


    [ClientRpc]
    private void DestroyObjectClientRpc(ulong id,ClientRpcParams clientRpcParams = default)
    {
        Destroy(GetNetworkObject(id).gameObject);
    }
    
    
    private void OnCollisionEnter(Collision collision)
    {
        if (IsServer)
        {
            if (collision.gameObject.GetComponent<BulletLogic>())
            {
                gameObject.GetComponent<PlayerNetwork>().health.Value -= 1;
                if (gameObject.GetComponent<PlayerNetwork>().health.Value <= 0)
                {
                    life.Value -= 1;
                    if (life.Value >= 1)
                    {
                        gameObject.GetComponent<PlayerNetwork>().health.Value = 6;
                    }
                    else
                        Destroy(gameObject);


                }
                DestroyObjectServerRpc(collision.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
            }
            if (collision.gameObject.GetComponent<HealthReference>())
            {
                if (gameObject.GetComponent<PlayerNetwork>().health.Value < 6)
                {
                    Debug.Log("Player " + (OwnerClientId + 1) + " healed");
                    gameObject.GetComponent<PlayerNetwork>().health.Value = 6;
                    DestroyObjectServerRpc(collision.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
                }
            }
        }

        if(!IsOwner)return;
            if (collision.gameObject.GetComponent<AmmoReference>())
            {
                if (ammo.Value < 5)
                {
                    Debug.Log("Player " + (OwnerClientId + 1) + " has reloaded");
                    ammo.Value = 5;
                    DestroyObjectServerRpc(collision.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
                }
            }
    }

   
    private GameObject target;
    Rect rect = new Rect(0, 0, 300, 100);
    Vector3 offset = new Vector3(-1f, 0f, -5f); // height above the target position
    void OnGUI()
    {  
        // if(!IsOwner) return;
        target = gameObject;
        Vector3 point = Camera.main.WorldToScreenPoint(target.transform.position + offset);
        rect.x = point.x;
        rect.y = Screen.height - point.y - rect.height; // bottom left corner set to the 3D point
        switch (health.Value)
        {
            case 6:
            GUI.color = Color.green;
            break;
            case 5:
                GUI.color = Color.green;
                break;
            case 4:
                GUI.color = Color.yellow;
                break;
            case 3:
                GUI.color = Color.yellow;
                break;
            case 2:
                GUI.color = Color.red;
                break;
            case 1:
                GUI.color = Color.red;
                break;
            
        }
        //Static array was the only way to store strings, but I don't know if it's working cross network
        GUI.HorizontalSlider(new Rect(rect.x,rect.y,50,20),health.Value,2f,6f);
        
        
    } 
}
    
        //names[OwnerClientId];
