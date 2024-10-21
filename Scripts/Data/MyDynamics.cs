using System;
using System.Collections.Generic;

namespace LevelNet.Data
{
    public static class MyDynamics
    {
        public static object CreateCopy(object src)
        {
            if (src is ICloneable clonable)
            {
                return clonable.Clone();
            }
            else
            {
                Type type = src.GetType();
                object tgt = Activator.CreateInstance(type);

                foreach (var fieldInfo in type.GetFields())
                {
                    if (fieldInfo.FieldType.IsByRef)
                    {
                        if (fieldInfo.FieldType.IsSZArray)
                        {
                            var elementType = fieldInfo.FieldType.GetElementType();
                            if (elementType.IsByRef)
                            {
                                throw new NotImplementedException();
                            }
                            else
                            {
                                var array = (Array)fieldInfo.GetValue(src);
                                fieldInfo.SetValue(tgt, array.Clone());
                            }
                        }
                        else if (MyDynamics.IsList(fieldInfo.FieldType))
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            throw new Exception($"Can't create deep copy of field {fieldInfo.Name} in {type.Name}");
                        }
                    }
                    else
                    {
                        fieldInfo.SetValue(tgt, fieldInfo.GetValue(src));
                    }
                }

                return tgt;
            }
        }

        public static bool IsList(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }
}
