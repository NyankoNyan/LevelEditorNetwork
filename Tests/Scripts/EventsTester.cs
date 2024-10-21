using System;
using System.IO;
using LevelNet.Data;
using LevelNet.Netcode;
using Unity.Netcode;
using Unity.SharpZipLib.GZip;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LevelNet.Tests
{
    public class EventsTester : MonoBehaviour
    {
        [SerializeField] private InputAction _action1 = new("action1", binding: "<Keyboard>/1");
        [SerializeField] private InputAction _action2 = new("action2", binding: "<Keyboard>/2");
        [SerializeField] private InputAction _action3 = new("action3", binding: "<Keyboard>/3");
        [SerializeField] private InputAction _action4 = new("action4", binding: "<Keyboard>/4");
        [SerializeField] private InputAction _action5 = new("action5", binding: "<Keyboard>/5");

        private void Start()
        {
            _action1.Enable();
            _action2.Enable();
            _action3.Enable();
            _action4.Enable();
            _action5.Enable();

            _action1.performed += _action1_performed;
            _action2.performed += _action2_performed;
            _action3.performed += _action3_performed;
            _action4.performed += _action4_performed;
            _action5.performed += _action5_performed;
        }

        private void _action1_performed(InputAction.CallbackContext obj)
        {
            DataTransferPoint.Instance.SendDataToServerRpc(new(Time.time));
        }

        private void _action2_performed(InputAction.CallbackContext obj)
        {
            GameBlocksState state = new()
            {
                size = new Vector2Int(2, 2),
                colors = new Color[4] { Color.red, Color.blue, Color.green, Color.white }
            };

            DataTransferPoint.Instance.SendDataToServerRpc(new(state));
        }

        private void _action3_performed(InputAction.CallbackContext obj)
        {
            FastBufferWriter writer = new(1024, Unity.Collections.Allocator.Temp, 65536);
            GameBlocksState state = new()
            {
                size = new Vector2Int(2, 2),
                colors = new Color[4] { Color.red, Color.blue, Color.green, Color.white }
            };

            NetcodeStaticSerializer.Instance.Serialize(state, writer);
            byte[] data = writer.ToArray();

            MemoryStream ms = new MemoryStream();
            using (GZipOutputStream outStream = new GZipOutputStream(ms))
            {
                outStream.Write(data, 0, data.Length);
            }

            byte[] zipData = ms.ToArray();
            ms.Close();

            Debug.Log(data.Length);
            Debug.Log(zipData.Length);

            ms = new MemoryStream(zipData);

            byte[] buffer = new byte[1024];
            int len = 0;
            data = null;

            using (GZipInputStream inputStream = new(ms))
            {
                while ((len = inputStream.Read(buffer)) > 0)
                {
                    data = new byte[len];
                    Array.Copy(buffer, data, len);
                }
            }

            //data = ms.ToArray();
            ms.Close();

            Debug.Log(data.Length);
        }

        private void _action4_performed(InputAction.CallbackContext obj)
        {
            GameBlocksState state = new()
            {
                size = new Vector2Int(2, 2),
                colors = new Color[4] { Color.red, Color.blue, Color.green, Color.white }
            };

            SyncDataContainer container = new SyncDataContainer();
            container.SetupData(state);

            container.ChangeData("Color/0", Color.white);
            container.ChangeData("Size", new Vector2Int(2, 1));
            container.ChangeData("Color", new Color[2] { Color.red, Color.white });
        }

        private void _action5_performed(InputAction.CallbackContext obj)
        {
        }
    }
}