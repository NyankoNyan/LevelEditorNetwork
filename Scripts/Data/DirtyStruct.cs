using System;
using System.Collections;

namespace LevelNet.Data
{
    public class DirtyStruct : IDirtyCollection
    {
        private BitArray _dirtyFields;
        private DirtyArray[] _arrays;
        private DirtyList[] _lists;

        public DirtyStruct(Type type)
        {
            var mapping = FieldsMapping.Create(type);

            _dirtyFields = new(mapping.FieldsCount);
            if (mapping.ArraysCount > 0)
            {
                _arrays = new DirtyArray[mapping.ArraysCount];
                for (int i = 0; i < _arrays.Length; i++)
                {
                    _arrays[i] = new();
                }
            }
            if (mapping.ListsCount > 0)
            {
                _lists = new DirtyList[mapping.ListsCount];
                for (int i = 0; i < _lists.Length; i++)
                {
                    _lists[i] = new();
                }
            }
        }

        public void RejectChanges()
        {
            _dirtyFields.Xor(_dirtyFields);
            if (_arrays != null)
            {
                foreach (var array in _arrays)
                {
                    array.RejectChanges();
                }
            }
            if (_lists != null)
            {
                foreach (var list in _lists)
                {
                    list.RejectChanges();
                }
            }
        }

        public void ApplyChanges()
        {
            _dirtyFields.Xor(_dirtyFields);
            if (_arrays != null)
            {
                foreach (var array in _arrays)
                {
                    array.ApplyChanges();
                }
            }
            if (_lists != null)
            {
                foreach (var list in _lists)
                {
                    list.ApplyChanges();
                }
            }
        }

        public void SetDirty(int fieldIndex)
        {
            _dirtyFields[fieldIndex] = true;
        }

        public DirtyArray GetArray(int index) => _arrays[index];

        internal void Init(object data)
        {
            var mapping = FieldsMapping.Create(data.GetType());
            foreach (var info in mapping.GetInfos())
            {
                if (info.IsArray)
                {
                    _arrays[info.arrayIndex].Init(info.fieldInfo.GetValue(data));
                }
                else if (info.IsList)
                {
                    _lists[info.arrayIndex].Init(info.fieldInfo.GetValue(data));
                }
            }
        }
    }
}
