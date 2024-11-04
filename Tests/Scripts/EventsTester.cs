
using UnityEngine;
using UnityEngine.InputSystem;

namespace LevelNet.Tests
{
    public class EventsTester : MonoBehaviour
    {
        [SerializeField] private InputAction _action1 = new("action1", binding: "<Keyboard>/1");
        [SerializeField] private InputAction _action2 = new("action2", binding: "<Keyboard>/2");
        [SerializeField] private InputAction _action3 = new("action3", binding: "<Keyboard>/3");
        [SerializeField] private InputAction _action4 = new("action4", binding: "<Keyboard>/4");
        [SerializeField] private InputAction _action5 = new("action5", binding: "<Keyboard>/5");

        private void Start()
        {
            _action1.Enable();
            _action2.Enable();
            _action3.Enable();
            _action4.Enable();
            _action5.Enable();

            _action1.performed += _action1_performed;
            _action2.performed += _action2_performed;
            _action3.performed += _action3_performed;
            _action4.performed += _action4_performed;
            _action5.performed += _action5_performed;
        }

        private void _action1_performed(InputAction.CallbackContext obj)
        {
            // All tests
            LevelNetDataTests.TestSendData();
            LevelNetDataTests.TestContainerChange();
            LevelNetDataTests.TestPartialSerialization();
            LevelNetDataTests.TestZipCompression();
        }

        private void _action2_performed(InputAction.CallbackContext obj)
        {
        }

        private void _action3_performed(InputAction.CallbackContext obj)
        {
        }

        private void _action4_performed(InputAction.CallbackContext obj)
        {
        }

        private void _action5_performed(InputAction.CallbackContext obj)
        {
        }
    }
}