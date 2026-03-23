#!/bin/bash
set -e

# Post-tool-use hook for Unity project
# Runs after Edit, MultiEdit, or Write tools complete
# Outputs actionable reminders based on the file type edited

# Read tool information from stdin (jq unavailable — use node)
tool_info=$(cat)

tool_name=$(echo "$tool_info" | node -e "
  let d=''; process.stdin.on('data',c=>d+=c).on('end',()=>{
    try { const o=JSON.parse(d); process.stdout.write(o.tool_name||''); } catch(e){}
  });
")

file_path=$(echo "$tool_info" | node -e "
  let d=''; process.stdin.on('data',c=>d+=c).on('end',()=>{
    try { const o=JSON.parse(d); process.stdout.write(o.tool_input&&o.tool_input.file_path||''); } catch(e){}
  });
")

# Only handle edit/write tools with a file path
if [[ ! "$tool_name" =~ ^(Edit|MultiEdit|Write)$ ]] || [[ -z "$file_path" ]]; then
    exit 0
fi

# Normalize path separators to forward slashes
file_path_normalized="${file_path//\\//}"

# Determine file extension
ext="${file_path_normalized##*.}"

case "$ext" in
    cs)
        echo ""
        echo "📝 C# 스크립트 수정됨: $(basename "$file_path_normalized")"
        echo "   → 컴파일: unity-cli editor refresh --compile"
        echo ""
        ;;
    unity)
        echo ""
        echo "🎬 씬 파일 수정됨: $(basename "$file_path_normalized")"
        echo "   → 재직렬화: unity-cli reserialize"
        echo ""
        ;;
    prefab)
        echo ""
        echo "🧩 프리팹 수정됨: $(basename "$file_path_normalized")"
        echo "   → 재직렬화: unity-cli reserialize"
        echo ""
        ;;
    asset|mat|anim|controller|overrideController)
        echo ""
        echo "🗂️  Unity 에셋 수정됨: $(basename "$file_path_normalized")"
        echo "   → 재직렬화: unity-cli reserialize"
        echo ""
        ;;
    json|xml|yaml|yml)
        echo ""
        echo "📋 데이터 파일 수정됨: $(basename "$file_path_normalized")"
        echo "   → 새로고침: unity-cli editor refresh"
        echo ""
        ;;
    *)
        exit 0
        ;;
esac

exit 0
