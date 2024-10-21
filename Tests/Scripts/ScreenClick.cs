using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LevelNet.Tests
{
    public class ScreenClick : MonoBehaviour
    {
        [SerializeField]
        private InputAction _clickAction = new InputAction("click", binding: "<Mouse>/leftButton");

        [SerializeField]
        private InputAction _positionAction = new InputAction("click", binding: "<Mouse>/position");

        private void Start()
        {
            _clickAction?.Enable();
            _clickAction.performed += _clickAction_performed;

            _positionAction.Enable();
        }

        private void _clickAction_performed(InputAction.CallbackContext obj)
        {
            Vector2 screenPos = _positionAction.ReadValue<Vector2>();
            var clickRay = Camera.main.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(clickRay).OrderBy(hit => hit.distance).ToArray();
            foreach (RaycastHit hit in hits)
            {
                GameObject srch = hit.collider.gameObject;
                if (srch != null)
                {
                    var clickReceiver = srch.GetComponentInParent<IClickReceiver>();
                    if (clickReceiver != null)
                    {
                        clickReceiver.Click(hit.collider.gameObject);
                        break;
                    }
                }
            }
        }
    }
}