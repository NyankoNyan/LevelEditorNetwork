using Unity.Netcode;

namespace Level.API.Network
{
    public static class GridBoundsRectExtension
    {
        public static void SerializeValue<T>(this BufferSerializer<T> serializer, ref GridBoundsRect value)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref value.chunkFrom);
            serializer.SerializeValue(ref value.chunkTo);
        }
    }
}