---
source: "D:\Krafton\Ref_Dinkum\기존 전투 시스템 분석 - 5minlab (5ml) - Wiki.pdf"
category: reference
format: PDF
file_size: 0.5MB
extracted: 2026-04-01 12:22
total_pages: 2
extracted_pages: 2
---

# 기존 전투 시스템 분석 - 5minlab (5ml) - Wiki

## Page 1

페이지/…/ 전투 시스템  ( 기술 )
알 수  없는  사용자  (jr.lee)님이 작성 , 2024-03-12 에 최종  변경기존 전투  시스템  분석
문서 목적 :
새로운  전투  시스템  만들기  전에  기존  시스템을  충분히  파악하고  추후  협업  할  분들을  위해  설명
플로우 :
1. Tool 의  Animation 에  붙어  있는  startAttack 이벤트가  불림
2. EquipItemToChar.startAttack()
a. 현재 toolWeapon 을  들고  있는지  체크
3. MeleeAttacks.attack()
a. 현재 attack 이  플레이  중인지  확인
b. genericAttack Coroutine 호출
4. MeleeAttacks.genericAttack()
a. 캐릭터가  제일  가까운  target 을  찾고  방향  바라보도록  조정
b. 캐릭터  일정  시간동안  제자리에  고정시킴
c. myHitBox 의  startAttack 호출
5. ItemHitbox.startAttack()
a. 데미지  관련  정보  초기화
b. Collider 킴
6. ItemHitBox.OnTriggerEnter()
a. 공격 가능한  타겟인지  확인
i. Damageable 이  붙어  있는지
ii. 아군 여부  (isFriendly)
7. MeleeAttacks.attackAndDealDamage()
a. 서버로  myCharMovement.CmdDealDamage 호출
i. 대상 및  데미지  Multiplier 정보  보냄
8. CharMovement.CmdDealDamage() - Cmd 함수들은  클라 → 서버  호출  하는  함수
a. 서버에서  Damageable 대상  찾아서  Damageable.attackAndDoDamage()
호출
b. 대상 스턴  할지  확인  (ItemHitBox 에  stun 을  걸건지  정보가  있음 )
9. Damageable.attackAndDoDamage()
a. HP 깎음  (Damageable 에  HP 수치가  있음 )
b. 넉백 호출
Collider 구성
ItemHitbox 가  무기에  달려  있음
Collider
데미지  값
데미지  타입
스턴 여부
넉백 수치  오버라이드  ( 기본
2.5)

---

## Page 2

Target 선정
CharMovement.findClosestT argetAndFace()
CheckSphere, OverlapSphere 통해서  주변  Enemy 는  확인  하지만  실제로  closest 를  sorting 하고  있진  않고  overlapSphere 에서
첫번째로  주어지는  타겟을  바라보도록  하고  있음
ItemHitBox.OnT riggerEnter()
해당 공격  중에  공격한  적이  아니면  모두  공격
레이블  없음