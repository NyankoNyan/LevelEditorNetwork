using System.Collections.Generic;

namespace LevelNet.Data
{
    public class SyncContainerManager
    {
        public static SyncContainerManager Instance => _instance ??= new();
        public static SyncContainerManager ServerInstance => _serverInstance ??= new();

        private static SyncContainerManager _instance;
        private static SyncContainerManager _serverInstance;

        private int _idCounter;
        private readonly Dictionary<int, SyncDataContainer> _dataContainers = new();

        private SyncContainerManager() { }

        public SyncDataContainer CreateOnServer()
        {
            SyncDataContainer container = new(_idCounter++);
            _dataContainers.Add(container.Id, container);
            NetEventsFabric.Create().RegDataContainer(container);
            return container;
        }

        public SyncDataContainer CreateOnClient(int id)
        {
            SyncDataContainer container = new(id);
            _dataContainers.Add(container.Id, container);
            return container;
        }

        public SyncDataContainer GetContainer(int id)
        {
            return _dataContainers[id];
        }

        public IEnumerable<SyncDataContainer> GetContainers()
        {
            return _dataContainers.Values;
        }
    }
}