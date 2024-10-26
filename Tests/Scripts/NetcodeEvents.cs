using System.Collections.Generic;
using LevelNet.Data;
using Unity.Netcode;
using UnityEngine;

namespace LevelNet.Netcode
{    
    public class NetcodeEvents : INetEvents
    {
        public bool IsOnline => NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient;

        public bool IsServer => NetworkManager.Singleton.IsServer;

        public INetEvents.StartAsServerDelegate OnStartAsServer { get; set; }
        public INetEvents.StartAsClientDelegate OnStartAsClient { get; set; }
        public INetEvents.DataCreatedDelegate OnDataCreated { get; set; }


        private List<SyncDataContainer> _dataContainers = new();
        private bool _enableDataReceiver;

        internal NetcodeEvents()
        {
            var netMan = NetworkManager.Singleton;
            netMan.OnServerStarted += () => OnStartAsServer?.Invoke();
            netMan.OnClientStarted += () => OnStartAsClient?.Invoke();

            
        }

        public void StartDataReceiver()
        {
            throw new System.NotImplementedException();
        }

        public void RegDataContainer(SyncDataContainer dataContainer)
        {
            if (!IsOnline)
            {
                throw new NetEventException("Data registry unawailable because server isn't online");
            }
            if (!IsServer)
            {
                throw new NetEventException("Data registry awailable only on server");
            }

            _dataContainers.Add(dataContainer);
        }
    }
}