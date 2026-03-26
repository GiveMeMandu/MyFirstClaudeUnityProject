using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectSun.TechTree.Testing
{
    /// <summary>
    /// IMGUI 기반 기술 트리 UI.
    /// 카테고리 탭 + 노드 표시 + 연구 시작/전환.
    /// </summary>
    public class TechTreeUI : MonoBehaviour
    {
        [SerializeField] private TechTreeManager techTreeManager;

        private bool showWindow;
        private int selectedCategoryIndex;
        private Vector2 scrollPosition;
        private TechNodeSO hoveredNode;

        // 스타일 캐시
        private GUIStyle nodeStyle;
        private GUIStyle completedStyle;
        private GUIStyle lockedStyle;
        private GUIStyle inProgressStyle;
        private GUIStyle pausedStyle;
        private GUIStyle headerStyle;
        private GUIStyle tooltipStyle;
        private bool stylesInitialized;

        // 완료 알림
        private string completionMessage;
        private float completionMessageTimer;

        private void OnEnable()
        {
            if (techTreeManager != null)
            {
                techTreeManager.OnResearchCompleted += HandleResearchCompleted;
            }
        }

        private void OnDisable()
        {
            if (techTreeManager != null)
            {
                techTreeManager.OnResearchCompleted -= HandleResearchCompleted;
            }
        }

        private void Update()
        {
            if (completionMessageTimer > 0)
                completionMessageTimer -= Time.deltaTime;
        }

        private void HandleResearchCompleted(TechNodeSO node)
        {
            completionMessage = $"연구 완료: {node.nodeName}";
            completionMessageTimer = 4f;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            nodeStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            completedStyle = new GUIStyle(nodeStyle);
            completedStyle.normal.textColor = Color.green;

            lockedStyle = new GUIStyle(nodeStyle);
            lockedStyle.normal.textColor = Color.gray;

            inProgressStyle = new GUIStyle(nodeStyle);
            inProgressStyle.normal.textColor = Color.yellow;

            pausedStyle = new GUIStyle(nodeStyle);
            pausedStyle.normal.textColor = new Color(1f, 0.6f, 0f);

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            tooltipStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                padding = new RectOffset(8, 8, 6, 6)
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (techTreeManager == null || techTreeManager.TechTreeData == null) return;

            InitStyles();

            DrawToggleButton();
            DrawCompletionNotification();

            if (showWindow)
                DrawTechTreeWindow();
        }

        private void DrawToggleButton()
        {
            var current = techTreeManager.CurrentResearch;
            string label = current != null
                ? $"Research: {current.nodeName} ({techTreeManager.GetProgress(current) * 100:F0}%)"
                : "Research: (none)";

            if (GUI.Button(new Rect(Screen.width - 320, 40, 310, 30), label))
                showWindow = !showWindow;
        }

        private void DrawCompletionNotification()
        {
            if (completionMessageTimer <= 0 || string.IsNullOrEmpty(completionMessage)) return;

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = Color.green;

            float w = 400, h = 40;
            GUI.Box(new Rect((Screen.width - w) / 2, 80, w, h), completionMessage, style);
        }

        private void DrawTechTreeWindow()
        {
            float windowW = 700, windowH = 500;
            float windowX = (Screen.width - windowW) / 2;
            float windowY = (Screen.height - windowH) / 2;

            GUI.Box(new Rect(windowX, windowY, windowW, windowH), "");

            GUILayout.BeginArea(new Rect(windowX + 10, windowY + 10, windowW - 20, windowH - 20));

            // 타이틀 + 닫기
            GUILayout.BeginHorizontal();
            GUILayout.Label(techTreeManager.TechTreeData.treeName, headerStyle);
            if (GUILayout.Button("X", GUILayout.Width(30), GUILayout.Height(25)))
                showWindow = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 카테고리 탭
            var categories = techTreeManager.TechTreeData.categories;
            if (categories.Count > 0)
            {
                var tabNames = categories.Select(c => c != null ? c.categoryName : "?").ToArray();
                selectedCategoryIndex = GUILayout.Toolbar(selectedCategoryIndex, tabNames, GUILayout.Height(28));

                if (selectedCategoryIndex >= 0 && selectedCategoryIndex < categories.Count)
                {
                    var cat = categories[selectedCategoryIndex];
                    if (cat != null)
                    {
                        GUILayout.Space(5);
                        DrawCategoryNodes(cat);
                    }
                }
            }

            // 현재 연구 상태 바
            GUILayout.FlexibleSpace();
            DrawCurrentResearchBar();

            GUILayout.EndArea();

            // 툴팁 (윈도우 영역 외부에 그림)
            if (hoveredNode != null)
                DrawNodeTooltip(windowX, windowY, windowW);
        }

        private void DrawCategoryNodes(TechTreeCategorySO category)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            hoveredNode = null;

            // 노드를 행 단위로 배치 (Y 좌표 기준 그룹화)
            var nodesByRow = GroupNodesByRow(category.nodes);

            foreach (var row in nodesByRow.OrderBy(r => r.Key))
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                foreach (var node in row.Value.OrderBy(n => n.nodePosition.x))
                {
                    DrawNode(node);
                    GUILayout.Space(10);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
            }

            GUILayout.EndScrollView();
        }

        private Dictionary<int, List<TechNodeSO>> GroupNodesByRow(List<TechNodeSO> nodes)
        {
            var result = new Dictionary<int, List<TechNodeSO>>();
            foreach (var node in nodes)
            {
                if (node == null) continue;
                int row = Mathf.RoundToInt(node.nodePosition.y);
                if (!result.ContainsKey(row))
                    result[row] = new List<TechNodeSO>();
                result[row].Add(node);
            }
            return result;
        }

        private void DrawNode(TechNodeSO node)
        {
            var state = techTreeManager.GetNodeState(node);
            var style = state switch
            {
                TechNodeState.Completed => completedStyle,
                TechNodeState.Locked => lockedStyle,
                TechNodeState.InProgress => inProgressStyle,
                TechNodeState.Paused => pausedStyle,
                _ => nodeStyle
            };

            string prefix = state switch
            {
                TechNodeState.Completed => "[V] ",
                TechNodeState.Locked => "[X] ",
                TechNodeState.InProgress => "[>>] ",
                TechNodeState.Paused => "[||] ",
                _ => ""
            };

            string label = $"{prefix}{node.nodeName}";

            // 진행도 바
            if (state == TechNodeState.InProgress || state == TechNodeState.Paused)
            {
                float progress = techTreeManager.GetProgress(node);
                label += $"\n{techTreeManager.GetCurrentProgressPoints(node):F1}/{node.requiredResearchPoints}";
            }

            float nodeW = 150, nodeH = 55;

            if (GUILayout.Button(label, style, GUILayout.Width(nodeW), GUILayout.Height(nodeH)))
            {
                OnNodeClicked(node, state);
            }

            // 호버 감지
            var lastRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint && lastRect.Contains(Event.current.mousePosition))
            {
                hoveredNode = node;
            }

            // 진행도 바 오버레이
            if (state == TechNodeState.InProgress || state == TechNodeState.Paused)
            {
                float progress = techTreeManager.GetProgress(node);
                var barRect = new Rect(lastRect.x + 2, lastRect.yMax - 8, (lastRect.width - 4) * progress, 5);
                var bgRect = new Rect(lastRect.x + 2, lastRect.yMax - 8, lastRect.width - 4, 5);
                GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
                GUI.color = state == TechNodeState.InProgress ? Color.cyan : new Color(1f, 0.6f, 0f);
                GUI.DrawTexture(barRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        private void OnNodeClicked(TechNodeSO node, TechNodeState state)
        {
            switch (state)
            {
                case TechNodeState.Available:
                case TechNodeState.Paused:
                    techTreeManager.StartResearch(node);
                    break;
                case TechNodeState.Locked:
                    // 잠긴 노드 — 아무 동작 없음 (툴팁에서 선행 연구 표시)
                    break;
            }
        }

        private void DrawNodeTooltip(float windowX, float windowY, float windowW)
        {
            var node = hoveredNode;
            var state = techTreeManager.GetNodeState(node);

            var lines = new List<string>();
            lines.Add(node.nodeName);
            lines.Add("");

            if (!string.IsNullOrEmpty(node.description))
                lines.Add(node.description);

            lines.Add($"상태: {GetStateKorean(state)}");
            lines.Add($"필요 연구 포인트: {node.requiredResearchPoints}");

            if (node.researchCost.Count > 0)
            {
                var costs = string.Join(", ", node.researchCost.Select(c => $"{c.resourceId} {c.amount}"));
                bool paid = techTreeManager.HasPaidCost(node);
                lines.Add($"착수 비용: {costs}{(paid ? " (지불됨)" : "")}");
            }

            if (node.prerequisites.Count > 0)
            {
                var prereqs = string.Join(", ", node.prerequisites.Where(p => p != null).Select(p => p.nodeName));
                lines.Add($"선행 연구: {prereqs}");
            }

            if (node.effects.Count > 0)
            {
                lines.Add("");
                lines.Add("효과:");
                foreach (var effect in node.effects)
                {
                    string desc = !string.IsNullOrEmpty(effect.description) ? effect.description : $"{effect.effectType} ({effect.targetId}: {effect.value})";
                    lines.Add($"  - {desc}");
                }
            }

            string text = string.Join("\n", lines);
            float tooltipW = 280, tooltipH = 20 + lines.Count * 16;
            var mousePos = Event.current.mousePosition;
            float tx = Mathf.Min(mousePos.x + 15, Screen.width - tooltipW - 10);
            float ty = Mathf.Min(mousePos.y + 15, Screen.height - tooltipH - 10);

            GUI.Box(new Rect(tx, ty, tooltipW, tooltipH), text, tooltipStyle);
        }

        private void DrawCurrentResearchBar()
        {
            GUILayout.BeginHorizontal("box");

            var current = techTreeManager.CurrentResearch;
            if (current != null)
            {
                float progress = techTreeManager.GetProgress(current);
                GUILayout.Label($"연구 중: {current.nodeName}", GUILayout.Width(200));
                GUILayout.Label($"{techTreeManager.GetCurrentProgressPoints(current):F1} / {current.requiredResearchPoints}", GUILayout.Width(80));

                // 프로그레스 바
                var barRect = GUILayoutUtility.GetRect(150, 18);
                GUI.color = new Color(0.2f, 0.2f, 0.2f);
                GUI.DrawTexture(barRect, Texture2D.whiteTexture);
                var fillRect = new Rect(barRect.x, barRect.y, barRect.width * progress, barRect.height);
                GUI.color = Color.cyan;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                GUILayout.Label($"{progress * 100:F0}%", GUILayout.Width(45));
            }
            else
            {
                GUILayout.Label("연구 대상 없음 — 노드를 클릭하여 연구를 시작하세요");
            }

            GUILayout.EndHorizontal();
        }

        private string GetStateKorean(TechNodeState state) => state switch
        {
            TechNodeState.Locked => "잠김 (선행 연구 필요)",
            TechNodeState.Available => "연구 가능",
            TechNodeState.InProgress => "연구 진행 중",
            TechNodeState.Paused => "일시 중지 (진행도 보존)",
            TechNodeState.Completed => "완료",
            _ => state.ToString()
        };
    }
}
