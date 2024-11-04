using System;
using System.IO;

using LevelNet.Data;
using LevelNet.Netcode;

using Unity.Netcode;
using Unity.SharpZipLib.GZip;

using UnityEngine;
using UnityEngine.Assertions;

namespace LevelNet.Tests
{
    public static class LevelNetDataTests
    {
        public static void TestSendData()
        {
            GameBlocksState state = new() {
                size = new Vector2Int(2, 2),
                colors = new Color[4] { Color.red, Color.blue, Color.green, Color.white }
            };

            DataTransferPoint.Instance.SendDataToServerRpc(new(state));
        }

        public static void TestContainerChange()
        {
            GameBlocksState state = new() {
                size = new Vector2Int(2, 2),
                colors = new Color[4] { Color.red, Color.blue, Color.green, Color.white }
            };

            SyncDataContainer container = new SyncDataContainer();
            container.SetupData(state);

            container.ChangeData("Color/0", Color.white);
            container.ChangeData("Size", new Vector2Int(2, 1));
            container.ChangeData("Color", new Color[2] { Color.red, Color.white });

            Assert.AreEqual(((GameBlocksState)container.ClientState).size, new Vector2Int(2, 1));
            Assert.AreEqual(((GameBlocksState)container.ClientState).colors[0], Color.red);
            Assert.AreEqual(((GameBlocksState)container.ClientState).colors[1], Color.white);
        }

        public static void TestPartialSerialization()
        {
            FastBufferWriter writer = new(1024, Unity.Collections.Allocator.Temp, 65536);
            GameBlocksState state = new() {
                size = new Vector2Int(2, 2),
                colors = new Color[4] { Color.red, Color.blue, Color.green, Color.white }
            };

            SyncDataContainer container = new SyncDataContainer();
            container.SetupData(state);
            container.ChangeData("Color/0", Color.white);

            NetcodeStaticSerializer.Instance.PartialSerialize(container.ClientState, container.DirtyFlags, writer);

            byte[] data = writer.ToArray();
            string dataRepr = string.Join(", ", data);

            Assert.AreEqual(dataRepr, "1, 1, 2, 0, 1, 0, 0, 128, 63, 0, 0, 128, 63, 0, 0, 128, 63, 0, 0, 128, 63");

            SyncDataContainer container2 = new SyncDataContainer();
            container2.SetupData(MyDynamics.CreateCopy(state));

            FastBufferReader reader = new(data, Unity.Collections.Allocator.Temp, data.Length, 0);
            NetcodeStaticSerializer.Instance.PartialDeserialize(container2.ServerState, container2.DirtyFlags, reader);

            Assert.AreEqual(container.ClientState.ToString(), container2.ServerState.ToString());
        }

        public static void TestZipCompression()
        {
            FastBufferWriter writer = new(1024, Unity.Collections.Allocator.Temp, 65536);
            GameBlocksState state = new() {
                size = new Vector2Int(2, 2),
                colors = new Color[4] { Color.red, Color.blue, Color.green, Color.white }
            };

            NetcodeStaticSerializer.Instance.Serialize(state, writer);
            byte[] data = writer.ToArray();

            MemoryStream ms = new MemoryStream();
            using (GZipOutputStream outStream = new GZipOutputStream(ms)) {
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

            using (GZipInputStream inputStream = new(ms)) {
                while ((len = inputStream.Read(buffer)) > 0) {
                    data = new byte[len];
                    Array.Copy(buffer, data, len);
                }
            }

            ms.Close();

            Debug.Log(data.Length);
        }
    }
}