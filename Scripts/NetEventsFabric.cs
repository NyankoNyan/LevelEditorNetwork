using System;

using LevelNet.Data;

namespace LevelNet
{
    public class NetEventException : Exception
    {
        public NetEventException(string message) : base(message) { }
    }

    public class DataCreatedEventArgs
    {
        public SyncDataContainer container;
    }

    public interface INetEvents
    {
        delegate void StartAsServerDelegate();
        delegate void StartAsClientDelegate();
        delegate void DataCreatedDelegate(DataCreatedEventArgs args);
        bool IsOnline { get; }
        bool IsServer { get; }
        bool IsClient { get; }

        StartAsServerDelegate OnStartAsServer { get; set; }
        StartAsClientDelegate OnStartAsClient { get; set; }
        DataCreatedDelegate OnDataCreated { get; set; }

        void StartDataReceiver();

        void RegDataContainer(SyncDataContainer dataContainer);
        void NetSendTick();
        void RegDataOnClient(object data, int containerId);
        void RegChangedContainer(int id);
        void RegChangedContainerSrv(int id);
    }

    public static class NetEventsFabric
    {
        private static INetEvents _netEvents;

        public static INetEvents Create() => _netEvents ??= new Netcode.NetcodeEvents();
    }
}
