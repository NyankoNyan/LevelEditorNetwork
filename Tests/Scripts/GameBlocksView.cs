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
        public string name;
        public object value;
    }

    public class DataChangeInfo
    {
        public IEnumerable<DataChangeElement> dataChangeElements;
    }

    public interface IData
    {
        public delegate void OnDataChangeDelegate(DataChangeInfo info);

        public event OnDataChangeDelegate OnDataChange;

        public delegate void OnDestroyDelegate();

        public event OnDestroyDelegate OnDestroy;

        public void ChangeRequest(string name, object value);

        public object Current { get; }
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

        public Color PaintColor
        {
            get => _paintColor;
            set
            {
                _paintColor = value;
            }
        }

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
                Debug.Log("Data changes:");
                foreach (var elem in info.dataChangeElements)
                {
                    Debug.Log($"This is {elem.name} with {elem.value}");
                    string[] nameParts = elem.name.Split('/');
                    if (nameParts.Length == 2 && nameParts[0] == "Color")
                    {
                        Color color = (Color)elem.value;
                        Vector2Int coord = PlainToIndex2D(int.Parse(nameParts[1]));
                        ChangeBlockColor(coord, color);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            };

            onDestroy = () =>
            {
                data.OnDataChange -= onDataChange;
                data.OnDestroy -= onDestroy;
            };

            data.OnDataChange += onDataChange;
            data.OnDestroy += onDestroy;

            var blocksState = (GameBlocksState)data.Current;
            InitBlocks(blocksState.size, blocksState.colors);
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

        private Vector2Int PlainToIndex2D(int plain)
        {
            Vector2Int coord = new Vector2Int()
            {
                x = plain % _currentSize.x,
                y = plain / _currentSize.y
            };
            if (coord.x < 0 || coord.x >= _currentSize.x || coord.y < 0 || coord.y >= _currentSize.y)
            {
                throw new ArgumentException(plain.ToString());
            }
            return coord;
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