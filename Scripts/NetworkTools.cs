using System.Collections.Generic;

using Unity.Netcode;


namespace Level.API.Network
{
    public static class NetworkTools
    {
        public static ClientRpcParams ResponceClient(ServerRpcParams serverRpcParams)
        {
            return new ClientRpcParams() {
                Send = new ClientRpcSendParams() {
                    TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
                }
            };
        }

        public static void NullableArraySerialize<T, B>(BufferSerializer<B> serializer, ref T[] value)
            where B : IReaderWriter
            where T : INetworkSerializable, new()
        {
            if (serializer.IsWriter && value == null) {
                throw new System.Exception("Serializable array is null");
            }

            if (serializer.IsReader) {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out bool hasValue);
                if (hasValue) {
                    reader.ReadValueSafe(out int arrLen);
                    value = new T[arrLen];
                } else {
                    value = null;
                }

            } else if (serializer.IsWriter) {
                var writer = serializer.GetFastBufferWriter();
                if (value == null) {
                    writer.WriteValueSafe(false);
                } else {
                    writer.WriteValueSafe(true);
                    writer.WriteValueSafe(value.Length);
                }
            }

            if (value != null) {
                for (int i = 0; i < value.Length; i++) {
                    serializer.SerializeValue(ref value[i]);
                }
            }
        }

        public static void ArraySerialize<T, B>(BufferSerializer<B> serializer, ref T[] value)
            where B : IReaderWriter
            where T : INetworkSerializable, new()
        {
            if (serializer.IsWriter && value == null) {
                throw new System.Exception("Serializable array is null");
            }

            if (serializer.IsReader) {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int arrLen);
                value = new T[arrLen];

            } else if (serializer.IsWriter) {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(value.Length);
            }

            for (int i = 0; i < value.Length; i++) {
                serializer.SerializeValue(ref value[i]);
            }
        }

        public static void StringArraySerialize<B>(BufferSerializer<B> serializer, ref string[] value)
            where B : IReaderWriter
        {
            if (serializer.IsWriter && value == null) {
                throw new System.Exception("Serializable array is null");
            }

            if (serializer.IsReader) {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int arrLen);
                value = new string[arrLen];

            } else if (serializer.IsWriter) {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(value.Length);
            }

            for (int i = 0; i < value.Length; i++) {
                serializer.SerializeValue(ref value[i]);
            }
        }

        public static void NullableSerialize<T, B>(BufferSerializer<B> serializer, ref T value)
            where B : IReaderWriter
            where T : class, INetworkSerializable, new()
        {
            if (serializer.IsWriter) {
                var writer = serializer.GetFastBufferWriter();
                if (value == null) {
                    writer.WriteValueSafe(false);
                } else {
                    writer.WriteValueSafe(true);
                    writer.WriteValueSafe(value);
                }
            } else if (serializer.IsReader) {
                var reader = serializer.GetFastBufferReader();
                bool hasValue;
                reader.ReadValueSafe(out hasValue);
                if (hasValue) {
                    reader.ReadValueSafe(out value);
                }
            }
        }

        public static void ListSerialize<T, B>(BufferSerializer<B> serializer, ref List<T> value)
            where B : IReaderWriter
            where T : INetworkSerializable, new()
        {
            if (serializer.IsReader) {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int count);
                if (count == -1) {
                    value = null;
                } else {
                    value = new List<T>();
                    for (int i = 0; i < count; i++) {
                        reader.ReadValueSafe(out T elem);
                        value.Add(elem);
                    }
                }

            } else if (serializer.IsWriter) {
                var writer = serializer.GetFastBufferWriter();
                if (value == null) {
                    writer.WriteValueSafe(-1);
                } else {
                    writer.WriteValueSafe(value.Count);
                    for (int i = 0; i < value.Count; i++) {
                        writer.WriteValueSafe(value[i]);
                    }
                }
            }
        }

        public delegate void SerializeMethod<T, B>(BufferSerializer<B> serializer, ref T value)
            where B : IReaderWriter
            where T : new();

        public static void ListSerialize<T, B>(BufferSerializer<B> serializer, ref List<T> value, SerializeMethod<T, B> serializeMethod)
            where B : IReaderWriter
            where T : new()
        {
            T elem = default;
            if (serializer.IsReader) {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int count);
                if (count == -1) {
                    value = null;
                } else {
                    value = new List<T>();
                    for (int i = 0; i < count; i++) {
                        serializeMethod(serializer, ref elem);
                        value.Add(elem);
                    }
                }

            } else if (serializer.IsWriter) {
                var writer = serializer.GetFastBufferWriter();
                if (value == null) {
                    writer.WriteValueSafe(-1);
                } else {
                    writer.WriteValueSafe(value.Count);
                    for (int i = 0; i < value.Count; i++) {
                        elem = value[i];
                        serializeMethod(serializer, ref elem);
                    }
                }
            }
        }

        public static void SerializeValue<T, B>(this BufferSerializer<B> serializer, ref List<T> value, bool ignoreNull)
            where B : IReaderWriter
            where T : INetworkSerializable, new()
        {
            if (!ignoreNull && serializer.IsWriter && value == null) {
                throw new System.Exception("Serializable array is null");
            }

            if (serializer.IsReader) {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int arrLen);
                value = new(arrLen);
                for (int i = 0; i < arrLen; i++) {
                    reader.ReadValueSafe(out T elem);
                    value[i] = elem;
                }
            } else if (serializer.IsWriter) {
                var writer = serializer.GetFastBufferWriter();
                if (value == null) {
                    writer.WriteValueSafe(0);
                } else {
                    writer.WriteValueSafe(value.Count);
                    for (int i = 0; i < value.Count; i++) {
                        writer.WriteValueSafe(value[i]);
                    }
                }
            }
        }
    }
}