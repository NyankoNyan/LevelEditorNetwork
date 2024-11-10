using System.Collections.Generic;
using LevelNet.Data;

using UnityEngine;

namespace LevelNet.Tests
{
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField]
        private List<Color> _clientsColors = new() {
            Color.red,
            Color.blue,
            Color.green,
            Color.cyan,
            Color.yellow,
            Color.magenta,
        };

        [SerializeField]
        private Vector2Int _size = new(5, 5);

        private INetEvents netEvents;

        private void Start()
        {
        }

        private void Update()
        {
            if (netEvents != null)
            {
                netEvents.NetSendTick();
            }
        }

        public void Init()
        {
            netEvents = NetEventsFabric.Create();
            if (netEvents.IsOnline)
            {
                if (netEvents.IsClient)
                {
                    ClientSetup();
                }
                if (netEvents.IsServer)
                {
                    CreateData();
                }
            }
            else
            {
                netEvents.OnStartAsServer = () => CreateData();
                netEvents.OnStartAsClient = () =>
                {
                    ClientSetup();
                };
            }
        }

        private void ClientSetup()
        {
            netEvents.OnDataCreated = (args) => SyncView(args.container);
            FindAnyObjectByType<GameBlocksView>().PaintColor = _clientsColors[(int)netEvents.ClientId % _clientsColors.Count];
            netEvents.StartDataReceiver();
        }

        private void CreateData()
        {
            var container = SyncContainerManager.ServerInstance.CreateOnServer();
            GameBlocksState blockState = new()
            {
                size = _size,
                colors = ColorsArray(_size.x * _size.y, Color.white)
            };
            container.SetupData(blockState);

            static Color[] ColorsArray(int size, Color color)
            {
                Color[] colors = new Color[size];
                for (int i = 0; i < size; i++)
                {
                    colors[i] = color;
                }
                return colors;
            }
        }

        private void SyncView(SyncDataContainer container)
        {
            if (container.ClientState is GameBlocksState gameBlocksState)
            {
                var view = GameObject.FindAnyObjectByType<GameBlocksView>();
                view.InitData(container);
            }
            else
            {
                throw new System.Exception($"Unknown data type on client {container.ClientState.GetType().FullName}");
            }
        }
    }
}