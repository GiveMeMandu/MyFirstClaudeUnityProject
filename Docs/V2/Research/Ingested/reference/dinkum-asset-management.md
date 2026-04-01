---
source: "D:\Krafton\Ref_Dinkum\에셋 관리 - 5minlab (5ml) - Wiki.pdf"
category: reference
format: PDF
file_size: 0.4MB
extracted: 2026-04-01 12:22
total_pages: 1
extracted_pages: 1
---

# 에셋 관리 - 5minlab (5ml) - Wiki

## Page 1

Wonje Hyun ( 현원제 )님이 작성 , 2024-05-20 에 최종  변경에셋 관리
개요
Addressable 을  쓴다 .
Addressable 참조  코드는  AssetResource 코드  안에서만  동작
실제 게임  구현  로직이  Addressable 을  참조하지  않게  하기
외부에서는  asset key 로  AssetResource 에  접근해서  에셋을  가져간다 .
AssetResourceManager
key 로  asset 로딩  
Scope 개념
에셋이  언제  언로딩  되어야  하는지  정의를  편하게  하기위해  Scope 를  정의
개별 에셋의  언로딩  시점을  제어하는게  아니라 , 에셋이  로딩될  때  포함되는  Scope 가  정해져있고 ,
Scope 를  언로딩하면  포함된  모든  에셋을  언로드하기
Scope 종류
Global : 앱  시작시  로딩해서  유지하는  에셋들
World : WorldMap 단위로  로딩할  에셋 . 다른  맵으로  이동  시  언로드한다 .
필요에  따라  Scope 는  추가될  수  있음
Case
World 로딩  시 , Map Data 를  보고  포함된  타일  / 오브젝트  등을  모두  체크하여  로딩이  필요한  리소스를  World scope 로  로딩한다 .
이후 World Map 을  생성한다 .
게임 진행  중 , 추가로  로딩되어야할  에셋이  있을  수  있다 .
해금되면서  새로  오브젝트가  생겼다거나 , 배치했을  때
이 경우  해당  시점에  추가로  에셋을  World scope 로  async 로딩한다 .
1초 이내의  로딩  딜레이가  발생할  수  있음
Lazy Loading 을  고려하여  Addressable 로  로딩되는  에셋에 , 로직이  포함되지  않도록  신경써야  할  수  있음
에셋 목록
종류 Asset Type scope 설명
Shader Shader Global 대부분  오브젝트를  커버할  큰  셰이더를  만들어  대부분  쓸것으로  예상 .
머터리얼  들에서  공통으로  참조할  것이므로  따로  분리하는  것이  좋다 .
타일 Mesh Mesh Global 타일 메쉬는  어떤  맵이든  비슷하게  공유될  것으로  항상  들고  있는게  맞겠다 .
메쉬 생성  자체를  다른식으로  할  수도  있어서 ( 아예  코드로  구성하기 ) 에셋  로딩이  필요없어질  수  있음
타일 Material Material Global or World월드에  따라  사용하는  타일종류가  많이  다르다면 , World scope 를  사용할  것
현재와  같이  월드에서  대부분의  타일을  포함하는  형태라면  Global scope 를  사용
월드 환경  정보 Scriptable Object
MaterialGlobal or World타일 Material 과  마찬가지로  얼마나  공유되느냐에  따라  Global / World scpoe 결정
캐릭터  파트 Prefab / FBX World 종류가  다양할  것임에  비해  실제  사용되는  오브젝트  개수는  적을  것이므로  필요한  것만  로딩해서  사용하기
타일 오브젝트  프리팹 Prefab / FBX World 종류가  다양하므로  필요한것만  로딩해서  사용
몬스터  모델 Prefab / FBX World 맵 마다  나오는  종류가  꽤  다를것으로  예상
참고문서
Addressables: Planning and best practices
Addressables 와  AssetBundle 사이의  이러한  긴밀한  관계를  염두에  두고  Addressables 콘텐츠를  구성할  때  가장  중요한  규칙은 함께 로드  및  언로드될  것으로  예상되는  개별  자산  세트를  포
함하는  AssetBundle 을  생성하는  것 입니다
Addressables Profiler 참고  하기  
번들설정  사용사례 .pdf
공식 매뉴얼
레이블  없음