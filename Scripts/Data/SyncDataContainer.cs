using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine.Assertions;

namespace LevelNet.Data
{
    public class SyncDataContainer : IData
    {
        public int Id => _id;
        private int _id;
        private object _serverStateData;
        private object _clientStateData;
        private DirtyFlags _dirtyFlags;

        public event IData.OnDataChangeDelegate OnDataChange;

        public event IData.OnDestroyDelegate OnDestroy;

        internal DirtyFlags DirtyFlags => _dirtyFlags;
        internal object ClientState => _clientStateData;
        internal object ServerState => _serverStateData;

        public object Current => ClientState;

        internal SyncDataContainer(int id)
        {
            _id = id;
        }

        public SyncDataContainer()
        {
        }

        public void SetupData(object data)
        {
            _serverStateData = data;
            _clientStateData = MyDynamics.CreateCopy(data);
            _dirtyFlags = new DirtyFlags(data);
        }

        public void UpdateClientData(object data)
        {
            _clientStateData = data;
            _dirtyFlags.ApplyChanges();
        }

        public void ChangeData(string path, object value)
        {
            if (String.IsNullOrEmpty(path)) {
                throw new NotImplementedException();
            } else {
                string[] pathParts = path.Split('/');
                if (ChangeManagedStruct(pathParts, 0, _clientStateData, value, _dirtyFlags.Root)) {
                    _dirtyFlags.SetDirty();
                }
            }
            NetEventsFabric.Create().RegChangedContainer(Id);
        }

        public void SyncClientState()
        {
            var changes = CollectServerChanges();
            OnDataChange?.Invoke(new() {
                dataChangeElements = changes
            });
            DirtyFlags.ApplyChanges();
        }

        private List<DataChangeElement> CollectServerChanges()
        {
            List<DataChangeElement> changes = new();
            if (_dirtyFlags.IsDirty) {
                CollectStructChanges(_dirtyFlags.Root, ServerState, changes, "");
            } else {
                throw new Exception("Missing changes???");
            }
            return changes;
        }

        private void CollectStructChanges(DirtyStruct dirtyStruct, object serverPart, List<DataChangeElement> changes, string prevPath)
        {
            var fieldsMapping = FieldsMapping.Create(serverPart.GetType());
            foreach (var info in fieldsMapping.GetInfos()) {
                if (!dirtyStruct.DirtyFields[info.fieldIndex])
                    continue;
                if (info.IsPartial) {
                    if (info.IsArray) {
                        var dirtyArray = dirtyStruct.GetArray(info.arrayIndex);
                        if (!dirtyArray.IsFullChanged) {
                            var array = (Array)info.fieldInfo.GetValue(serverPart);
                            CollectArrayChanges(dirtyArray, array, changes, prevPath + info.name + '/');
                            continue;
                        }
                    } else {
                        throw new NotImplementedException();
                    }
                }
                changes.Add(new DataChangeElement() {
                    name = prevPath + info.name,
                    value = info.fieldInfo.GetValue(serverPart)
                });
            }
        }

        private void CollectArrayChanges(DirtyArray dirtyArray, Array array, List<DataChangeElement> changes, string prevPath)
        {
            Assert.IsNotNull(array);

            for (int i = 0; i < dirtyArray.Flags.Count; i++) {
                if (!dirtyArray.Flags[i])
                    continue;
                changes.Add(new() {
                    name = prevPath + i.ToString(),
                    value = array.GetValue(i)
                });
            }
        }

        private bool ChangeManagedStruct(string[] pathParts, int currentPart, object currentStruct, object value, DirtyStruct dirtyFlags)
        {
            Type type = currentStruct.GetType();
            var mapping = FieldsMapping.Create(type);
            string fieldCaption = pathParts[currentPart];
            MapFieldInfo info;
            try {
                info = mapping.GetInfo(fieldCaption);
            } catch (Exception e) {
                throw new Exception($"Unknown field {fieldCaption} in {type.Name}", e);
            }

            if (info.IsPartial) {
                if (info.IsArray) {
                    Type elementType = info.fieldInfo.FieldType.GetElementType();

                    FieldsMapping elementMapping;
                    try {
                        elementMapping = FieldsMapping.Create(elementType);
                    } catch {
                        if (ChangeUnmanagedArray(pathParts, currentPart + 1, value, dirtyFlags.GetArray(info.arrayIndex), currentStruct, info.fieldInfo)) {
                            dirtyFlags.SetDirty(info.fieldIndex);
                            return true;
                        } else {
                            return false;
                        }
                    }
                    if (ChangeManagedArray()) {
                        dirtyFlags.SetDirty(info.fieldIndex);
                        return true;
                    } else {
                        return false;
                    }
                } else {
                    throw new NotImplementedException();
                }
            } else {
                object currentValue = info.fieldInfo.GetValue(currentStruct);
                if (!currentValue.Equals(value)) {
                    if (value is ICloneable clonable) {
                        info.fieldInfo.SetValue(currentStruct, clonable.Clone());
                    } else {
                        info.fieldInfo.SetValue(currentStruct, value);
                    }
                    dirtyFlags.SetDirty(info.fieldIndex);
                    return true;
                } else {
                    return false;
                }
            }
        }

        private bool ChangeManagedArray()
        {
            throw new NotImplementedException();
        }

        private bool ChangeUnmanagedArray(string[] pathParts, int currentPart, object value, DirtyArray dirtyArray, object currentStruct, FieldInfo arrayFieldInfo)
        {
            if (currentPart >= pathParts.Length) {
                //full copy
                var array = (Array)value;
                arrayFieldInfo.SetValue(currentStruct, array.Clone());
                dirtyArray.FullChange();
                return true;
            } else if (currentPart == pathParts.Length - 1) {
                var locArray = (Array)arrayFieldInfo.GetValue(currentStruct);
                if (int.TryParse(pathParts[currentPart], out int index)) {
                    if (index < 0 || index >= locArray.Length) {
                        throw new Exception($"Out of bounds  array index {index}");
                    } else {
                        object currentValue = locArray.GetValue(index);
                        if (!currentValue.Equals(value)) {
                            locArray.SetValue(value, index);
                            dirtyArray.SetDirty(index);
                            return true;
                        } else {
                            return false;
                        }
                    }
                } else {
                    throw new Exception($"{pathParts[currentPart]} is not a number");
                }
            } else {
                throw new Exception($"Can't parse path after {pathParts[currentPart]}");
            }
        }

        public void ChangeRequest(string name, object value) => ChangeData(name, value);

        public void Destroy()
        {
            _serverStateData = null;
            _clientStateData = null;
            _dirtyFlags = null;
            OnDataChange = null;
            OnDestroy?.Invoke();
            OnDestroy = null;
        }
    }
}