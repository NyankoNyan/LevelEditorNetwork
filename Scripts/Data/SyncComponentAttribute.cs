using System;

namespace LevelNet.Data
{
    public class SyncComponentAttribute : Attribute
    {
        public readonly string name;
        public readonly bool partial;

        public SyncComponentAttribute(string name = null, bool partial = false)
        {
            this.name = name;
            this.partial = partial;
        }
    }
}
