using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LevelNet.Data;

using Unity.Netcode;

namespace LevelNet.Netcode
{
    internal class ClientState
    {
        private ulong _id;

        public ulong Id => _id;
        public bool IsNew { 
            get; 
            set; 
        }

        public ClientState(ulong id)
        {
            _id = id;
            IsNew = true;
        }
    }

    public class NetcodeEvents : INetEvents
    {
        public bool IsOnline => NetworkManager.Singleton.IsServer || ( NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsConnectedClient);

        public bool IsServer => NetworkManager.Singleton.IsServer;

        public bool IsClient => NetworkManager.Singleton.IsClient;

        public INetEvents.StartAsServerDelegate OnStartAsServer { get; set; }
        public INetEvents.StartAsClientDelegate OnStartAsClient { get; set; }
        public INetEvents.DataCreatedDelegate OnDataCreated { get; set; }

        public ulong ClientId => NetworkManager.Singleton.LocalClientId;

        private bool _enableDataReceiver;

        //TODO По хорошему конечно на разные объекты распилить
        //Server side
        private List<ClientState> _srvClients = new();

        private bool _srvHasNewClient;
        private List<SyncDataContainer> _srvNewContainers = new();
        private HashSet<int> _srvChangedContainersIds = new();

        //Client side
        private HashSet<int> _clnChangedContainersIds = new();

        internal NetcodeEvents()
        {
            var netMan = NetworkManager.Singleton;
            //netMan.OnServerStarted += () => OnStartAsServer?.Invoke();
            //netMan.OnClientStarted += () => OnStartAsClient?.Invoke();

            //netMan.OnServerStarted += () => UnityEngine.Debug.Log("Server started");

            netMan.OnClientConnectedCallback += (id) => {
                if (!IsServer)
                    return;
                _srvClients.Add(new(id));
                _srvHasNewClient = true;
            };
            foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds) {
                _srvClients.Add(new(id));
                _srvHasNewClient = true;
            }

            netMan.OnClientDisconnectCallback += (id) => {
                if (!IsServer)
                    return;
                _srvClients.Remove(_srvClients.Single(c => c.Id == id));
            };

            netMan.OnConnectionEvent += (nm, args) =>
            {
                if(args.EventType == ConnectionEvent.ClientConnected && args.ClientId == ClientId)
                {
                    if (IsClient)
                    {
                        OnStartAsClient?.Invoke();
                    }
                }
                
            };
        }

        public void StartDataReceiver()
        {
        }

        public void NetSendTick()
        {
            if (IsClient) {
                SendContainersUpdatesToServer();
            }
            if (IsServer) {
                SendContainersToOldClients();
                SendContainersToNewClients();
                SendChangesToClients();

                foreach (var clientState in _srvClients.Where(c => c.IsNew))
                {
                    clientState.IsNew = false;
                }
                _srvNewContainers.Clear();
            }
        }

        private void SendChangesToClients()
        {
            if (_srvChangedContainersIds.Count == 0) {
                return;
            }
            foreach (var id in _srvChangedContainersIds) {
                var container = SyncContainerManager.ServerInstance.GetContainer(id);
                DataTransferPoint.Instance.SendChangesToClient_ClientRpc(
                    CompressedContainerChanges.CreateForWriting(container, true),
                    container.Id
                );
            }
            _srvChangedContainersIds.Clear();
        }

        private void SendContainersToNewClients()
        {
            if (!_srvHasNewClient) {
                return;
            }
            _srvHasNewClient = false;

            var clientIds = _srvClients.Where(c => c.IsNew).Select(c => c.Id).ToArray();
            if (clientIds.Length == 0) {
                return;
            }
            ClientRpcParams rpcParams = new() {
                Send = new() {
                    TargetClientIds = clientIds
                }
            };
            foreach (var container in SyncContainerManager.ServerInstance.GetContainers()) {
                DataTransferPoint.Instance.SendDataToClients_ClientRpc(new(container.ServerState), container.Id, rpcParams);
                container.DirtyFlags.ApplyChanges();
            }

        }

        private void SendContainersToOldClients()
        {
            if (_srvNewContainers.Count == 0) {
                return;
            }

            var clientIds = _srvClients.Where(c => !c.IsNew).Select(c => c.Id).ToArray();
            if (clientIds.Length == 0) {
                return;
            }
            ClientRpcParams rpcParams = new() {
                Send = new() {
                    TargetClientIds = clientIds
                }
            };
            foreach (var container in _srvNewContainers) {
                DataTransferPoint.Instance.SendDataToClients_ClientRpc(new(container.ServerState), container.Id, rpcParams);
                container.DirtyFlags.ApplyChanges();
            }

            
        }

        private void SendContainersUpdatesToServer()
        {
            if (_clnChangedContainersIds.Count == 0) {
                return;
            }
            foreach (int id in _clnChangedContainersIds) {
                var container = SyncContainerManager.Instance.GetContainer(id);
                DataTransferPoint.Instance.SendChangesToServer_ServerRpc(
                    CompressedContainerChanges.CreateForWriting(container, false), container.Id);
            }
            _clnChangedContainersIds.Clear();
        }

        public void RegDataContainer(SyncDataContainer dataContainer)
        {
            if (!IsOnline) {
                throw new NetEventException("Data registry unawailable because server isn't online");
            }
            if (!IsServer) {
                throw new NetEventException("Data registry awailable only on server");
            }

            _srvNewContainers.Add(dataContainer);
        }

        public void RegDataOnClient(object data, int containerId)
        {
            if (!IsOnline) {
                throw new NetEventException("Data receiving unawailable because client isn't online");
            }
            if (!IsClient) {
                throw new NetEventException("Data receiving awailable only on client");
            }

            SyncDataContainer container;
            //if (IsServer) {
            //    container = SyncContainerManager.Instance.GetContainer(containerId);
            //    container.UpdateClientData(data);
            //} else {
            container = SyncContainerManager.Instance.CreateOnClient(containerId);
            container.SetupData(data);
            //}

            OnDataCreated?.Invoke(new DataCreatedEventArgs() { container = container });
        }

        public void RegChangedContainer(int id)
        {
            if (!IsOnline) {
                throw new NetEventException("Changes registration unawailable because client isn't online");
            }
            if (!IsClient) {
                throw new NetEventException("Changes registration awailable only on client");
            }
            _clnChangedContainersIds.Add(id);
        }

        public void RegChangedContainerSrv(int id)
        {
            if (!IsOnline) {
                throw new NetEventException("Changes registration unawailable because server isn't online");
            }
            if (!IsServer) {
                throw new NetEventException("Changes registration awailable only on server");
            }
            _srvChangedContainersIds.Add(id);
        }
    }
}