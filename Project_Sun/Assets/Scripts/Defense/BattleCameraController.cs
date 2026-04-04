using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectSun.Defense
{
    /// <summary>
    /// 전투 카메라 컨트롤러.
    /// 탑다운 고정 모드와 자유 카메라 모드를 전환 가능.
    /// 줌인/줌아웃 지원.
    /// </summary>
    public class BattleCameraController : MonoBehaviour
    {
        [Header("탑다운 모드 설정")]
        [SerializeField] private Vector3 topDownOffset = new(0f, 20f, -10f);
        [SerializeField] private Vector3 topDownRotation = new(60f, 0f, 0f);
        [SerializeField] private Transform followTarget;

        [Header("줌 설정")]
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 40f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float currentZoom = 20f;

        [Header("자유 카메라 설정")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float lookSpeed = 2f;
        #pragma warning disable CS0414
        [SerializeField] private float freeCamHeight = 10f;
        #pragma warning restore CS0414

        [Header("패닝 설정 (탑다운)")]
        [SerializeField] private float panSpeed = 15f;
        [SerializeField] private Vector2 panLimitMin = new(-50f, -50f);
        [SerializeField] private Vector2 panLimitMax = new(50f, 50f);

        private bool isFreeCamMode;
        private Vector3 panPosition;
        private float freeCamYaw;
        private float freeCamPitch = 30f;
        private bool isRightMouseHeld;

        public bool IsFreeCamMode => isFreeCamMode;

        private void Start()
        {
            panPosition = followTarget != null ? followTarget.position : Vector3.zero;
            ApplyTopDownView();
        }

        private void Update()
        {
            HandleZoom();

            if (isFreeCamMode)
            {
                HandleFreeCameraMovement();
            }
            else
            {
                HandleTopDownPanning();
                ApplyTopDownView();
            }
        }

        /// <summary>
        /// 탑다운 ↔ 자유 카메라 전환
        /// </summary>
        public void ToggleCameraMode()
        {
            isFreeCamMode = !isFreeCamMode;

            if (!isFreeCamMode)
            {
                ApplyTopDownView();
            }
            else
            {
                freeCamYaw = transform.eulerAngles.y;
                freeCamPitch = transform.eulerAngles.x;
            }
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentZoom -= scroll * zoomSpeed * Time.unscaledDeltaTime;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }
        }

        private void HandleTopDownPanning()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var panInput = Vector3.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) panInput.z += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) panInput.z -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) panInput.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) panInput.x += 1f;

            panPosition += panInput * panSpeed * Time.unscaledDeltaTime;
            panPosition.x = Mathf.Clamp(panPosition.x, panLimitMin.x, panLimitMax.x);
            panPosition.z = Mathf.Clamp(panPosition.z, panLimitMin.y, panLimitMax.y);
        }

        private void ApplyTopDownView()
        {
            var offset = topDownOffset.normalized * currentZoom;
            transform.position = panPosition + offset;
            transform.rotation = Quaternion.Euler(topDownRotation);
        }

        private void HandleFreeCameraMovement()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            // 우클릭 시 회전
            isRightMouseHeld = mouse.rightButton.isPressed;
            if (isRightMouseHeld)
            {
                var mouseDelta = mouse.delta.ReadValue();
                freeCamYaw += mouseDelta.x * lookSpeed;
                freeCamPitch -= mouseDelta.y * lookSpeed;
                freeCamPitch = Mathf.Clamp(freeCamPitch, -80f, 80f);
            }

            transform.rotation = Quaternion.Euler(freeCamPitch, freeCamYaw, 0f);

            // 이동
            var moveInput = Vector3.zero;
            if (keyboard.wKey.isPressed) moveInput += transform.forward;
            if (keyboard.sKey.isPressed) moveInput -= transform.forward;
            if (keyboard.aKey.isPressed) moveInput -= transform.right;
            if (keyboard.dKey.isPressed) moveInput += transform.right;
            if (keyboard.eKey.isPressed) moveInput += Vector3.up;
            if (keyboard.qKey.isPressed) moveInput -= Vector3.up;

            transform.position += moveInput.normalized * moveSpeed * Time.unscaledDeltaTime;
        }
    }
}
