using System;
using System.Collections;

namespace LevelNet.Data
{
    public class DirtyArray : IDirtyCollection
    {
        private bool _fullChange;
        private BitArray _elements;

        public void ApplyChanges()
        {
            throw new NotImplementedException();
        }

        public void RejectChanges()
        {
            throw new NotImplementedException();
        }

        internal void FullChange()
        {
            _fullChange = true;
        }

        internal void Init(object data)
        {
            var array = (Array)data;
            if (_elements != null && _elements.Length == array.Length)
            {
                _elements.Xor(_elements);
            }
            else
            {
                _elements = new BitArray(array.Length);
            }
        }

        internal void SetDirty(int index)
        {
            _elements[index] = true;
        }
    }
}
