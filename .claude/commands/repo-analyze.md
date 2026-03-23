Analyze an external GitHub repository and save a structured evaluation report.

Arguments: $ARGUMENTS
Expected format: `<repository-url>`

Example:
- `/repo-analyze https://github.com/owner/repo`

## Steps

1. **Parse URL**: Extract the repository URL from $ARGUMENTS. If empty, ask the user for a URL.

2. **Extract repo name**: Parse the last path segment from the URL (e.g., `owner/repo` → `repo`).

3. **Clone the repository** (shallow):
   ```
   git clone --depth 1 <url> /tmp/<repo-name>
   ```

4. **Analyze** the cloned repository using Read, Glob, and Grep tools:
   - Build a directory tree (depth 2–3)
   - Read README.md (or README, README.rst, docs/index.md)
   - Identify the entry point files (main.*, index.*, App.*, Program.*, __init__.py, etc.)
   - Identify config files (package.json, *.csproj, Cargo.toml, go.mod, requirements.txt, etc.)
   - Read the top 3–5 most important source files based on the structure
   - Note the primary language(s) and license

5. **Create output directory**:
   ```
   Docs/Study/<repo-name>/
   ```
   Use today's date for the filename: `report-YYYY-MM-DD.md`
   Today's date is available from the system context.

6. **Write the report** to `Docs/Study/<repo-name>/report-YYYY-MM-DD.md` using this structure:

```markdown
# <Repo Name> 분석 리포트

- **분석일**: YYYY-MM-DD
- **원본**: <URL>
- **목적**: 라이브러리/기술 평가

---

## 1. 개요

| 항목 | 내용 |
|---|---|
| 설명 | (한 줄 설명) |
| 주요 언어 | (언어 목록) |
| 라이선스 | (LICENSE 파일 또는 README 기반) |
| 주요 의존성 | (핵심 패키지/라이브러리) |

---

## 2. 전체 구조 트리

```
<폴더/파일 트리 (depth 2~3)>
```

---

## 3. 핵심 파일 요약

### README 요약
(README 핵심 내용 2~5 문장)

### 진입점
(main 파일 / App 파일 역할 설명)

### 설정 파일
(package.json / .csproj 등 주요 설정 요약)

---

## 4. 라이브러리 평가

### 장점
- (장점 목록)

### 단점 / 주의사항
- (단점 또는 주의사항)

### Project_Sun 도입 적합성
(이 Unity 프로젝트에 도입하기 적합한지 판단 및 이유)

---

## 5. 학습 포인트

### 배울 수 있는 패턴/기법
- (패턴 목록)

### 참고할 코드 구조
- (구체적인 파일/폴더 경로와 그 이유)

### 추천 학습 순서
1. (첫 번째로 볼 파일/개념)
2. (두 번째)
3. ...
```

7. **Clean up** the cloned directory:
   ```
   rm -rf /tmp/<repo-name>
   ```

8. **Report to user**: Tell the user the report was saved at `Docs/Study/<repo-name>/report-YYYY-MM-DD.md`.
