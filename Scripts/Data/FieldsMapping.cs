using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine.Assertions;

namespace LevelNet.Data
{
    internal struct MapFieldInfo
    {
        public FieldInfo fieldInfo;
        public int fieldIndex;
        public int arrayIndex;
        public int listIndex;
        public string name;

        public readonly bool IsList => listIndex >= 0;
        public readonly bool IsArray => arrayIndex >= 0;
        public readonly bool IsPartial => IsList || IsArray;
    }

    internal class FieldsMapping
    {
        private static Dictionary<Type, FieldsMapping> _mappingsBuffer = new();

        public int ArraysCount => _arraysCount;
        public int FieldsCount => _fieldsCount;
        public int ListsCount => _listsCount;

        private readonly int _arraysCount = 0;
        private readonly int _fieldsCount = 0;
        private readonly int _listsCount = 0;
        private readonly List<MapFieldInfo> _infos = new();
        private readonly Dictionary<string, int> _nameIndex = new();

        private FieldsMapping(Type type)
        {
            Assert.IsTrue(type.IsStruct());

            if (type.GetCustomAttribute<SyncTypeAttribute>() == null)
            {
                throw new Exception($"{type.Name} is not SyncType");
            }

            foreach (var fieldInfo in type.GetFields())
            {
                var syncComponent = fieldInfo.GetCustomAttribute<SyncComponentAttribute>();
                if (syncComponent == null)
                {
                    continue;
                }
                MapFieldInfo info = new()
                {
                    fieldInfo = fieldInfo,
                    fieldIndex = _infos.Count,
                    arrayIndex = -1,
                    listIndex = -1,
                    name = syncComponent.name
                };
                if (syncComponent.partial)
                {
                    if (fieldInfo.FieldType.IsSZArray)
                    {
                        info.arrayIndex = _arraysCount++;
                    }
                    else if (MyDynamics.IsList(fieldInfo.FieldType))
                    {
                        info.listIndex = _listsCount++;
                    }
                    else
                    {
                        throw new Exception($"{fieldInfo.Name} of {type.Name} can't be partial");
                    }
                }

                _infos.Add(info);
                _nameIndex.Add(syncComponent.name, _fieldsCount++);
            }
        }

        public static FieldsMapping Create(Type type)
        {
            FieldsMapping mapping;
            if (!_mappingsBuffer.TryGetValue(type, out mapping))
            {
                mapping = new(type);
                _mappingsBuffer.Add(type, mapping);
            }
            return mapping;
        }

        public MapFieldInfo GetInfo(string name) => _infos[_nameIndex[name]];
        public IEnumerable<MapFieldInfo> GetInfos() => _infos;
    }
}