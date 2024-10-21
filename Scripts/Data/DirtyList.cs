using System;
using System.Collections.Generic;

namespace LevelNet.Data
{
    public class DirtyList : IDirtyCollection
    {
        private List<byte> _statuses = new();

        public void ApplyChanges()
        {
            throw new NotImplementedException();
        }

        public void RejectChanges()
        {
            throw new NotImplementedException();
        }

        internal void Init(object v)
        {
            throw new NotImplementedException();
        }
    }
}
