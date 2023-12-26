using Unity.Netcode;

namespace Level.API.Network
{
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