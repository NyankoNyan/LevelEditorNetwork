using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LevelNet.Data;
using Unity.Netcode;
using UnityEngine;

namespace LevelNet.Netcode
{
    public class NetcodeStaticSerializer
    {
        private static Dictionary<byte, Type> _userTypes;

        public static NetcodeStaticSerializer Instance { get; private set; }

        static NetcodeStaticSerializer()
        {
            Instance = new();
        }

        private NetcodeStaticSerializer()
        { }

        public void Serialize(object data, FastBufferWriter writer)
        {
            var mainType = data.GetType();
            SyncTypeAttribute mainAttr;
            try
            {
                mainAttr = mainType.GetCustomAttribute<SyncTypeAttribute>();
            }
            catch (Exception e)
            {
                throw new Exception($"Type {mainType.Name} missing serialization attribute {nameof(SyncTypeAttribute)}", e);
            }

            writer.WriteValueSafe(mainAttr.Id);

            foreach (var fieldInfo in mainType.GetFields())
            {
                SerializeField(data, fieldInfo, writer);
            }
        }

        private void SerializeVariable(object data, FastBufferWriter writer)
        {
            var variableType = data.GetType();

            if (variableType == typeof(Vector2Int))
            {
                Vector2Int vector2Int = (Vector2Int)data;
                writer.WriteValueSafe(vector2Int);
            }
            else if (variableType == typeof(Color))
            {
                Color color = (Color)data;
                writer.WriteValueSafe(color);
            }
            else if (IsStruct(variableType))
            {
                foreach (var fieldInfo in variableType.GetFields())
                {
                    SerializeField(data, fieldInfo, writer);
                }
            }
        }

        private bool IsStruct(Type type) => type.IsValueType && !type.IsPrimitive && !type.IsEnum;

        private void SerializeField(object data, FieldInfo fieldInfo, FastBufferWriter writer)
        {
            if (fieldInfo.FieldType.IsSZArray)
            {
                Array array = (Array)fieldInfo.GetValue(data);
                if (array == null)
                {
                    writer.WriteValueSafe(-1);
                }
                else
                {
                    writer.WriteValueSafe(array.Length);
                    foreach (object elem in array)
                    {
                        SerializeVariable(elem, writer);
                    }
                }
            }
            else
            {
                object value = fieldInfo.GetValue(data);
                SerializeVariable(value, writer);
            }
        }

        public object Deserialize(FastBufferReader reader)
        {
            InitUserTypes();
            reader.ReadValueSafe(out byte typeId);
            Type mainType;
            try
            {
                mainType = _userTypes[typeId];
            }
            catch (Exception e)
            {
                throw new Exception($"Unknown user type with id {typeId}", e);
            }

            object result = Activator.CreateInstance(mainType);
            DeserializeVariable(ref result, reader);
            return result;
        }

        private void DeserializeVariable(ref object data, FastBufferReader reader)
        {
            var variableType = data.GetType();

            if (variableType == typeof(Vector2Int))
            {
                reader.ReadValueSafe(out Vector2Int vector2i);
                data = vector2i;
            }
            else if (variableType == typeof(Color))
            {
                reader.ReadValueSafe(out Color color);
                data = color;
            }
            else if (IsStruct(variableType))
            {
                foreach (var fieldInfo in variableType.GetFields())
                {
                    DeserializeField(ref data, fieldInfo, reader);
                }
            }
        }

        private void DeserializeField(ref object data, FieldInfo fieldInfo, FastBufferReader reader)
        {
            if (fieldInfo.FieldType.IsSZArray)
            {
                reader.ReadValueSafe(out int size);
                if (size >= 0)
                {
                    Array array = (Array)Activator.CreateInstance(fieldInfo.FieldType, new object[] { size });
                    fieldInfo.SetValue(data, array);
                    Type elemType = fieldInfo.FieldType.GetElementType();
                    object elemContainer = Activator.CreateInstance(elemType);

                    for (int i = 0; i < size; i++)
                    {
                        DeserializeVariable(ref elemContainer, reader);
                        array.SetValue(elemContainer, i);
                    }
                }
            }
            else
            {
                object fieldData = Activator.CreateInstance(fieldInfo.FieldType);
                DeserializeVariable(ref fieldData, reader);
                fieldInfo.SetValue(data, fieldData);
            }
        }

        private static void InitUserTypes()
        {
            if (_userTypes == null)
            {
                _userTypes =
                    (from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
                     from t in a.GetTypes()
                     let attributes = t.GetCustomAttributes<SyncTypeAttribute>(true).ToArray()
                     where attributes != null && attributes.Length == 1
                     select new { t, a = attributes[0] })
                     .ToDictionary(r => r.a.Id, r => r.t);
            }
        }
    }
}
