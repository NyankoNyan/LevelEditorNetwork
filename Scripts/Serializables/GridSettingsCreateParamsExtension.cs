using Unity.Netcode;

namespace Level.API.Network
{
    public static class GridSettingsCreateParamsExtension
    {
        public static void SerializeValue<T>(this BufferSerializer<T> serializer, ref GridSettingsCreateParams value)
           where T : IReaderWriter
        {
            serializer.SerializeValue(ref value.name);
            serializer.SerializeValue(ref value.chunkSize);
            serializer.SerializeValue(ref value.cellSize);
            serializer.SerializeValue(ref value.formFactor);
            serializer.SerializeValue(ref value.hasBounds);
            serializer.SerializeValue(ref value.bounds);
            NetworkTools.ListSerialize(serializer, ref value.layers, DataLayerSettingsExtension.SerializeValue);
        }
    }
}