using System.Collections.Generic;

namespace LevelNet.Data
{
    public class SyncContainerManager
    {
        public static SyncContainerManager Instance => _instance;
        private static readonly SyncContainerManager _instance = new();

        private int _idCounter;
        private readonly Dictionary<int, SyncDataContainer> _dataContainers = new();

        public SyncDataContainer Create()
        {
            SyncDataContainer container = new(_idCounter++);
            _dataContainers.Add(container.Id, container);
            return container;
        }
    }
}
