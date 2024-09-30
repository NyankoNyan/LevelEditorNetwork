using Unity.Netcode;

namespace Level.API.Network
{
    public struct GridSettingsInfoSerialize : INetworkSerializable
    {
        public GridSettingsInfo info;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref info.id);
            serializer.SerializeValue(ref info.content);
        }

        public static implicit operator GridSettingsInfoSerialize(GridSettingsInfo value)
        {
            return new() { info = value };
        }

        public static implicit operator GridSettingsInfo(GridSettingsInfoSerialize value)
        {
            return value.info;
        }
    }
}