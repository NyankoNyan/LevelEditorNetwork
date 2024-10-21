using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

namespace LevelNet.Data
{
    public struct DataChangeElement
    {
        public IComponent component;
        public object value;
    }

    public class DataChangeInfo
    {
        public IEnumerable<DataChangeElement> dataChangeElements;
    }

    public interface IComponent
    {
        public bool IsReadable { get; }
        public bool IsWritable { get; }
        public string Name { get; }
        public bool IsPartial { get; }
        public IEnumerable<IComponent> Components { get; }
    }

    public interface IData
    {
        public delegate void OnDataChangeDelegate(DataChangeInfo info);

        public event OnDataChangeDelegate OnDataChange;

        public delegate void OnDestroyDelegate();

        public event OnDestroyDelegate OnDestroy;

        public void ChangeRequest(string name, object value);
    }

    public interface IDataFabric
    {
        IData Create(Type type);
    }

    public enum SyncUpdateRule
    {
        AsClient,
        AsServer
    }
}

namespace LevelNet.Netcode
{
    //public class DataFabric : IDataFabric
    //{
    //    public IData Create(Type type)
    //    {
    //        var typeInfo = type.GetTypeInfo();
    //        foreach (var member in typeInfo.DeclaredMembers)
    //        {
    //            if (member.MemberType == MemberTypes.Field)
    //            {
    //                var field = (FieldInfo)member;
    //                var activeC = field.CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(ActiveComponentAttribute));
    //                if (activeC != null)
    //                {
    //                }
    //            }
    //        }
    //    }
    //}

    //internal class ComponentFabric
    //{
    //    public static IComponent Create(ActiveComponentAttribute activeComponent, FieldInfo fieldInfo)
    //    {
    //        if (activeComponent.partial)
    //        {
    //        }
    //        return new Component(activeComponent.name, activeComponent.partial, null);
    //    }
    //}

    //internal class Data : IData
    //{
    //    public IEnumerable<IComponent> Components => throw new NotImplementedException();

    //    public event IData.OnDataChangeDelegate OnDataChange;

    //    public event IData.OnDestroyDelegate OnDestroy;

    //    public void ChangeRequest(string name, object value)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //internal class ArrayComponent : Component
    //{
    //}

    //internal abstract class Component : IComponent
    //{
    //    private bool _read;
    //    private bool _write;
    //    private readonly string _name;
    //    private readonly bool _partial;
    //    private readonly IComponent[] _components;

    //    public bool IsReadable => _read;

    //    public bool IsWritable => _write;

    //    public string Name => _name;

    //    public bool IsPartial => _partial;

    //    public IEnumerable<IComponent> Components => _components;

    //    internal Component(string name, bool partial, IComponent[] components)
    //    {
    //        _name = name;
    //        _partial = partial;
    //        _components = components;
    //    }

    //    internal void UpdateRights(bool read, bool write)
    //    {
    //        _read = read;
    //        _write = write;
    //    }
    //}

    public enum BaseTypes
    {
        BYTE = 128,
        SHORT,
        INT,
        FLOAT,
        DOUBLE,
        VEC2,
        VEC3,
        VEC4,
        VEC2I,
        VEC3I,
        QUATERNION,
        BOOL,
        ARRAY,
        NULL
    }
}

namespace LevelNet.Tests
{
    using LevelNet.Data;

    internal enum UserTypes
    {
        GameBlockState = 1
    }

    [SyncType((byte)UserTypes.GameBlockState)]
    public struct GameBlocksState : ICloneable
    {
        [SyncComponent(name: "Size")]
        public Vector2Int size;

        [SyncComponent(name: "Color", partial: true)]
        public Color[] colors;

        public object Clone()
        {
            return new GameBlocksState()
            {
                size = size,
                colors = (Color[])colors.Clone()
            };
        }

        public override readonly string ToString()
        {
            return $"{{{size}, [{String.Join(", ", colors)}]}}";
        }
    }

    public interface IClickReceiver
    {
        void Click(GameObject target);
    }

    public class GameBlocksView : MonoBehaviour, IClickReceiver
    {
        [SerializeField]
        private GameObject _blockPrefab;

        [SerializeField]
        private Vector2 _physicalSize = new(5, 5);

        [SerializeField]
        private float _colorChangeSpeed = 1f;

        [SerializeField]
        private Color _paintColor = Color.red;

        [SerializeField]
        private Color _waitColor = Color.gray;

        private readonly Dictionary<Vector2Int, Color> _colorChanges = new();
        private Vector2Int _currentSize;
        private IData _data;
        private readonly Dictionary<Vector2Int, GameObject> _blocks = new();

        private void Update()
        {
            ProcessColorChange(Time.deltaTime);
        }

        public void InitBlocks(Vector2Int size, Color[] colors)
        {
            Assert.AreEqual(size.x * size.y, colors.Length);
            DeleteBlocks();

            _currentSize = size;

            int counter = 0;
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var block = Instantiate(_blockPrefab, transform);
                    block.transform.SetLocalPositionAndRotation(
                        new Vector3(
                        (x - ((size.x - 1) / 2f)) * _physicalSize.x / size.x,
                        (y - ((size.y - 1) / 2f)) * _physicalSize.y / size.y,
                        0),
                        Quaternion.identity);
                    block.name = $"Block_{x}_{y}";
                    var renderer = block.GetComponent<Renderer>();
                    renderer.material.color = colors[counter++];
                    _blocks.Add(new Vector2Int(x, y), block);
                }
            }
        }

        public void ChangeBlockColor(Vector2Int coord, Color color)
        {
            if (_colorChanges.ContainsKey(coord))
            {
                _colorChanges[coord] = color;
            }
            else
            {
                _colorChanges.Add(coord, color);
            }
        }

        private void ProcessColorChange(float deltaTime)
        {
            foreach (var key in _colorChanges.Keys.ToArray())
            {
                Color targetColor = _colorChanges[key];
                GameObject block = _blocks[key];
                Color newColor = Color.Lerp(block.GetComponent<Renderer>().material.color, targetColor, deltaTime * _colorChangeSpeed);
                Color delta = newColor - targetColor;
                float distance = Mathf.Abs(delta.r) + Mathf.Abs(delta.g) + Mathf.Abs(delta.b);
                if (distance > 0.01f)
                {
                    block.GetComponent<Renderer>().material.color = newColor;
                }
                else
                {
                    block.GetComponent<Renderer>().material.color = targetColor;
                    _colorChanges.Remove(key);
                }
            }
        }

        private void DeleteBlocks()
        {
            foreach (var block in _blocks.Values.ToArray())
            {
                Destroy(block);
            }

            _blocks.Clear();
            _colorChanges.Clear();

            _currentSize = default;
        }

        public void InitData(IData data)
        {
            _data = data;

            IData.OnDataChangeDelegate onDataChange = null;
            IData.OnDestroyDelegate onDestroy = null;

            onDataChange = (info) =>
            {
            };

            onDestroy = () =>
            {
                data.OnDataChange -= onDataChange;
                data.OnDestroy -= onDestroy;
            };

            data.OnDataChange += onDataChange;
            data.OnDestroy += onDestroy;
        }

        public void Click(GameObject target)
        {
            Regex regex = new(@"Block_(\d+)_(\d+)");

            var match = regex.Match(target.name);
            if (match.Success)
            {
                Vector2Int coord = new(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
                _data.ChangeRequest($"Color/{Index2DToPlain(coord)}", _paintColor);
            }
        }

        private int Index2DToPlain(Vector2Int coord)
        {
            if (coord.x >= _currentSize.x || coord.y >= _currentSize.y)
            {
                throw new ArgumentException(coord.ToString());
            }
            return coord.y * _currentSize.x + coord.x;
        }

#if UNITY_EDITOR

        [ContextMenu("Test Init")]
        private void TestInit()
        {
            var colors = new Color[25];
            Array.Fill(colors, Color.green);
            InitBlocks(new(5, 5), colors);
        }

#endif
    }
}