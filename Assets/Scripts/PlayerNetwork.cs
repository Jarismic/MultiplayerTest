using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Transform spawnedObjectPrefab;

    private Transform spawnedObjectTransform;
    
    // Value that is readable by everyone, and changeable by the owner.
    private NetworkVariable<MyCustomData> randomNumber = new NetworkVariable<MyCustomData>(
        new MyCustomData
        {
            _int = 56, _bool = true
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); // Specify who can read and/or write.

    // Need to use INetworkSerializable to allow this to work.
    public struct MyCustomData : INetworkSerializable
    {
        public int _int;
        public bool _bool;
        public FixedString128Bytes message;
        
        // Also need this for some reason.
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref message);
        }
    }
    
    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += (MyCustomData previousValue, MyCustomData newValue) =>
        {
            Debug.Log($"{OwnerClientId}; randomNumber: {newValue._int}; {newValue._bool}; {newValue.message}");
        };
    }

    private void Update()
    {
        
        if (!IsOwner) return;   // Don't run code below unless player owns the object.

        if (Input.GetKeyDown(KeyCode.T))
        {
            // This spawns objects using the server/host. Clients cannot spawn objects themselves using this.
            spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
            spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
            
            return;
            
            TestClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> {1} } });
            
            
            randomNumber.Value = new MyCustomData
            {
                _int = 10, _bool = false, message = "This is a test message!"
            };
            
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            Destroy(spawnedObjectTransform.gameObject);
        }
        
        // Basic player movement
        Vector3 moveDir = new Vector3(0, 0, 0);
        
        if (Input.GetKey(KeyCode.W))
        {
            moveDir.z = +1f;
        }
        
        if (Input.GetKey(KeyCode.S))
        {
            moveDir.z = -1f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            moveDir.x = -1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            moveDir.x = +1f;
        }

        float moveSpeed = 3f;
        transform.position += moveDir * (moveSpeed * Time.deltaTime);
    }

    [ServerRpc]
    private void TestServerRpc(ServerRpcParams serverRpcParams)
    {
        Debug.Log($"TestServerRpc {OwnerClientId}; {serverRpcParams.Receive.SenderClientId}");
    }

    [ClientRpc]
    private void TestClientRpc(ClientRpcParams clientRpcParams)
    {
        Debug.Log("TestClientRpc");
    }
}
