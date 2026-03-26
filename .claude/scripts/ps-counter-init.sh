#!/usr/bin/env bash
set -euo pipefail

# ps-counter-init.sh — PS-NNN 카운터 초기화
# 기존 커밋에서 가장 높은 PS 번호를 찾아 카운터 파일에 기록
# Usage: ps-counter-init.sh [--force]

COMMON_DIR=$(git rev-parse --git-common-dir 2>/dev/null)
if [ -z "$COMMON_DIR" ]; then
  echo "오류: git 저장소가 아닙니다."
  exit 1
fi

COUNTER_FILE="$COMMON_DIR/ps-counter"

# 이미 존재하면 --force 없이 skip
if [ -f "$COUNTER_FILE" ] && [ "${1:-}" != "--force" ]; then
  CURRENT=$(cat "$COUNTER_FILE")
  echo "카운터가 이미 존재합니다: PS-$(printf '%03d' "$CURRENT")"
  echo "강제 초기화: ps-counter-init.sh --force"
  exit 0
fi

# 모든 브랜치의 커밋에서 [PS-NNN] 패턴 스캔
MAX_NUM=0

while IFS= read -r line; do
  if [[ "$line" =~ \[PS-([0-9]+)\] ]]; then
    NUM=$((10#${BASH_REMATCH[1]}))
    if [ "$NUM" -gt "$MAX_NUM" ]; then
      MAX_NUM=$NUM
    fi
  fi
done < <(git log --all --oneline --format='%s' 2>/dev/null)

echo "$MAX_NUM" > "$COUNTER_FILE"

if [ "$MAX_NUM" -eq 0 ]; then
  echo "기존 PS 커밋 없음. 카운터를 0으로 초기화 (다음 커밋: PS-001)"
else
  echo "최대 PS 번호: PS-$(printf '%03d' "$MAX_NUM"). 카운터를 ${MAX_NUM}으로 설정."
fi

echo "카운터 파일: $COUNTER_FILE"
