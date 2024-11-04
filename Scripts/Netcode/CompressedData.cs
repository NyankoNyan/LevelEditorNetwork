using System;
using System.Collections.Generic;
using System.IO;

using LevelNet.Data;

using Unity.Netcode;
using Unity.SharpZipLib.GZip;

namespace LevelNet.Netcode
{
    public class CompressedData
    {
        private object _data;

        public object Data => _data;

        public CompressedData(object data)
        {
            _data = data;
        }

        public CompressedData(FastBufferReader reader)
        {
            Read(reader);
        }

        public override string ToString()
        {
            if (_data != null) {
                return _data.ToString();
            } else {
                return "null";
            }
        }

        public void Read(FastBufferReader packedReader)
        {
            // Read zip data
            packedReader.ReadValueSafe(out int zipDataSize);
            byte[] zipData = new byte[zipDataSize];
            packedReader.ReadBytesSafe(ref zipData, zipDataSize);

            byte[] unpackedData = Compress.Unpack(zipData);
            zipData = null;

            // Deserialize data as usuial
            FastBufferReader unpackedReader = new(unpackedData, Unity.Collections.Allocator.Temp, unpackedData.Length, 0);
            _data = NetcodeStaticSerializer.Instance.Deserialize(unpackedReader);
        }

        public void Write(FastBufferWriter packedWriter)
        {
            FastBufferWriter unpackedWriter = new(1024, Unity.Collections.Allocator.Temp, 65536);
            NetcodeStaticSerializer.Instance.Serialize(_data, unpackedWriter);
            byte[] unpackedData = unpackedWriter.ToArray();
            byte[] packedData = Compress.Pack(unpackedData);

            packedWriter.WriteValueSafe(packedData.Length);
            packedWriter.WriteBytesSafe(packedData);
        }
    }

    public class CompressedContainerChanges
    {
        private SyncDataContainer _container;
        private bool _asServer;
        private byte[] _unpackedData;

        private CompressedContainerChanges()
        { }

        public static CompressedContainerChanges CreateForReading(FastBufferReader packedReader)
        {
            CompressedContainerChanges data = new();
            data.Read(packedReader);
            return data;
        }

        public static CompressedContainerChanges CreateForWriting(SyncDataContainer container, bool asServer)
        {
            return new() {
                _container = container,
                _asServer = asServer
            };
        }

        public void SyncContainer(SyncDataContainer container, bool asServer)
        {
            // Deserialize data as usuial
            FastBufferReader unpackedReader = new(_unpackedData, Unity.Collections.Allocator.Temp, _unpackedData.Length, 0);
            NetcodeStaticSerializer.Instance.PartialDeserialize(asServer ? container.ServerState : container.ClientState, container.DirtyFlags, unpackedReader);
        }

        private void Read(FastBufferReader packedReader)
        {
            // Read zip data
            packedReader.ReadValueSafe(out int zipDataSize);
            byte[] zipData = new byte[zipDataSize];
            packedReader.ReadBytesSafe(ref zipData, zipDataSize);

            _unpackedData = Compress.Unpack(zipData);
        }

        public void Write(FastBufferWriter packedWriter)
        {
            FastBufferWriter unpackedWriter = new(1024, Unity.Collections.Allocator.Temp, 65536);
            NetcodeStaticSerializer.Instance.PartialSerialize(_asServer ? _container.ServerState : _container.ClientState, _container.DirtyFlags, unpackedWriter);
            byte[] unpackedData = unpackedWriter.ToArray();
            byte[] packedData = Compress.Pack(unpackedData);

            packedWriter.WriteValueSafe(packedData.Length);
            packedWriter.WriteBytesSafe(packedData);
        }
    }

    public static class CompressedContainerChangesNetcodeExtension
    {
        public static void ReadValueSafe(this FastBufferReader reader, out CompressedContainerChanges data)
        {
            data = CompressedContainerChanges.CreateForReading(reader);
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in CompressedContainerChanges data)
        {
            data.Write(writer);
        }
    }

    public static class Compress
    {
        public static byte[] Unpack(byte[] zipData)
        {
            const int bufferSize = 1024;

            int readLen = 0;
            int totalReadSize = 0;
            byte[] buffer = new byte[bufferSize];
            List<byte[]> unpackedParts = new();

            // Unzip data
            using (var ms = new MemoryStream(zipData)) {
                using (GZipInputStream inputStream = new(ms)) {
                    while ((readLen = inputStream.Read(buffer)) > 0) {
                        if (readLen == bufferSize) {
                            unpackedParts.Add(buffer);
                        } else {
                            byte[] lastPart = new byte[readLen];
                            Array.Copy(buffer, lastPart, readLen);
                            unpackedParts.Add(lastPart);
                        }
                        totalReadSize += readLen;
                    }
                }
            }

            // Merge unziped data parts
            byte[] unpackedData = new byte[totalReadSize];
            int currentSize = 0;
            foreach (var dataPart in unpackedParts) {
                Array.Copy(dataPart, 0, unpackedData, currentSize, dataPart.Length);
                currentSize += dataPart.Length;
            }

            return unpackedData;
        }

        public static byte[] Pack(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream()) {
                using (GZipOutputStream outStream = new GZipOutputStream(ms)) {
                    outStream.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }
    }
}