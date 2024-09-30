using Unity.Netcode;

namespace Level.API.Network
{
    public struct ContentChangeInfo<TData> : INetworkSerializable
        where TData : INetworkSerializable, new()
    {
        public TData[] add;
        public TData[] change;
        public TData[] remove;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            NetworkTools.NullableArraySerialize(serializer, ref add);
            NetworkTools.NullableArraySerialize(serializer, ref change);
            NetworkTools.NullableArraySerialize(serializer, ref remove);
        }
    }
}