using CinliMahzen.Core;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// TEMPORARY: while the human is controlled locally, parents <c>Camera.main</c> to the head so Sandbox can be walked in first person.
    /// Delete this component (and its menu wiring in HumanPrefabBuilder) when the CameraRig arrives in TODO 1.2.
    /// Detaches again, restoring the camera's old parent and pose, when the pawn stops being the local one.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HumanController))]
    public sealed class TempFpsCameraAttach : MonoBehaviour
    {
        private HumanController _human;
        private Transform _camera;
        private Transform _oldParent;
        private Vector3 _oldLocalPosition;
        private Quaternion _oldLocalRotation;
        private bool _attached;
        private bool _failedWhileWanted;

        private void Awake()
        {
            _human = GetComponent<HumanController>();
        }

        private void LateUpdate()
        {
            // The camera object can vanish with a scene change.
            if (_attached && _camera == null)
                _attached = false;

            bool want = _human.IsLocallyControlled && _human.Head != null;
            if (!want)
            {
                _failedWhileWanted = false;
                if (_attached)
                    Detach();
            }
            else if (!_attached && !_failedWhileWanted)
            {
                Attach();
            }
        }

        private void OnDisable()
        {
            if (_attached)
                Detach();
        }

        private void Attach()
        {
            Camera main = Camera.main;
            if (main == null)
            {
                _failedWhileWanted = true;
                CMLog.Warn("Human", "TempFpsCameraAttach: no camera tagged MainCamera in the scene");
                return;
            }

            _camera = main.transform;
            _oldParent = _camera.parent;
            _oldLocalPosition = _camera.localPosition;
            _oldLocalRotation = _camera.localRotation;
            _camera.SetParent(_human.Head, false);
            _camera.localPosition = Vector3.zero;
            _camera.localRotation = Quaternion.identity;
            _attached = true;
        }

        private void Detach()
        {
            _attached = false;
            if (_camera == null)
                return;
            _camera.SetParent(_oldParent != null ? _oldParent : null, false);
            _camera.localPosition = _oldLocalPosition;
            _camera.localRotation = _oldLocalRotation;
            _camera = null;
        }
    }
}
