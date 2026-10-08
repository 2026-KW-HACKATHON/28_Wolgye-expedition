# 🌙 월계원정대 (Wolgye Expedition)

2026 KW HACKATHON · 28팀 출품작

---

## 📖 소개

**월계원정대**는 실제 지도 위를 걸어 다니며 동네 곳곳에 숨어 있는 캐릭터 **'달수'** 를 찾아내는 Android 게임입니다.

- 🗺️ **지도 탐험** – GPS로 내 위치를 따라가는 Mapbox 지도 위에 달수가 나타납니다.
- 📷 **AR 포획** – 달수를 탭하면 AR 카메라가 열리고, 바닥 평면 위에 숨어 있는 달수를 찾아 사진을 찍어 잡습니다.
- 🔳 **QR 스탬프** – 제휴 장소의 QR 코드를 스캔하면 특별한 달수가 등장하고 스탬프가 쌓입니다.
- 🏠 **달수 방** – 잡은 달수들이 돌아다니는 나만의 방. 가구를 배치해 꾸미고, 달수를 돌려보내 재화를 얻을 수 있습니다.
- 📚 **도감** – 지금까지 만난 달수를 모아 보는 컬렉션.


## 📜 사용 에셋 및 라이선스

- [Mapbox Unity SDK](https://github.com/mapbox/mapbox-unity-sdk) – `MapboxSDK/LICENSE` 참고
- ithappy – Food Free, Cute Furniture Free
- Free Hyper Casual Street Food Pack, Free Stickman
- 폰트: Pretendard, 티머니 둥근바람체
- ZXing.Net, Serialized Collections

각 에셋의 라이선스는 해당 에셋 폴더 및 배포처의 약관을 따릅니다.

## 🎮 게임 흐름

```
Start ──▶ Map(지도) ──┬─ 달수 탭 ──▶ AR ──(촬영 성공)──▶ Reward(획득 결과) ──▶ Map
                      ├─ QR 스캔 ──▶ QR-AR ──(촬영 성공)──▶ Reward ──▶ Map (+스탬프)
                      └─ 내 방 ────▶ Room (꾸미기 / 달수 리스트 / 돌려보내기)
```

| 씬 | 설명 |
| --- | --- |
| `Start` | 타이틀 화면 |
| `Map` | Mapbox 지도 + GPS 위치 추적, 달수 스폰/디스폰, 상점·도감·쿠폰·QR 스캔 UI |
| `AR` | AR Foundation 평면 인식 → 화면 밖 평면에 달수를 숨겨 스폰, "주변을 둘러보세요" 가이드 |
| `QR-AR` | QR로 인식한 달수를 카메라 정면에 바로 소환 |
| `Reward` | 포획 사진 미리보기, 갤러리 저장 / 공유, 도감 등록 |
| `Room` | 그리드 기반 가구 배치, 보유 달수 AI 이동, 돌려보내기 보상 |

## ✨ 주요 기능

### 🗺️ 위치 기반 스폰
- 플레이어가 **20~40m(랜덤) 이동할 때마다** 주변 30m 안에 달수가 생성되고, 50m 이상 멀어지면 사라집니다.
- 특정 **스폰 구역**에 들어가면 구역별 가중치 테이블에 따라 달수가 등장합니다.

  | 구역 | 반경 |
  | --- | --- |
  | 광운대 광장 | 100m |
  | 우이천 달빛거리 | 150m |
  | 광운대 식당가 | 100m |

### 📷 AR 포획
- 달수는 주변을 둘러보며 찾아야 합니다.
- 달수는 대기 · 회전 · 배회 · 카메라 접근 · 앉기 등 가중치 기반 행동을 하며, 고개와 몸을 돌려 카메라를 바라봅니다.
- 달수가 화면 안쪽일 때 촬영하면 포획 성공입니다.

### 🔳 QR 스탬프 & 쿠폰
- ZXing으로 QR 코드를 인식합니다.
- 스캔할 때마다 스탬프 +1, 일정 개수가 모이면 쿠폰이 발급되고 스탬프가 초기화됩니다.

### 🏠 달수 방
- 그리드 위에 침대 · 의자 · 화분 · 소파 · 테이블을 배치하고 회전할 수 있습니다.
- 잡은 달수들이 방 안을 돌아다닙니다.
- 달수를 돌려보내면 희귀도에 따라 재화를 받습니다(희귀도 2 이상 100, 그 외 50).

### 🐣 달수 도감

| ID | 이름 | 희귀도 |
| --- | --- | :---: |
| Dalsu_001 | 달수 | ★ |
| Dalsu_002 | 너굴달수 | ★★★ |
| Dalsu_003 | 카페달수 | ★ |
| Dalsu_004 | 햄버거달수 | ★★★ |
| Dalsu_005 | 케이크달수 | ★★ |
| Dalsu_006 | 피자달수 | ★★★ |
| Dalsu_007 | 후라이달수 | ★★ |

## 🛠 기술 스택

| 분류 | 사용 기술 |
| --- | --- |
| 엔진 | Unity **6000.3.9f1** (Unity 6), URP 17.3 |
| 플랫폼 | Android (minSdk 29, IL2CPP, ARM64) |
| AR | AR Foundation 6.3 + ARCore |
| 지도 / 위치 | Mapbox Unity SDK 3.1.1 (`MapboxSDK/` 로컬 패키지) |
| QR | ZXing.Net |
| 기타 | Cinemachine, AI Navigation, Input System, TextMesh Pro, Newtonsoft.Json |
| 네이티브 | Android 플러그인 – 갤러리 저장 · 사진 공유 |

## 📁 프로젝트 구조

```
Assets/
├─ Scenes/            # Start, Map, AR, QR-AR, Reward, Room
├─ Scripts/
│  ├─ Map/            # GPS 위치(PlayerLocation), 달수 스폰/디스폰(DalsuSpawner), 지도 입력
│  ├─ AR/             # AR 스폰, 달수 행동/시선 제어, 촬영·포획, 결과 UI, 갤러리 저장/공유
│  ├─ Room/DecoSystem # 그리드 기반 가구 배치 시스템
│  ├─ Character_AI/   # 방 안 달수 NavMesh 이동
│  ├─ Camera/         # 카메라 모드 전환, 오버뷰 팬, 가림 페이드
│  ├─ Managers/       # SceneLoader(로딩 화면), BGM, UI, 스탬프
│  ├─ Data/           # DalsuData / Database / SpawnArea, 저장, 재화
│  └─ UI/             # 도감 슬롯, 재화 표시
├─ Resources/
│  ├─ ScriptableObject/Dalsu/          # 달수 데이터
│  ├─ ScriptableObject/DalsuSpawnArea/ # 스폰 구역 데이터
│  └─ Mapbox/MapboxConfiguration.txt   # Mapbox Access Token
└─ Plugins/Android/   # Gradle 템플릿, 네이티브 공유 플러그인
MapboxSDK/            # Mapbox Unity SDK (로컬 패키지)
```

## 🧩 콘텐츠 추가하기

- **새 달수**: `Create ▸ Dalsu ▸ Dalsu Data`로 에셋을 만들고 `id`, 이름, 희귀도, 아이콘, 프리팹을 지정한 뒤 `DalsuDatabase`에 등록합니다.
- **새 스폰 구역**: `Create ▸ Dalsu ▸ Dalsu Spawn Area`로 위도/경도, 반경, 최대 스폰 수, 등장 달수와 가중치를 설정하고 `DalsuSpawner`에 연결합니다.
- **새 가구**: `Create ▸ Room ▸ Furniture Data`로 ID, 아이콘, 프리팹, 차지하는 칸 수(`size`)를 지정합니다.
- **QR 코드**: 달수 ID 문자열(예: `Dalsu_005`)을 그대로 QR로 인코딩하면 됩니다.