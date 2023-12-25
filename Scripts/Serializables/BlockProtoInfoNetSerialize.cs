using Unity.Netcode;

namespace Level.API.Network
{
    public struct BlockProtoInfoNetSerialize : INetworkSerializable
    {
        public uint id;
        public BlockProtoSettingsNetSerialize content;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref content);
        }

        public static implicit operator BlockProtoInfo(BlockProtoInfoNetSerialize value)
        {
            return new BlockProtoInfo() {
                id = value.id,
                content = value.content
            };
        }

        public static implicit operator BlockProtoInfoNetSerialize(BlockProtoInfo value)
        {
            return new BlockProtoInfoNetSerialize() {
                id = value.id,
                content = value.content
            };
        }
    }
}