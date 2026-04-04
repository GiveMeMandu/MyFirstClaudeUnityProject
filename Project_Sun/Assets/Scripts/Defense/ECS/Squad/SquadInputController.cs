using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectSun.V2.Defense.ECS
{
    /// <summary>
    /// 분대 RTS 입력 컨트롤러 (MonoBehaviour).
    ///
    /// New Input System 기반. 입력 → SquadCommandQueue → ECS 전달.
    ///
    /// 입력 파이프라인:
    ///   1. 좌클릭: 분대 선택 (화면 레이캐스트 → 분대 엔티티 판별)
    ///   2. 우클릭: 이동 명령 (지면 레이캐스트 → 목표 위치)
    ///   3. A + 우클릭: 공격 이동 명령
    ///   4. H: 정지 명령
    ///   5. Space: 일시정지 토글
    ///
    /// 일시정지(timeScale=0) 중에도:
    ///   - 입력은 정상 수신 (New Input System은 unscaledTime 기반)
    ///   - 명령은 큐에 적재
    ///   - 재개 시 ECS가 즉시 처리
    ///
    /// 성공 기준: 명령 입력 후 0.1초(100ms) 이내 반응
    /// </summary>
    public class SquadInputController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Camera mainCamera;
        [SerializeField] LayerMask groundLayer;
        [SerializeField] LayerMask squadLayer;

        [Header("Debug")]
        [SerializeField] bool logCommands = true;

        // Input state
        int _selectedSquadId = -1; // -1 = 선택 없음
        bool _attackMoveMode;
        bool _isPaused;

        // Input Actions (코드 생성)
        InputAction _selectAction;
        InputAction _commandAction;
        InputAction _attackMoveAction;
        InputAction _holdAction;
        InputAction _pauseAction;
        InputAction _pointerPosition;

        void OnEnable()
        {
            // New Input System 액션 바인딩
            // 실제 프로젝트에서는 InputActionAsset에서 로드하지만,
            // PoC에서는 코드 내 직접 생성으로 의존성 최소화
            _selectAction = new InputAction("Select", InputActionType.Button, "<Mouse>/leftButton");
            _commandAction = new InputAction("Command", InputActionType.Button, "<Mouse>/rightButton");
            _attackMoveAction = new InputAction("AttackMove", InputActionType.Button, "<Keyboard>/a");
            _holdAction = new InputAction("HoldPosition", InputActionType.Button, "<Keyboard>/h");
            _pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/space");
            _pointerPosition = new InputAction("Pointer", InputActionType.Value, "<Mouse>/position");

            _selectAction.performed += OnSelect;
            _commandAction.performed += OnCommand;
            _attackMoveAction.performed += OnAttackMoveToggle;
            _holdAction.performed += OnHoldPosition;
            _pauseAction.performed += OnPauseToggle;

            _selectAction.Enable();
            _commandAction.Enable();
            _attackMoveAction.Enable();
            _holdAction.Enable();
            _pauseAction.Enable();
            _pointerPosition.Enable();

            SquadCommandQueue.Initialize();
        }

        void OnDisable()
        {
            _selectAction.performed -= OnSelect;
            _commandAction.performed -= OnCommand;
            _attackMoveAction.performed -= OnAttackMoveToggle;
            _holdAction.performed -= OnHoldPosition;
            _pauseAction.performed -= OnPauseToggle;

            _selectAction.Dispose();
            _commandAction.Dispose();
            _attackMoveAction.Dispose();
            _holdAction.Dispose();
            _pauseAction.Dispose();
            _pointerPosition.Dispose();
        }

        /// <summary>좌클릭 — 분대 선택</summary>
        void OnSelect(InputAction.CallbackContext ctx)
        {
            if (mainCamera == null) return;

            Vector2 screenPos = _pointerPosition.ReadValue<Vector2>();
            Ray ray = mainCamera.ScreenPointToRay(screenPos);

            // 분대 레이어에 히트하면 해당 분대 선택
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, squadLayer))
            {
                // SquadMarker MonoBehaviour로 분대 ID를 가져옴
                var marker = hit.collider.GetComponentInParent<SquadMarker>();
                if (marker != null)
                {
                    SelectSquad(marker.SquadId);
                    return;
                }
            }

            // 빈 곳 클릭 → 선택 해제
            DeselectAll();
        }

        /// <summary>우클릭 — 이동 또는 공격 이동 명령</summary>
        void OnCommand(InputAction.CallbackContext ctx)
        {
            if (_selectedSquadId < 0) return;
            if (mainCamera == null) return;

            Vector2 screenPos = _pointerPosition.ReadValue<Vector2>();
            Ray ray = mainCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
            {
                float3 targetPos = new float3(hit.point.x, 0, hit.point.z);

                var cmdType = _attackMoveMode
                    ? SquadCommandType.AttackMove
                    : SquadCommandType.Move;

                IssueCommand(_selectedSquadId, cmdType, targetPos);

                // AttackMove 모드는 1회 사용 후 리셋
                _attackMoveMode = false;
            }
        }

        /// <summary>A 키 — 공격 이동 모드 토글</summary>
        void OnAttackMoveToggle(InputAction.CallbackContext ctx)
        {
            _attackMoveMode = !_attackMoveMode;
            if (logCommands)
                Debug.Log($"[SquadInput] Attack move mode: {_attackMoveMode}");
        }

        /// <summary>H 키 — 정지 명령</summary>
        void OnHoldPosition(InputAction.CallbackContext ctx)
        {
            if (_selectedSquadId < 0) return;
            IssueCommand(_selectedSquadId, SquadCommandType.HoldPosition, float3.zero);
        }

        /// <summary>Space — 일시정지 토글</summary>
        void OnPauseToggle(InputAction.CallbackContext ctx)
        {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;

            if (logCommands)
                Debug.Log($"[SquadInput] Pause: {_isPaused} (timeScale={Time.timeScale})");
        }

        void SelectSquad(int squadId)
        {
            _selectedSquadId = squadId;
            _attackMoveMode = false;

            // ECS 엔티티의 SquadSelected 갱신
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = em.CreateEntityQuery(typeof(SquadTag), typeof(SquadId), typeof(SquadSelected));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var id = em.GetComponentData<SquadId>(entities[i]);
                em.SetComponentData(entities[i], new SquadSelected { Value = (id.Value == squadId) });
            }
            entities.Dispose();

            if (logCommands)
                Debug.Log($"[SquadInput] Selected squad {squadId}");
        }

        void DeselectAll()
        {
            _selectedSquadId = -1;
            _attackMoveMode = false;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = em.CreateEntityQuery(typeof(SquadTag), typeof(SquadSelected));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                em.SetComponentData(entities[i], new SquadSelected { Value = false });
            }
            entities.Dispose();
        }

        void IssueCommand(int squadId, SquadCommandType type, float3 targetPos)
        {
            double issueTime = Time.unscaledTimeAsDouble;

            SquadCommandQueue.Enqueue(new SquadCommandEntry
            {
                SquadId = squadId,
                CommandType = type,
                TargetPosition = targetPos,
                IssuedTime = issueTime
            });

            if (logCommands)
                Debug.Log($"[SquadInput] Command: {type} → squad {squadId} at ({targetPos.x:F1}, {targetPos.z:F1})");
        }
    }

    /// <summary>
    /// 분대 GameObject에 부착하여 ECS 분대 ID를 전달하는 마커.
    /// 레이캐스트로 분대 선택 시 사용.
    /// </summary>
    public class SquadMarker : MonoBehaviour
    {
        public int SquadId;
    }
}
