using LevelNet.Data;

using UnityEngine;

namespace LevelNet.Tests
{
    public class GameInitializer : MonoBehaviour
    {
        private INetEvents netEvents;

        private void Start()
        {
        }

        private void Update()
        {
            if (netEvents != null) {
                netEvents.NetSendTick();
            }
        }

        public void Init()
        {
            netEvents = NetEventsFabric.Create();
            if (netEvents.IsOnline) {
                if (netEvents.IsClient) {
                    netEvents.OnDataCreated = (args) => SyncView(args.container);
                    netEvents.StartDataReceiver();
                }
                if (netEvents.IsServer) {
                    CreateData();
                }
            } else {
                netEvents.OnStartAsServer = () => CreateData();
                netEvents.OnStartAsClient = () => {
                    netEvents.OnDataCreated = (args) => SyncView(args.container);
                    netEvents.StartDataReceiver();
                };
            }
        }

        private void CreateData()
        {
            var container = SyncContainerManager.ServerInstance.CreateOnServer();
            GameBlocksState blockState = new() {
                size = new Vector2Int(2, 2),
                colors = ColorsArray(4, Color.white)
            };
            container.SetupData(blockState);

            static Color[] ColorsArray(int size, Color color)
            {
                Color[] colors = new Color[size];
                for (int i = 0; i < size; i++) {
                    colors[i] = color;
                }
                return colors;
            }
        }

        private void SyncView(SyncDataContainer container)
        {
            if (container.ClientState is GameBlocksState gameBlocksState) {
                var view = GameObject.FindAnyObjectByType<GameBlocksView>();
                view.InitData(container);
            } else {
                throw new System.Exception($"Unknown data type on client {container.ClientState.GetType().FullName}");
            }
        }

    }
}