using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System.Diagnostics;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace LevelNet.Tests
{
    public class NetcodeAutoconnect : NetworkBehaviour
    {
        [SerializeField]
        private bool _startAsHost = true;

        [SerializeField]
        private int _playersCount = 2;

        [SerializeField]
        private string _buildPath = "Build/WinTest";

        [SerializeField]
        private string _scenePath = "Assets/LevelEditorNetwork/Tests/Scenes/BaseNetworkTest.unity";

        private void Start()
        {
            Init();
            FindAnyObjectByType<GameInitializer>().Init();
        }

#if UNITY_EDITOR

        [ContextMenu("Build And Start Multiplayer")]
        private void StartLocalMultiplayer()
        {
            Build();
            EditorApplication.EnterPlaymode();
        }

#endif

        private void Init()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            if (args.Contains("-client"))
            {
                NetworkManager.Singleton.StartClient();
            }
            else if (args.Contains("-host"))
            {
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                if (_startAsHost)
                {
                    NetworkManager.Singleton.StartHost();

                    for (int i = 0; i < _playersCount - 1; i++)
                    {
                        StartInstance("-client");
                    }
                }
                else
                {
                    StartInstance("-host");

                    NetworkManager.Singleton.StartClient();

                    for (int i = 0; i < _playersCount - 2; i++)
                    {
                        StartInstance("-client");
                    }
                }
            }
        }

        private void StartInstance(string args)
        {
            // Run the game (Process class from System.Diagnostics).
            Process proc = new Process();
            proc.StartInfo.FileName = GetGamePath();
            proc.StartInfo.Arguments = args;
            proc.Start();
        }

#if UNITY_EDITOR

        private void Build()
        {
            // Get filename.
            string[] levels = new string[] { _scenePath };

            // Build player.
            BuildPipeline.BuildPlayer(levels, GetGamePath(), BuildTarget.StandaloneWindows64, BuildOptions.None);
        }

#endif

        private string GetGamePath() => _buildPath + "/BuiltGame.exe";
    }
}
