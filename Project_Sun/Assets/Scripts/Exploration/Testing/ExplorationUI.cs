using ProjectSun.Workforce;
using UnityEngine;

namespace ProjectSun.Exploration.Testing
{
    /// <summary>
    /// IMGUI 기반 탐사 시스템 UI.
    /// 전체 화면 노드 맵 + 원정대 관리 패널.
    /// </summary>
    public class ExplorationUI : MonoBehaviour
    {
        [Header("연동")]
        [SerializeField] private ExplorationManager explorationManager;
        [SerializeField] private WorkforceManager workforceManager;

        [Header("UI 설정")]
        [SerializeField] private float nodeRadius = 25f;
        [SerializeField] private float mapPadding = 80f;

        private bool showMap;
        private int selectedTeamIndex = -1;
        private Vector2 scrollPosition;

        private void OnGUI()
        {
            if (explorationManager == null) return;

            DrawToggleButton();

            if (showMap)
            {
                DrawFullScreenMap();
            }
        }

        private void DrawToggleButton()
        {
            if (GUI.Button(new Rect(Screen.width - 160, 10, 150, 30), showMap ? "탐사 맵 닫기" : "탐사 맵 열기"))
            {
                showMap = !showMap;
            }
        }

        private void DrawFullScreenMap()
        {
            // 반투명 배경
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 타이틀
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(0, 10, Screen.width, 40), "탐사 맵", titleStyle);

            var mapData = explorationManager.MapData;
            if (mapData == null)
            {
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2, 200, 30),
                    "맵 데이터가 없습니다.");
                return;
            }

            // 맵 영역
            float mapLeft = mapPadding;
            float mapTop = 60;
            float mapWidth = Screen.width - mapPadding * 2 - 280; // 오른쪽 패널용 공간
            float mapHeight = Screen.height - mapTop - mapPadding;

            DrawEdges(mapData, mapLeft, mapTop, mapWidth, mapHeight);
            DrawNodes(mapData, mapLeft, mapTop, mapWidth, mapHeight);
            DrawTeamMarkers(mapData, mapLeft, mapTop, mapWidth, mapHeight);

            // 오른쪽 패널: 원정대 관리
            DrawTeamPanel(Screen.width - 270, 60, 260, Screen.height - 120);

            // 닫기
            if (GUI.Button(new Rect(Screen.width / 2 - 60, Screen.height - 45, 120, 35), "닫기"))
            {
                showMap = false;
            }
        }

        private void DrawEdges(ExplorationMapSO mapData, float mapLeft, float mapTop, float mapWidth, float mapHeight)
        {
            foreach (var edge in mapData.edges)
            {
                if (!mapData.IsValidIndex(edge.nodeIndexA) || !mapData.IsValidIndex(edge.nodeIndexB))
                    continue;

                var fogA = explorationManager.GetFogState(edge.nodeIndexA);
                var fogB = explorationManager.GetFogState(edge.nodeIndexB);

                // 양쪽 모두 Hidden이면 안 그림
                if (fogA == FogState.Hidden && fogB == FogState.Hidden) continue;

                var posA = GetNodeScreenPos(mapData.nodes[edge.nodeIndexA].mapPosition,
                    mapLeft, mapTop, mapWidth, mapHeight);
                var posB = GetNodeScreenPos(mapData.nodes[edge.nodeIndexB].mapPosition,
                    mapLeft, mapTop, mapWidth, mapHeight);

                // 간선 라인 (GUI로 간소화)
                Color lineColor = (fogA == FogState.Hidden || fogB == FogState.Hidden)
                    ? new Color(0.5f, 0.5f, 0.5f, 0.3f)
                    : new Color(0.7f, 0.7f, 0.7f, 0.8f);

                DrawLine(posA, posB, lineColor);

                // 소요 턴수 표시
                if (fogA != FogState.Hidden && fogB != FogState.Hidden && edge.travelTurns > 1)
                {
                    var midPoint = (posA + posB) / 2;
                    var turnStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleCenter
                    };
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(midPoint.x - 15, midPoint.y - 10, 30, 20),
                        $"{edge.travelTurns}턴", turnStyle);
                    GUI.color = Color.white;
                }
            }
        }

        private void DrawNodes(ExplorationMapSO mapData, float mapLeft, float mapTop, float mapWidth, float mapHeight)
        {
            for (int i = 0; i < mapData.nodes.Count; i++)
            {
                var entry = mapData.nodes[i];
                var fogState = explorationManager.GetFogState(i);

                if (fogState == FogState.Hidden) continue;

                var screenPos = GetNodeScreenPos(entry.mapPosition, mapLeft, mapTop, mapWidth, mapHeight);
                var nodeRect = new Rect(screenPos.x - nodeRadius, screenPos.y - nodeRadius,
                    nodeRadius * 2, nodeRadius * 2);

                // 노드 색상
                Color nodeColor;
                if (i == explorationManager.BaseNodeIndex)
                    nodeColor = Color.cyan;
                else if (explorationManager.IsNodeVisited(i))
                    nodeColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
                else if (fogState == FogState.Hinted)
                    nodeColor = new Color(0.8f, 0.7f, 0.3f, 0.8f);
                else
                    nodeColor = GetNodeTypeColor(entry.nodeData);

                GUI.color = nodeColor;
                GUI.DrawTexture(nodeRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // 노드 라벨
                string label;
                if (i == explorationManager.BaseNodeIndex)
                    label = "기지";
                else if (fogState == FogState.Hinted)
                    label = entry.nodeData != null ? GetNodeTypeIcon(entry.nodeData.nodeType) : "?";
                else
                    label = entry.nodeData != null ? entry.nodeData.nodeName : $"N{i}";

                var labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                GUI.Label(nodeRect, label, labelStyle);

                // 힌트 텍스트 (하단)
                if (fogState == FogState.Hinted && entry.nodeData != null &&
                    !string.IsNullOrEmpty(entry.nodeData.hintText))
                {
                    var hintStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 9,
                        alignment = TextAnchor.UpperCenter
                    };
                    GUI.color = new Color(1, 1, 0.6f, 0.8f);
                    GUI.Label(new Rect(screenPos.x - 50, screenPos.y + nodeRadius + 2, 100, 20),
                        entry.nodeData.hintText, hintStyle);
                    GUI.color = Color.white;
                }

                // 클릭 처리 (선택된 팀의 목적지 설정)
                if (selectedTeamIndex >= 0 && GUI.Button(nodeRect, GUIContent.none, GUIStyle.none))
                {
                    explorationManager.SetTeamDestination(selectedTeamIndex, i);
                }
            }
        }

        private void DrawTeamMarkers(ExplorationMapSO mapData, float mapLeft, float mapTop, float mapWidth, float mapHeight)
        {
            foreach (var team in explorationManager.Teams)
            {
                if (team.MemberCount <= 0) continue;
                int nodeIdx = team.CurrentNodeIndex;
                if (nodeIdx < 0 || nodeIdx >= mapData.nodes.Count) continue;

                var screenPos = GetNodeScreenPos(mapData.nodes[nodeIdx].mapPosition,
                    mapLeft, mapTop, mapWidth, mapHeight);

                // 팀 마커 (노드 위에 오프셋)
                float offset = -nodeRadius - 18 + team.TeamId * 14;
                var markerRect = new Rect(screenPos.x - 20, screenPos.y + offset, 40, 16);

                Color markerColor = team.TeamId switch
                {
                    0 => Color.green,
                    1 => new Color(1f, 0.5f, 0f),
                    2 => new Color(0.5f, 0.5f, 1f),
                    _ => Color.white
                };

                GUI.color = markerColor;
                GUI.DrawTexture(markerRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                var markerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter
                };

                string stateIcon = team.State switch
                {
                    ExpeditionState.Moving => ">>",
                    ExpeditionState.Returning => "<<",
                    _ => ""
                };

                GUI.Label(markerRect, $"T{team.TeamId + 1}{stateIcon}", markerStyle);
            }
        }

        private void DrawTeamPanel(float x, float y, float width, float height)
        {
            GUI.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(x, y + 5, width, 25), "원정대", headerStyle);

            float cy = y + 35;

            // 인력 요약
            if (workforceManager != null)
            {
                GUI.Label(new Rect(x + 10, cy, width - 20, 20),
                    $"가용 인력: {workforceManager.IdleCount} / 건강: {workforceManager.HealthyCount}");
                cy += 22;
                GUI.Label(new Rect(x + 10, cy, width - 20, 20),
                    $"원정 인력: {workforceManager.ExpeditionWorkers}");
                cy += 25;
            }

            GUI.Label(new Rect(x + 10, cy, width - 20, 20),
                $"최대 팀: {explorationManager.MaxTeams}");
            cy += 25;

            // 구분선
            GUI.color = Color.gray;
            GUI.DrawTexture(new Rect(x + 10, cy, width - 20, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            cy += 10;

            // 각 팀 표시
            for (int i = 0; i < explorationManager.Teams.Count; i++)
            {
                var team = explorationManager.Teams[i];
                bool isSelected = selectedTeamIndex == i;

                // 팀 배경
                if (isSelected)
                {
                    GUI.color = new Color(0.3f, 0.3f, 0.5f, 0.8f);
                    GUI.DrawTexture(new Rect(x + 5, cy - 2, width - 10, 100), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                GUI.Label(new Rect(x + 10, cy, width - 20, 20),
                    $"팀 {i + 1} [{team.State}]");
                cy += 20;

                GUI.Label(new Rect(x + 10, cy, width - 20, 20),
                    $"  인원: {team.MemberCount} (min:{explorationManager.MinTeamSize} max:{explorationManager.MaxTeamSize})");
                cy += 20;

                if (team.State == ExpeditionState.Moving || team.State == ExpeditionState.Returning)
                {
                    GUI.Label(new Rect(x + 10, cy, width - 20, 20),
                        $"  남은 턴: {team.TurnsRemaining}");
                    cy += 20;
                }

                // 버튼들
                float btnY = cy;

                if (team.State == ExpeditionState.Idle)
                {
                    // 인원 조정 버튼
                    if (team.MemberCount < explorationManager.MaxTeamSize)
                    {
                        if (GUI.Button(new Rect(x + 10, btnY, 55, 22), "+인원"))
                            explorationManager.SetTeamMembers(i, team.MemberCount + 1);
                    }
                    if (team.MemberCount > 0)
                    {
                        if (GUI.Button(new Rect(x + 70, btnY, 55, 22), "-인원"))
                            explorationManager.SetTeamMembers(i, team.MemberCount - 1);
                    }

                    if (team.MemberCount >= explorationManager.MinTeamSize)
                    {
                        if (GUI.Button(new Rect(x + 130, btnY, 60, 22), isSelected ? "선택해제" : "목적지"))
                            selectedTeamIndex = isSelected ? -1 : i;
                    }

                    btnY += 25;

                    if (team.MemberCount > 0)
                    {
                        if (GUI.Button(new Rect(x + 10, btnY, 60, 22), "해산"))
                            explorationManager.DisbandTeam(i);
                    }
                }
                else if (team.State == ExpeditionState.Arrived)
                {
                    if (GUI.Button(new Rect(x + 10, btnY, 80, 22), "목적지 지정"))
                        selectedTeamIndex = isSelected ? -1 : i;

                    if (GUI.Button(new Rect(x + 100, btnY, 60, 22), "귀환"))
                        explorationManager.OrderReturn(i);
                }

                cy = btnY + 30;

                // 구분선
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                GUI.DrawTexture(new Rect(x + 10, cy, width - 20, 1), Texture2D.whiteTexture);
                GUI.color = Color.white;
                cy += 8;
            }

            if (explorationManager.Teams.Count == 0)
            {
                var infoStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                GUI.Label(new Rect(x + 10, cy, width - 20, 40),
                    "탐사 건물을 건설하세요", infoStyle);
            }
        }

        private Vector2 GetNodeScreenPos(Vector2 normalizedPos, float mapLeft, float mapTop, float mapWidth, float mapHeight)
        {
            return new Vector2(
                mapLeft + normalizedPos.x * mapWidth,
                mapTop + normalizedPos.y * mapHeight
            );
        }

        private Color GetNodeTypeColor(ExplorationNodeSO nodeData)
        {
            if (nodeData == null) return Color.gray;
            return nodeData.nodeType switch
            {
                ExplorationNodeType.Resource => new Color(0.2f, 0.8f, 0.3f, 0.9f),
                ExplorationNodeType.Recon => new Color(0.3f, 0.5f, 0.9f, 0.9f),
                ExplorationNodeType.Encounter => new Color(0.9f, 0.4f, 0.2f, 0.9f),
                ExplorationNodeType.Tech => new Color(0.8f, 0.6f, 0.9f, 0.9f),
                _ => Color.gray
            };
        }

        private string GetNodeTypeIcon(ExplorationNodeType type)
        {
            return type switch
            {
                ExplorationNodeType.Resource => "[R]",
                ExplorationNodeType.Recon => "[S]",
                ExplorationNodeType.Encounter => "[E]",
                ExplorationNodeType.Tech => "[T]",
                _ => "[?]"
            };
        }

        private void DrawLine(Vector2 from, Vector2 to, Color color)
        {
            GUI.color = color;
            float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(from, to);

            GUIUtility.RotateAroundPivot(angle, from);
            GUI.DrawTexture(new Rect(from.x, from.y - 1, length, 2), Texture2D.whiteTexture);
            GUIUtility.RotateAroundPivot(-angle, from);
            GUI.color = Color.white;
        }
    }
}
