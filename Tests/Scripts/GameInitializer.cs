using System;
using LevelNet.Data;
using LevelView;
using UnityEngine;

namespace LevelNet
{
    public class NetEventException : Exception
    {
        public NetEventException(string message):base(message) { }
    }

    public class DataCreatedEventArgs
    {

    }
    public interface INetEvents
    {
        delegate void StartAsServerDelegate();
        delegate void StartAsClientDelegate();
        delegate void DataCreatedDelegate(DataCreatedEventArgs args);
        bool IsOnline { get; }
        bool IsServer { get; }

        StartAsServerDelegate OnStartAsServer { get; set; }
        StartAsClientDelegate OnStartAsClient { get; set; }
        DataCreatedDelegate OnDataCreated { get; set; }

        void StartDataReceiver();

        void RegDataContainer(SyncDataContainer dataContainer);
    }



    public static class NetEventsFabric
    {
        private static INetEvents _netEvents;

        public static INetEvents Create() => _netEvents ??= new Netcode.NetcodeEvents();
    }
}

namespace LevelNet.Netcode
{
}

namespace LevelNet.Tests
{
    public class GameInitializer : MonoBehaviour
    {
        private void Start()
        {
            Init();
        }

        private void Init()
        {
            INetEvents netEvents = NetEventsFabric.Create();
            if (netEvents.IsOnline)
            {
                if (netEvents.IsServer) {
                    CreateData();
                }
                else
                {
                    netEvents.OnDataCreated = (args) => SyncView();
                    netEvents.StartDataReceiver();
                }
            }
            else
            {
                netEvents.OnStartAsServer = ()=> CreateData();
                netEvents.OnStartAsClient = () =>
                {
                    netEvents.OnDataCreated = (args) => SyncView();
                    netEvents.StartDataReceiver();
                };
            }
        }

        private void CreateData()
        {

        }

        private void SyncView()
        {

        }

    }
}