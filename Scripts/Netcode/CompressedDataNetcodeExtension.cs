using Unity.Netcode;

namespace LevelNet.Netcode
{
    public static class CompressedDataNetcodeExtension
    {
        public static void ReadValueSafe(this FastBufferReader reader, out CompressedData data)
        {
            data = new(reader);
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in CompressedData data)
        {
            data.Write(writer);
        }
    }
}