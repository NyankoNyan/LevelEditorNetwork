using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LevelNet.Data;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.Assertions;

namespace LevelNet.Netcode
{
    public class NetcodeStaticSerializer
    {
        private const float PARTIAL_RATIO_TRESHOLD = .9f;
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
            WriteTypeId(writer, mainType);

            SerializeVariable(data, mainType, writer);
        }

        private void SerializeVariable(object data, Type type, FastBufferWriter writer)
        {
            if (type == typeof(Vector2Int)) {
                Vector2Int vector2Int = (Vector2Int)data;
                writer.WriteValueSafe(vector2Int);
            } else if (type == typeof(Color)) {
                Color color = (Color)data;
                writer.WriteValueSafe(color);
            } else if (IsStruct(type)) {
                foreach (var fieldInfo in type.GetFields()) {
                    object fieldData = fieldInfo.GetValue(data);
                    SerializeVariable(fieldData, fieldInfo.FieldType, writer);
                }
            } else if (type.IsArray) {
                if (data == null) {
                    writer.WriteValueSafe(-1);
                } else if (data is Array array) {
                    writer.WriteValueSafe(array.Length);

                    Type elemType = type.GetElementType();

                    foreach (object elem in array) {
                        SerializeVariable(elem, elemType, writer);
                    }
                } else {
                    throw new Exception("WTF");
                }
            }
        }

        private bool IsStruct(Type type) => type.IsValueType && !type.IsPrimitive && !type.IsEnum;

        public object Deserialize(FastBufferReader reader)
        {
            Type mainType = ReadTypeId(reader);

            return DeserializeVariable(mainType, reader);
        }

        private object DeserializeVariable(Type type, FastBufferReader reader)
        {
            if (type == typeof(Vector2Int)) {
                reader.ReadValueSafe(out Vector2Int vector2i);
                return vector2i;
            } else if (type == typeof(Color)) {
                reader.ReadValueSafe(out Color color);
                return color;
            } else if (IsStruct(type)) {
                object data = Activator.CreateInstance(type);
                foreach (var fieldInfo in type.GetFields()) {
                    object fieldData = DeserializeVariable(fieldInfo.FieldType, reader);
                    fieldInfo.SetValue(data, fieldData);
                }
                return data;
            } else if (type.IsSZArray) {
                reader.ReadValueSafe(out int size);
                if (size >= 0) {
                    Array array = (Array)Activator.CreateInstance(type, new object[] { size });
                    Type elemType = type.GetElementType();

                    for (int i = 0; i < size; i++) {
                        object elemBox = DeserializeVariable(elemType, reader);
                        array.SetValue(elemBox, i);
                    }

                    return array;
                } else {
                    return null;
                }
            } else {
                throw new NotImplementedException(type.FullName);
            }
        }

        //private void DeserializeField(ref object data, FieldInfo fieldInfo, FastBufferReader reader)
        //{
        //    if (fieldInfo.FieldType.IsSZArray) {
        //        reader.ReadValueSafe(out int size);
        //        if (size >= 0) {
        //            Array array = (Array)Activator.CreateInstance(fieldInfo.FieldType, new object[] { size });
        //            fieldInfo.SetValue(data, array);
        //            Type elemType = fieldInfo.FieldType.GetElementType();
        //            object elemContainer = Activator.CreateInstance(elemType);

        //            for (int i = 0; i < size; i++) {
        //                DeserializeVariable(ref elemContainer, reader);
        //                array.SetValue(elemContainer, i);
        //            }
        //        }
        //    } else {
        //        object fieldData = Activator.CreateInstance(fieldInfo.FieldType);
        //        DeserializeVariable(ref fieldData, reader);
        //        fieldInfo.SetValue(data, fieldData);
        //    }
        //}

        private static void InitUserTypes()
        {
            if (_userTypes == null) {
                _userTypes =
                    (from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
                     from t in a.GetTypes()
                     let attributes = t.GetCustomAttributes<SyncTypeAttribute>(true).ToArray()
                     where attributes != null && attributes.Length == 1
                     select new { t, a = attributes[0] })
                     .ToDictionary(r => r.a.Id, r => r.t);
            }
        }

        private static void WriteTypeId(FastBufferWriter writer, Type type)
        {
            SyncTypeAttribute mainAttr;
            try {
                mainAttr = type.GetCustomAttribute<SyncTypeAttribute>();
            } catch (Exception e) {
                throw new Exception($"Type {type.Name} missing serialization attribute {nameof(SyncTypeAttribute)}", e);
            }

            writer.WriteValueSafe(mainAttr.Id);
        }

        private void PartialSerializeVariable(object data, Type type, FastBufferWriter writer, IDirtyCollection dirtyFlags)
        {
            if (dirtyFlags == null) {
                SerializeVariable(data, type, writer);
            } else {
                if (IsStruct(type)) {
                    FieldsMapping fieldsMapping = null;
                    try {
                        fieldsMapping = FieldsMapping.Create(type);
                    } catch {
                        SerializeVariable(data, type, writer);
                        return;
                    }

                    DirtyStruct dirtyStruct = dirtyFlags as DirtyStruct;
                    if (dirtyStruct == null) {
                        throw new Exception("WTF");
                    }
                    bool usePartial = dirtyStruct.GetDirtnessRatio() < PARTIAL_RATIO_TRESHOLD;
                    writer.WriteByteSafe((byte)(usePartial ? 1 : 0));

                    if (usePartial) {
                        byte[] dirtyBits = BitArrayToByteArray(dirtyStruct.DirtyFields);
                        writer.WriteBytesSafe(dirtyBits);

                        foreach (var mapFieldInfo in fieldsMapping.GetInfos()) {
                            if (!dirtyStruct.DirtyFields[mapFieldInfo.fieldIndex]) {
                                continue;
                            }

                            object fieldData = mapFieldInfo.fieldInfo.GetValue(data);

                            if (mapFieldInfo.IsPartial) {
                                IDirtyCollection fieldDirtyFlags = null;
                                if (mapFieldInfo.IsArray) {
                                    fieldDirtyFlags = dirtyStruct.GetArray(mapFieldInfo.arrayIndex);
                                } else if (mapFieldInfo.IsList) {
                                    throw new NotImplementedException();
                                }
                                PartialSerializeVariable(fieldData, mapFieldInfo.fieldInfo.FieldType, writer, fieldDirtyFlags);
                            } else {
                                SerializeVariable(fieldData, mapFieldInfo.fieldInfo.FieldType, writer);
                            }
                        }
                    } else {
                        SerializeVariable(data, type, writer);
                    }
                } else if (type.IsArray) {
                    // Для массива надо определить, как сериализовать изменения
                    var arrayFlags = dirtyFlags as DirtyArray;
                    if (arrayFlags == null) {
                        throw new Exception("WTF");
                    }

                    writer.WriteByteSafe((byte)(arrayFlags.IsFullChanged ? 1 : 0));

                    if (arrayFlags.IsFullChanged) {
                        // Если массив меняется полностью, то нет смысла учитывать частичную сериализацию
                        SerializeVariable(data, type, writer);
                    } else {
                        // Размеры массива должны быть известны потребителю, поэтому их нет
                        byte[] dirtyBits = BitArrayToByteArray(arrayFlags.Flags);
                        writer.WriteBytesSafe(dirtyBits);

                        Array array = (Array)data;
                        Type elemType = type.GetElementType();

                        for (int i = 0; i < arrayFlags.Flags.Count; i++) {
                            if (!arrayFlags.Flags[i]) {
                                continue;
                            }

                            object element = array.GetValue(i);

                            SerializeVariable(element, elemType, writer);
                            // Вложенная в массивы грязь пока не поддерживается
                        }
                    }
                } else {
                    // Это что-то с dirtyFlags хз что
                    throw new NotImplementedException(type.FullName);
                }
            }
        }

        private void PartialDeserializeVariable(object data, Type type, FastBufferReader reader, IDirtyCollection dirtyFlags)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(dirtyFlags);

            if (IsStruct(type)) {
                FieldsMapping fieldsMapping;
                try {
                    fieldsMapping = FieldsMapping.Create(type);
                } catch (Exception e) {
                    throw new Exception($"Type {type.Name} unawailable for partial deserialization. May be attribute SyncType missing.", e);
                }

                reader.ReadByteSafe(out byte usePartialByte);
                Assert.IsTrue(usePartialByte == 0 || usePartialByte == 1);
                bool usePartial = usePartialByte == 1;

                DirtyStruct dirtyStruct = dirtyFlags as DirtyStruct;

                if (usePartial) {
                    byte[] dirtyBits = new byte[LengthInBytes(fieldsMapping.FieldsCount)];
                    reader.ReadBytesSafe(ref dirtyBits, dirtyBits.Length);
                    dirtyStruct.DirtyFields = new(dirtyBits);
                    dirtyStruct.DirtyFields.Length = fieldsMapping.FieldsCount;

                    foreach (var mapFieldInfo in fieldsMapping.GetInfos()) {
                        if (!dirtyStruct.DirtyFields[mapFieldInfo.fieldIndex]) {
                            continue;
                        }

                        if (mapFieldInfo.IsPartial) {
                            if (mapFieldInfo.IsArray) {
                                var arrayFlags = dirtyStruct.GetArray(mapFieldInfo.arrayIndex);

                                reader.ReadByteSafe(out byte fullChangedByte);
                                Assert.IsTrue(fullChangedByte == 0 || fullChangedByte == 1);

                                if (fullChangedByte == 1)
                                    arrayFlags.FullChange();

                                if (arrayFlags.IsFullChanged) {
                                    object fieldData = DeserializeVariable(mapFieldInfo.fieldInfo.FieldType, reader);
                                    mapFieldInfo.fieldInfo.SetValue(data, fieldData);
                                } else {
                                    object fieldData = mapFieldInfo.fieldInfo.GetValue(data);
                                    PartialDeserializeVariable(fieldData, mapFieldInfo.fieldInfo.FieldType, reader, arrayFlags);
                                }
                            } else if (mapFieldInfo.IsList) {
                                throw new NotImplementedException();
                            } else {
                                throw new Exception("WTF");
                            }
                        } else {
                            object fieldData = DeserializeVariable(mapFieldInfo.fieldInfo.FieldType, reader);
                            mapFieldInfo.fieldInfo.SetValue(data, fieldData);
                        }
                    }
                } else {
                    dirtyStruct.SetDirtyAll();

                    foreach (var fieldInfo in type.GetFields()) {
                        object fieldData = DeserializeVariable(fieldInfo.FieldType, reader);
                        fieldInfo.SetValue(data, fieldData);
                    }
                }
            } else if (type.IsSZArray) {
                Array array = (Array)data;
                if (array.Length == 0) {
                    throw new Exception("WTF");
                }

                byte[] dirtyBits = new byte[LengthInBytes(array.Length)];
                reader.ReadBytesSafe(ref dirtyBits, dirtyBits.Length);
                var dirtyArray = dirtyFlags as DirtyArray;
                Assert.IsNotNull(dirtyArray);

                dirtyArray.Flags = new(dirtyBits);
                dirtyArray.Flags.Length = array.Length;

                Type elemType = type.GetElementType();

                for (int i = 0; i < array.Length; i++) {
                    if (dirtyArray.Flags[i]) {
                        object elem = DeserializeVariable(elemType, reader);
                        array.SetValue(elem, i);
                    }
                }
            } else {
                throw new NotImplementedException(type.FullName);
            }
        }

        public void PartialSerialize(object data, DirtyFlags dirtyFlags, FastBufferWriter writer)
        {
            var mainType = data.GetType();
            WriteTypeId(writer, mainType);

            PartialSerializeVariable(data, mainType, writer, dirtyFlags.Root);
        }

        public void PartialDeserialize(object data, DirtyFlags dirtyFlags, FastBufferReader reader)
        {
            Assert.IsNotNull(dirtyFlags);
            Assert.IsNotNull(data);

            Type mainType = ReadTypeId(reader);

            PartialDeserializeVariable(data, mainType, reader, dirtyFlags.Root);
            dirtyFlags.SetDirty();
        }

        private static Type ReadTypeId(FastBufferReader reader)
        {
            Type mainType;
            InitUserTypes();
            reader.ReadValueSafe(out byte typeId);
            try {
                mainType = _userTypes[typeId];
            } catch (Exception e) {
                throw new Exception($"Unknown user type with id {typeId}", e);
            }

            return mainType;
        }

        public static byte[] BitArrayToByteArray(BitArray bits)
        {
            byte[] ret = new byte[LengthInBytes(bits.Count)];
            if (ret.Length > 0) {
                bits.CopyTo(ret, 0);
            }
            return ret;
        }

        public static int LengthInBytes(int length)
        {
            if (length == 0) {
                return 0;
            } else {
                return (length - 1) / 8 + 1;
            }
        }
    }
}