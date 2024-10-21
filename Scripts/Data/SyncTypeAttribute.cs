using System;

namespace LevelNet.Data
{
    public class SyncTypeAttribute : Attribute
    {
        private readonly byte _id;

        public byte Id => _id;

        public SyncTypeAttribute(byte id)
        {
            _id = id;
        }
    }
}
