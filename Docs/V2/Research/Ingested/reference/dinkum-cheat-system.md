---
source: "D:\Krafton\Ref_Dinkum\개발용 치트 기능 - 5minlab (5ml) - Wiki.pdf"
category: reference
format: PDF
file_size: 2.2MB
extracted: 2026-04-01 12:22
total_pages: 7
extracted_pages: 7
---

# 개발용 치트 기능 - 5minlab (5ml) - Wiki

## Page 1

작성자  : Wonje Hyun ( 현원제 ), 최근  변경  : Taeyoon Um ( 엄태윤 ) - 2025-01-03개발용  치트  기능
UI 숨김  기능
에디터  / PC
F11
치트 페이지  열기
에디터  / PC 
F12
Ctrl+`
빌드
화면 상단  중앙을  빠르게  3 번  터치해서  SRDebugger 페이지  열기
Options/ShowCheatScreen

---

## Page 2

아이템추가
예제
additem ItemId_10005 3
아이템추가  10005
 커맨드
add_item
additem
아이템추가
파라미터
additem (ItemId)
additem (ItemId) (Count)
아이템  프리셋  추가
예제
giveme airport
비행장  증서와  관련된  재료  아이템까지  한  번에  추가함
giveme tool
모든 도구  아이템을  한번에  추가
giveme (recipeKey)
레시피가  존재하면  레시피에  필요한  재료를  생성해  줌 . giveme 대신에  recipe 도  동일함 . ex) recipe
도끼
giveme (ItemId)
위의 additem 과  같지만 , 비행장  증서  같은  workbench 인  경우에  관련  아이템  다  추가
Light 설정  관련  시간  고정
예제
envtimefix on
env_time_fix on 13
envtimefix off
환경시간고정  on
 커맨드
envtimefix
fixenvtime
환경시간고정
env_time_fix
fix_env_time
파라미터
envtimefix (on/off)
시간 파라미터  없을  경우  현재  시간으로  고정
envtimefix (on/off) ( 시간 :0~23)
시간 변경
예제
areatime 23 55 0
areatime 12

---

## Page 3

커맨드
areatime
파라미터
areatime ( 시 ) ( 분 ) ( 초 )
비어있는  시간  파라미터는  0 으로  고정
ex) areatime 11 5  => 11 시  05 분  00 초
(게임 )시간  빠르게  흐르기
예제
timescale 1200
커맨드
timescale
파라미터
timescale (value)
value = 시간  배율  ( 현실시간  대비  배율 )
제약
Host 모드에서만  사용가능합니다 (Single 모드 )
(엔진 )시간  조절
예제
unitytimescale 0.5
커맨드
unitytimescale
파라미터
timescale (value)
value = 시간  배율
제약
Host 모드에서만  올바르게  동작 (Single 모드 )
날씨 변경
예제
weather clear
날씨 비
커맨드
weather
날씨
파라미터
option (clear/cloudy/rainy/snow/fog/dust/ 맑음 / 흐림 / 비 / 눈 / 안개 / 먼지 )
랜덤 날씨
예제
randomweather on
랜덤날씨  켜기
커맨드
randomweather
랜덤날씨
파라미터
option (on/off/ 켜기 / 끄기 )
재화 추가
예제
currency gold 5000
커맨드
currency
파라미터
currency (type) (amount)
type = (gold,)
amount = 값
터치 컨트롤  변경

---

## Page 4

touch : 터치 UI On
touch off : 터치 UI Off
동물 스폰 , 언스폰
예제
spawnanimal AnimalId_Animal_Kangaroo_0002
unspawnanimal AnimalId_FlyBug_UlyssesButterfly_0010
unspawnanimal all
killanimal all
killanimal AnimalId_Animal_Kangaroo_0002
커맨드
spawnanimal
unspawnanimal
killanimal
파라미터
spawnanimal (animalKey)
unspawnanimal (animalKey)
unspawnanimal all
killanimal all
killanimal (animalKey)
낚시 
예제 - 낚싯대를  들고  있어야  합니다
catchfish AnimalId_Fish_Bluefish_0001
커맨드
catchfish
플레이어  킬
예제
killplayer self
커맨드
killplayer
플레이어  스피드
예제
playerspeed 50
버프추가
예제
addbuff BuffEffectId_Food_None_1
커맨드
addbuff
buffadd
버프추가
파라미터
addbuff (buffId)
퀘스트  일괄  깨기
예제
questlist Quest_Gstar_0010 Quest_Gstar_0020 Quest_Gstar_0030 Quest_Gstar_0040
Quest_Gstar_0070 Quest_Gstar_0310_1
questlist Quest_Gstar_0310,2,QuestGoal_Gstar_ETC_TameEmu_NoMarker
Quest_Gstar_0310,2,QuestGoal_Gstar_Inv_Saddle
커맨드
questlist
파라메터
questlist (QuestKey / QuestStepKey / QuestKey,StepIndex,QuestObjectiveKey)

---

## Page 5

퀘스트  치트버튼  on/off
예제
questcheat on
questcheat off
커맨드
questcheat
파라미터
questcheat (on/off) 
기타
Graphy FPS GUI 는  Ctrl + F11 로  On/Off 가능
위치 설정
예제
positionset 100 100
커맨드
positionset
파라미터
positionset ( x ) ( z )
기타
위치는  tile 기준으로  설정
현재 위치  가져오기
에제
positionget
커맨드
positionget
기타
위치는  타일  기준으로  반환
스텔스  모드  (적이  플레이어를  인식하지  않음 )
예제
stealth on
stealth off
커맨드
stealth
파라미터
stealth (on/off)
무적모드  ( 플레이어의  체력이  달지  않음 )
예제
invincible on
invincible off
커맨드
invincible
파라미터
invincible (on/off)

---

## Page 6

캐릭터  커스터마이즈  변경
예제
customize hair 2
커맨드
customize
파라미터
customize (hair/eye/nose/mouth/top/bottom/back/eyeacc/faceacc/headwear/hip/shoes) (idx:0 부터  시
작)
캐릭터  커스터마이즈  색상  변경
예제
custimizecolor skin 0
커맨드
customizecolor
파라미터
customizecolor (hair/eye/skin) (idx:0 부터  시작 )
앞에 있는  타일오브젝트  키  가져오기
예제
gettileobjectkey
커맨드
gettileobjectkey
타일오브젝트  스폰
예제
spawntileobject TileObjectId_Environment_Tree_0040
커맨드
spawntileobject
파라미터
spawntileobject (TileObjectKey)
제한
일부 타일오브젝트  생성  불가
제작대 , 농작물 , Stall 등
기타
타일오브젝트  키는  gettileobjectkey 로  가져오는  것을  추천
Audio 켜고  끄기
예제
audio off bgm
audio on amb
커맨드
audio
파라미터
audio (on/off) (master/bgm/sfx/amb/ui)
Audio 볼륨  조절
예제
audiovolume bgm 50
audiovolume master 0
audiovolume sfx 100
커맨드
audiovolume
파라미터
audio (master/bgm/sfx/amb/ui) ( 볼륨값 )
제한
볼륨값은  0~100 사이의  정수여야  합니다 .
튜토리얼  페이지  띄우기

---

## Page 7

예제
tutorialpage TutorialPage_Harvest_0001
커맨드
tutorialpage
파라미터
TutorialPageKey
튜토리얼  표시하기
예제
tutorial Tutorial_Minimap_0001
tutorial Tutorial_Inventory_0001
tutorial Tutorial_Quest_0001
커맨드
tutorial
파라메터
TutorialKey
섬 이동
예제
travel PlayerIsland
travel FriendIsland
커맨드
travel
파라메터
IslandType (PlayerIsland/FriendIsland)
FOV 변경
예제
fov 5
fov -5
커맨드
fov
파라미터
Delta
시야 거리  변경
예제
viewdistance 10
viewdistance -10
vd 10
vd -10
커맨드
viewdistnace
vd
파라미터
Delta
청크 거리  변경
예제
chunkdistance 10
chunkdistance -10
cd 10
cd -10
커맨드
chunkdistance
cd
파라미터  Delta
레이블  없음