using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BulletLogic : NetworkBehaviour
{
    // Start is called before the first frame update

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
     

    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner) return;
            transform.position += new Vector3(transform.forward.x,0,transform.forward.z) * (40 * Time.deltaTime);
    }

 
}
