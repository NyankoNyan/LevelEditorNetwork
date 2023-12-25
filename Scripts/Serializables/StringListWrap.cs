using Unity.Netcode;

namespace Level.API.Network
{
    public struct StringListWrap : INetworkSerializable
    {
        private string[] _values;

        public string[] Values => _values;

        public StringListWrap(string[] values)
        {
            _values = values;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader) {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int size);
                _values = new string[size];
                for (int i = 0; i < size; i++) {
                    reader.ReadValueSafe(out _values[i]);
                }
            } else if (serializer.IsWriter) {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(_values.Length);
                for (int i = 0; i < _values.Length; i++) {
                    writer.WriteValueSafe(_values[i]);
                }
            }
        }
    }
}