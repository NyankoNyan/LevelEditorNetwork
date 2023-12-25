using Unity.Netcode;
using System.Collections;

namespace Level.API.Network
{
    public struct ListWrap<T> : INetworkSerializable, IEnumerable
        where T : INetworkSerializable, new()
    {
        T[] _values;

        public ListWrap(T[] values)
        {
            _values = values;
        }

        public void NetworkSerialize<T1>(BufferSerializer<T1> serializer) where T1 : IReaderWriter
        {
            NetworkTools.ArraySerialize(serializer, ref _values);
        }

        public IEnumerator GetEnumerator() => _values.GetEnumerator();
    }
}