# 스프린트 대시보드

## 진행 중인 태스크
```dataview
TASK
FROM "Sprints"
WHERE !completed
```

## 완료된 태스크
```dataview
TASK
FROM "Sprints"
WHERE completed
```

## 이번 주 일지
```dataview
LIST
FROM "Daily"
SORT file.name DESC
LIMIT 7
```
