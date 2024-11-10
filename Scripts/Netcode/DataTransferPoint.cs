using System;
using System.Linq;

using LevelNet.Data;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.Assertions;

namespace LevelNet.Netcode
{
    public class DataTransferPoint : NetworkBehaviour
    {
        public static DataTransferPoint Instance {
            get {
                if (!_instance) {
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

        private void Update()
        {
        }

        [Rpc(SendTo.Server)]
        public void SendDataToServerRpc(CompressedData compData, RpcParams rpcParams = default)
        {
            Debug.Log($"{compData} from {rpcParams.Receive.SenderClientId}");
        }

        private void FlushDirtyContainers()
        {
            var dirtyContainers = SyncContainerManager.Instance.GetContainers().Where(c => c.DirtyFlags.IsDirty);
            foreach (var container in dirtyContainers) {
            }
        }

        [ClientRpc]
        public void SendDataToClients_ClientRpc(CompressedData compData, int dataId, ClientRpcParams rpcParams = default)
        {
            NetEventsFabric.Create().RegDataOnClient(compData.Data, dataId);
        }

        [ServerRpc(RequireOwnership =false)]
        public void SendChangesToServer_ServerRpc(CompressedContainerChanges data, int dataId, ServerRpcParams rpcParams = default)
        {
            //TODO validate changes
            var container = SyncContainerManager.ServerInstance.GetContainer(dataId);
            data.SyncContainer(container, true);
            NetEventsFabric.Create().RegChangedContainerSrv(dataId);
        }

        [ClientRpc]
        public void SendChangesToClient_ClientRpc(CompressedContainerChanges data, int dataId, ClientRpcParams rpcParams = default)
        {
            var container = SyncContainerManager.Instance.GetContainer(dataId);
            data.SyncContainer(container, false);
            container.SyncClientState();
        }

        [ClientRpc]
        public void RejectChangesFromClient_ClientRpc(int dataId, ClientRpcParams rpcParams = default)
        {
            throw new NotImplementedException();
        }
    }
}