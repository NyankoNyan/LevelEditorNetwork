using Unity.Netcode;

namespace Level.API.Network
{
    public struct BlockProtoSettingsNetSerialize : INetworkSerializable
    {
        public BlockProtoSettings settings;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref settings.name);
            serializer.SerializeValue(ref settings.formFactor);
            serializer.SerializeValue(ref settings.layerTag);
            serializer.SerializeValue(ref settings.size);
            serializer.SerializeValue(ref settings.lockXZ);
        }

        public static implicit operator BlockProtoSettingsNetSerialize(BlockProtoSettings value)
        {
            return new BlockProtoSettingsNetSerialize() { settings = value };
        }
        public static implicit operator BlockProtoSettings(BlockProtoSettingsNetSerialize value)
        {
            return value.settings;
        }
    }
}