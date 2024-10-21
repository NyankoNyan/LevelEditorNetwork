using System;
using System.Reflection;

namespace LevelNet.Data
{
    public class SyncDataContainer
    {
        public int Id => _id;
        private int _id;
        private SyncUpdateRule _updateRule;
        private object _serverStateData;
        private object _clientStateData;
        private DirtyFlags _dirtyFlags;

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

        public void ChangeData(string path, object value)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new NotImplementedException();
            }
            else
            {
                string[] pathParts = path.Split('/');
                if (ChangeManagedStruct(pathParts, 0, _clientStateData, value, _dirtyFlags.Root))
                {
                    _dirtyFlags.SetDirty();
                }
            }
        }

        private bool ChangeManagedStruct(string[] pathParts, int currentPart, object currentStruct, object value, DirtyStruct dirtyFlags)
        {
            Type type = currentStruct.GetType();
            var mapping = FieldsMapping.Create(type);
            string fieldCaption = pathParts[currentPart];
            MapFieldInfo info;
            try
            {
                info = mapping.GetInfo(fieldCaption);
            }
            catch (Exception e)
            {
                throw new Exception($"Unknown field {fieldCaption} in {type.Name}", e);
            }

            if (info.IsPartial)
            {
                if (info.IsArray)
                {
                    Type elementType = info.fieldInfo.FieldType.GetElementType();

                    FieldsMapping elementMapping;
                    try
                    {
                        elementMapping = FieldsMapping.Create(elementType);
                    }
                    catch
                    {
                        if (ChangeUnmanagedArray(pathParts, currentPart + 1, value, dirtyFlags.GetArray(info.arrayIndex), currentStruct, info.fieldInfo))
                        {
                            dirtyFlags.SetDirty(info.fieldIndex);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    if (ChangeManagedArray())
                    {
                        dirtyFlags.SetDirty(info.fieldIndex);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                object currentValue = info.fieldInfo.GetValue(currentStruct);
                if (!currentValue.Equals(value))
                {
                    if (value is ICloneable clonable)
                    {
                        info.fieldInfo.SetValue(currentStruct, clonable.Clone());
                    }
                    else
                    {
                        info.fieldInfo.SetValue(currentStruct, value);
                    }
                    dirtyFlags.SetDirty(info.fieldIndex);
                    return true;
                }
                else
                {
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
            if (currentPart >= pathParts.Length)
            {
                //full copy
                var array = (Array)value;
                arrayFieldInfo.SetValue(currentStruct, array.Clone());
                dirtyArray.FullChange();
                return true;
            }
            else if (currentPart == pathParts.Length - 1)
            {
                var locArray = (Array)arrayFieldInfo.GetValue(currentStruct);
                if (int.TryParse(pathParts[currentPart], out int index))
                {
                    if (index < 0 || index >= locArray.Length)
                    {
                        throw new Exception($"Out of bounds  array index {index}");
                    }
                    else
                    {
                        object currentValue = locArray.GetValue(index);
                        if (!currentValue.Equals(value))
                        {
                            locArray.SetValue(value, index);
                            dirtyArray.SetDirty(index);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    throw new Exception($"{pathParts[currentPart]} is not a number");
                }
            }
            else
            {
                throw new Exception($"Can't parse path after {pathParts[currentPart]}");
            }
        }
    }
}
