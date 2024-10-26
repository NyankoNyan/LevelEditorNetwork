using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using Unity.SharpZipLib.GZip;

namespace LevelNet.Netcode
{
    public class CompressedData
    {
        private object _data;

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
            if (_data != null)
            {
                return _data.ToString();
            }
            else
            {
                return "null";
            }
        }

        public void Read(FastBufferReader packedReader)
        {
            // Read zip data
            packedReader.ReadValueSafe(out int zipDataSize);
            byte[] zipData = new byte[zipDataSize];
            packedReader.ReadBytesSafe(ref zipData, zipDataSize);

            byte[] unpackedData = UnpackData(zipData);
            zipData = null;

            // Deserialize data as usuial
            FastBufferReader unpackedReader = new(unpackedData, Unity.Collections.Allocator.Temp, unpackedData.Length, 0);
            _data = NetcodeStaticSerializer.Instance.Deserialize(unpackedReader);
        }

        private static byte[] UnpackData(byte[] zipData)
        {
            const int bufferSize = 1024;

            int readLen = 0;
            int totalReadSize = 0;
            byte[] buffer = new byte[bufferSize];
            List<byte[]> unpackedParts = new();

            // Unzip data
            using (var ms = new MemoryStream(zipData))
            {
                using (GZipInputStream inputStream = new(ms))
                {
                    while ((readLen = inputStream.Read(buffer)) > 0)
                    {
                        if (readLen == bufferSize)
                        {
                            unpackedParts.Add(buffer);
                        }
                        else
                        {
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
            foreach (var dataPart in unpackedParts)
            {
                Array.Copy(dataPart, 0, unpackedData, currentSize, dataPart.Length);
                currentSize += dataPart.Length;
            }

            return unpackedData;
        }

        public void Write(FastBufferWriter packedWriter)
        {
            // Serialize data without compression
            FastBufferWriter unpackedWriter = new(1024, Unity.Collections.Allocator.Temp, 65536);
            NetcodeStaticSerializer.Instance.Serialize(_data, unpackedWriter);
            byte[] unpackedData = unpackedWriter.ToArray();
            byte[] packedData = null;

            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipOutputStream outStream = new GZipOutputStream(ms))
                {
                    outStream.Write(unpackedData, 0, unpackedData.Length);
                }
                packedData = ms.ToArray();
            }

            packedWriter.WriteValueSafe(packedData.Length);
            packedWriter.WriteBytesSafe(packedData);
        }
    }
}