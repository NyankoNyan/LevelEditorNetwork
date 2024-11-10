using System;
using System.Collections;

namespace LevelNet.Data
{
    public class DirtyArray : IDirtyCollection
    {
        private bool _fullChange;
        private BitArray _elements;

        public bool IsFullChanged => _fullChange;
        public BitArray Flags {
            get => _elements;
            set {
                _elements = value;
            }
        }

        public void ApplyChanges()
        {
            _elements.SetAll(false);
        }

        public float GetDirtnessRatio()
        {
            int trues = 0;
            for(int i=0;i< _elements.Count; i++) {
                if (_elements[i]) {
                    trues++;
                }
            }
            return trues / (float)_elements.Count;
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
