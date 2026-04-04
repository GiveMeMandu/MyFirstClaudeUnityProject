using System;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.UI.Util;

namespace ProjectSun.UI.Components
{
    public enum CitizenAptitude { Combat, Construction, Exploration }

    public struct CitizenData
    {
        public int Id;
        public string Name;
        public CitizenAptitude Aptitude;
        public int Proficiency;
    }

    /// <summary>
    /// Controls a single citizen card element.
    /// Handles drag initiation via pointer events.
    /// </summary>
    public class CitizenCardController
    {
        public CitizenData Data { get; private set; }
        public VisualElement Root { get; private set; }

        private readonly Label _nameLabel;
        private readonly Label _aptitudeLabel;
        private readonly Label _proficiencyLabel;
        private readonly VisualElement _portrait;

        public CitizenCardController(VisualElement cardRoot, CitizenData data)
        {
            Root = cardRoot;
            Data = data;

            _nameLabel = Root.Q<Label>("citizen-name");
            _aptitudeLabel = Root.Q<Label>("citizen-aptitude");
            _proficiencyLabel = Root.Q<Label>("citizen-proficiency");
            _portrait = Root.Q("portrait");

            Refresh();
        }

        public void Refresh()
        {
            _nameLabel.text = Data.Name;
            _aptitudeLabel.text = Data.Aptitude.ToString();
            _proficiencyLabel.text = $"Lv.{Data.Proficiency}";

            // Set portrait border color by aptitude
            _portrait.RemoveFromClassList("citizen-portrait--combat");
            _portrait.RemoveFromClassList("citizen-portrait--construction");
            _portrait.RemoveFromClassList("citizen-portrait--exploration");

            string aptitudeClass = Data.Aptitude switch
            {
                CitizenAptitude.Combat => "citizen-portrait--combat",
                CitizenAptitude.Construction => "citizen-portrait--construction",
                CitizenAptitude.Exploration => "citizen-portrait--exploration",
                _ => ""
            };
            if (!string.IsNullOrEmpty(aptitudeClass))
                _portrait.AddToClassList(aptitudeClass);
        }

        public void SetDragging(bool dragging)
        {
            if (dragging)
                Root.AddToClassList("citizen-card--dragging");
            else
                Root.RemoveFromClassList("citizen-card--dragging");
        }

        public void SetPlaced(bool placed)
        {
            if (placed)
                Root.AddToClassList("citizen-card--placed");
            else
                Root.RemoveFromClassList("citizen-card--placed");
        }
    }

    /// <summary>
    /// Manages drag & drop of citizen cards onto building sockets.
    /// Uses PointerDown/Move/Up events on the root for a single drag handler.
    /// </summary>
    public class DragDropManager
    {
        public event Action<CitizenData, int> OnCitizenPlaced;
        public event Action<CitizenData> OnCitizenReturned;

        private readonly VisualElement _rootPanel;
        private readonly VisualElement _dragGhost;
        private readonly VisualElement _citizenPool;
        private readonly VisualElement _socketArea;
        private readonly Label _statusLog;

        private CitizenCardController _dragging;
        private Vector2 _dragOffset;
        private bool _isDragging;

        public DragDropManager(VisualElement root)
        {
            _rootPanel = root;
            _dragGhost = root.Q("drag-ghost");
            _citizenPool = root.Q("citizen-pool");
            _socketArea = root.Q("socket-area");
            _statusLog = root.Q<Label>("status-log");

            // Register on the root to capture all pointer events
            _rootPanel.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _rootPanel.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _rootPanel.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        public void Dispose()
        {
            _rootPanel.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _rootPanel.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _rootPanel.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            // Find if we clicked on a citizen card
            var target = evt.target as VisualElement;
            var cardElement = FindAncestorWithClass(target, "citizen-card");
            if (cardElement == null) return;

            // Get the controller from userData
            if (cardElement.userData is not CitizenCardController controller) return;

            _dragging = controller;
            _isDragging = true;

            // Capture pointer
            _rootPanel.CapturePointer(evt.pointerId);

            // Calculate offset
            var cardWorldPos = cardElement.worldBound.position;
            _dragOffset = new Vector2(
                evt.position.x - cardWorldPos.x,
                evt.position.y - cardWorldPos.y);

            // Setup ghost
            _dragGhost.style.display = DisplayStyle.Flex;
            _dragGhost.style.width = cardElement.resolvedStyle.width;
            _dragGhost.style.height = cardElement.resolvedStyle.height;
            _dragGhost.style.backgroundColor = new Color(0.16f, 0.15f, 0.2f, 0.9f);
            _dragGhost.style.borderTopWidth = 2;
            _dragGhost.style.borderBottomWidth = 2;
            _dragGhost.style.borderLeftWidth = 2;
            _dragGhost.style.borderRightWidth = 2;
            _dragGhost.style.borderTopColor = new Color(0.47f, 0.35f, 0.86f);
            _dragGhost.style.borderBottomColor = new Color(0.47f, 0.35f, 0.86f);
            _dragGhost.style.borderLeftColor = new Color(0.47f, 0.35f, 0.86f);
            _dragGhost.style.borderRightColor = new Color(0.47f, 0.35f, 0.86f);
            _dragGhost.style.borderTopLeftRadius = 8;
            _dragGhost.style.borderTopRightRadius = 8;
            _dragGhost.style.borderBottomLeftRadius = 8;
            _dragGhost.style.borderBottomRightRadius = 8;

            UpdateGhostPosition(evt.position);
            _dragging.SetDragging(true);
            controller.Root.style.opacity = 0.4f;

            _statusLog.text = $"Dragging: {_dragging.Data.Name} ({_dragging.Data.Aptitude})";

            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || _dragging == null) return;

            UpdateGhostPosition(evt.position);

            // Highlight socket under cursor
            var socketUnder = FindSocketAt(evt.position);
            ClearAllSocketHighlights();
            if (socketUnder != null)
            {
                bool isOccupied = socketUnder.userData != null;
                socketUnder.AddToClassList(isOccupied ? "socket-zone--invalid" : "socket-zone--hover");
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging || _dragging == null) return;

            _rootPanel.ReleasePointer(evt.pointerId);

            _dragGhost.style.display = DisplayStyle.None;
            _dragging.SetDragging(false);
            _dragging.Root.style.opacity = 1f;
            ClearAllSocketHighlights();

            // Check if dropped on a valid socket
            var socketUnder = FindSocketAt(evt.position);
            if (socketUnder != null && socketUnder.userData == null)
            {
                // Valid drop — place citizen
                PlaceCitizen(_dragging, socketUnder);
            }
            else if (socketUnder != null && socketUnder.userData != null)
            {
                // Invalid — socket occupied
                _statusLog.text = $"Socket already occupied! {_dragging.Data.Name} returned.";
                OnCitizenReturned?.Invoke(_dragging.Data);
                AnimateReturnToPool(_dragging);
            }
            else
            {
                // Dropped on invalid area — check if in citizen pool (return to pool)
                _statusLog.text = $"{_dragging.Data.Name} returned to pool.";
                OnCitizenReturned?.Invoke(_dragging.Data);
                AnimateReturnToPool(_dragging);
            }

            _isDragging = false;
            _dragging = null;
        }

        private void PlaceCitizen(CitizenCardController citizen, VisualElement socket)
        {
            int socketIndex = _socketArea.IndexOf(socket);
            string socketName = socket.Q<Label>("socket-label")?.text ?? $"Socket {socketIndex}";

            // Remove from current parent
            citizen.Root.RemoveFromHierarchy();

            // Clear socket content, add citizen card
            socket.Clear();
            socket.Add(citizen.Root);
            socket.userData = citizen;

            citizen.SetPlaced(true);
            citizen.Root.style.width = Length.Percent(100);
            citizen.Root.style.height = Length.Percent(100);

            _statusLog.text = $"{citizen.Data.Name} placed in {socketName}!";

            // Scale-pop feedback
            socket.style.scale = new Scale(new Vector2(1.15f, 1.15f));
            SimpleTween.To(
                () => 1.15f,
                v => socket.style.scale = new Scale(new Vector2(v, v)),
                1f, 0.25f,
                SimpleTween.EaseType.OutBack);

            OnCitizenPlaced?.Invoke(citizen.Data, socketIndex);
        }

        private void AnimateReturnToPool(CitizenCardController citizen)
        {
            // If citizen was placed in a socket, clear that socket
            var currentParent = citizen.Root.parent;
            if (currentParent != null && currentParent.ClassListContains("socket-zone"))
            {
                currentParent.userData = null;
                var label = new Label { text = currentParent.name };
                label.AddToClassList("socket-label");
                currentParent.Clear();
                currentParent.Add(label);
            }

            citizen.Root.RemoveFromHierarchy();
            citizen.SetPlaced(false);
            citizen.Root.style.width = 120;
            citizen.Root.style.height = 160;

            _citizenPool.Add(citizen.Root);

            // Flash on return
            citizen.Root.style.opacity = 0.3f;
            SimpleTween.To(
                () => 0.3f,
                v => citizen.Root.style.opacity = v,
                1f, 0.3f,
                SimpleTween.EaseType.OutQuad);
        }

        private void UpdateGhostPosition(Vector3 position)
        {
            _dragGhost.style.left = position.x - _dragOffset.x;
            _dragGhost.style.top = position.y - _dragOffset.y;
        }

        private VisualElement FindSocketAt(Vector3 screenPos)
        {
            foreach (var child in _socketArea.Children())
            {
                if (!child.ClassListContains("socket-zone")) continue;
                if (child.worldBound.Contains(new Vector2(screenPos.x, screenPos.y)))
                    return child;
            }
            return null;
        }

        private void ClearAllSocketHighlights()
        {
            foreach (var child in _socketArea.Children())
            {
                child.RemoveFromClassList("socket-zone--hover");
                child.RemoveFromClassList("socket-zone--invalid");
            }
        }

        private static VisualElement FindAncestorWithClass(VisualElement element, string className)
        {
            var current = element;
            while (current != null)
            {
                if (current.ClassListContains(className))
                    return current;
                current = current.parent;
            }
            return null;
        }
    }
}
