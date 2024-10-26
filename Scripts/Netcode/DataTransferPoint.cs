using System.Threading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace LevelNet.Netcode
{
    public class DataTransferPoint : NetworkBehaviour
    {
        public static DataTransferPoint Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindObjectOfType<DataTransferPoint>();
                    Assert.IsNotNull(_instance);
                }
                return _instance;
            }
        }

        private static DataTransferPoint _instance;

        private void Start()
        {
        }

        [Rpc(SendTo.Server)]
        public void SendDataToServerRpc(CompressedData compData, RpcParams rpcParams = default)
        {
            Debug.Log($"{compData} from {rpcParams.Receive.SenderClientId}");
        }
    }
}