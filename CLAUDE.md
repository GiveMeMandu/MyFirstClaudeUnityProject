# 프로젝트 컨텍스트

## 워크스페이스 구조
- 루트: D:\Unity\MYFIRSTCLAUDEUNITYPROJECT\
- Unity 프로젝트: Project_Sun\
- 인프라: .claude\ (hooks, skills, agents, commands)
- 문서: Docs\ (아직 생성 전)

## 현재 세팅 상태
- Claude Code 설치 완료
- .claude\ 폴더를 Project_Sun\ 안에서 워크스페이스 루트로 이동함
- settings.local.json에 UserPromptSubmit, PostToolUse 훅 등록됨
- skill-rules.json에 unity-dev-guidelines, game-design-doc 스킬 정의됨
- hooks\ 안에 skill-activation-prompt, post-tool-use-tracker 파일 있음

## 확인 필요한 것
- 훅 파일 경로가 이동 후에도 올바른지 검증 필요
- $CLAUDE_PROJECT_DIR이 워크스페이스 루트를 가리키는지 확인 필요

## 다음 목표
- Docs\ 폴더 구조 생성
- Discord webhook 연동
- Notion MCP 연동

# Unity Project - Claude Code Guide

## Unity CLI

unity-cli is installed and controls the Unity Editor via HTTP (no MCP required).
Binary: C:\Users\wooch\AppData\Local\unity-cli.exe

If `unity-cli` is not in PATH, use the full path:

```
/c/Users/wooch/AppData/Local/unity-cli.exe
```

### Editor Control — `unity-cli editor`

- `unity-cli editor play` — Enter play mode
- `unity-cli editor play --wait` — Enter play mode and wait until fully loaded
- `unity-cli editor stop` — Stop play mode
- `unity-cli editor pause` — Toggle pause (only during play mode)
- `unity-cli editor refresh` — Refresh assets
- `unity-cli editor refresh --compile` — Refresh and recompile scripts, waits for compilation to finish

### Console Logs — `unity-cli console`

- `unity-cli console` — Read error and warning logs (default)
- `unity-cli console --lines 20 --filter all` — Last 20 log entries of all types
- `unity-cli console --filter error` — Only errors
- `unity-cli console --stacktrace short` — Include short (filtered) stack traces
- `unity-cli console --stacktrace full` — Include full (raw) stack traces
- `unity-cli console --clear` — Clear the console

### Execute C# — `unity-cli exec`

Single expressions auto-return; multi-statement requires explicit `return`.

- `unity-cli exec "Time.time"` — Read current time
- `unity-cli exec "Application.dataPath"` — Get data path
- `unity-cli exec "GameObject.FindObjectsOfType<Camera>().Length"` — Count cameras
- `unity-cli exec "Selection.activeGameObject?.name ?? \"nothing selected\""` — Get selected object name
- `unity-cli exec "var go = new GameObject(\"Marker\"); go.tag = \"EditorOnly\"; return go.name;"` — Multi-statement
- `unity-cli exec "EditorSceneManager.GetActiveScene().name" --usings UnityEditor.SceneManagement` — With extra usings

### Menu Items — `unity-cli menu`

`File/Quit` is blocked for safety.

- `unity-cli menu "File/Save Project"` — Save the project
- `unity-cli menu "Assets/Refresh"` — Refresh assets via menu
- `unity-cli menu "Window/General/Console"` — Open the console window

### Asset Reserialize — `unity-cli reserialize`

Use after text-editing `.prefab`, `.unity`, `.asset`, `.mat` files.

- `unity-cli reserialize` — Reserialize entire project
- `unity-cli reserialize Assets/Prefabs/Player.prefab` — Single file
- `unity-cli reserialize Assets/Scenes/Main.unity Assets/Scenes/Lobby.unity` — Multiple files

### Profiler — `unity-cli profiler`

- `unity-cli profiler hierarchy` — Read profiler hierarchy (last frame, top-level)
- `unity-cli profiler hierarchy --depth 3` — Recursive drill-down to depth 3
- `unity-cli profiler hierarchy --root SimulationSystem --depth 3` — Focus on specific system
- `unity-cli profiler hierarchy --frames 30 --min 0.5` — Average over last 30 frames, filter by 0.5ms
- `unity-cli profiler hierarchy --min 0.5 --sort self --max 10` — Filter, sort, limit results
- `unity-cli profiler enable` — Enable profiler recording
- `unity-cli profiler disable` — Disable profiler recording
- `unity-cli profiler status` — Show profiler state
- `unity-cli profiler clear` — Clear captured frames

### Other

- `unity-cli status` — Show connected Unity instance info (port, project, version, PID)
- `unity-cli list` — List all available tools (built-in + custom)
- `unity-cli <tool_name>` — Call a custom tool
- `unity-cli <tool_name> --params '{"key":"value"}'` — Call custom tool with parameters
- `unity-cli update` — Update unity-cli to latest version

### Global Options

- `--port <N>` — Override Unity instance port (skip auto-discovery)
- `--project <path>` — Select Unity instance by project path
- `--timeout <ms>` — HTTP request timeout (default: 120000)

## Requirements

- Unity Editor must be open with this project loaded
- Unity connector package is installed: `com.youngwoocho02.unity-cli-connector`
- Recommended: Edit > Preferences > General > Interaction Mode → "No Throttling"

## Project Structure

- Unity project root: `Project_Sun/`
- Assets: `Project_Sun/Assets/`
- Packages: `Project_Sun/Packages/`
