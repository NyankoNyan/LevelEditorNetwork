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

    public struct GridSettingsNetSerialize : INetworkSerializable
    {
        public GridSettingsCreateParams settings;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref settings.name);
            serializer.SerializeValue(ref settings.chunkSize);
            serializer.SerializeValue(ref settings.cellSize);
            serializer.SerializeValue(ref settings.formFactor);
            serializer.SerializeValue(ref settings.hasBounds);
            serializer.SerializeValue(ref settings.bounds);
            serializer.SerializeValue<DataLayerSettings, T>(ref settings.layers, false);
        }
    }

    public static class GridBoundsRectExtension
    {
        public static void SerializeValue<T>(this BufferSerializer<T> serializer, ref GridBoundsRect value)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref value.chunkFrom);
            serializer.SerializeValue(ref value.chunkTo);
        }
    }

    public static class DataLayerSettingsExtension
    {
        public static void SerializeValue<T>(this BufferSerializer<T> serializer, ref DataLayerSettings value)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref value.layerType);
            serializer.SerializeValue(ref value.chunkSize);
            serializer.SerializeValue(ref value.tag);
            serializer.SerializeValue(ref value.hasViewLayer);
        }
    }
}