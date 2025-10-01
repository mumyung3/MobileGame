# 🍞 Cooking Bread

> **모바일 베이커리 시뮬레이션 게임**
> Unity로 개발된 iOS 아이들/경영 시뮬레이션 게임

## 📱 게임 소개

Cooking Bread는 플레이어가 베이커리를 운영하며 고객들에게 빵을 판매하는 모바일 게임입니다. 고객 대기열 시스템과 실시간 상호작용을 통해 몰입감 있는 경영 시뮬레이션 경험을 제공합니다.

<div align="center">
  <img src="./Documentation/Photo/스크린샷 2025-10-01 오후 9.12.44.png" width="300" alt="메인 게임화면">
</div>

### 🎮 주요 기능

- **실시간 고객 서비스**: 랜덤 스폰되는 고객들의 주문 처리
- **대기열 시스템**: 큐(Queue) 자료구조를 활용한 고객 대기열 관리
- **다양한 서비스 옵션**: 테이크아웃과 매장 내 식사 시스템
- **업그레이드 시스템**: 게임 진행에 따른 장비 및 효율성 개선
- **인터랙티브 UI**: 직관적인 터치 기반 조작

### 🎬 게임플레이 영상
[![Cooking Bread 게임플레이](https://img.youtube.com/vi/Soz8vazko1E/maxresdefault.jpg)](https://youtube.com/shorts/Soz8vazko1E?feature=share)
> 클릭하여 YouTube에서 게임플레이 영상 시청

### 📸 게임 스크린샷

<div style="overflow-x: auto; white-space: nowrap;">
  <img src="./Documentation/Photo/스크린샷 2025-10-01 오후 9.13.18.png" width="250" alt="고객 서비스" style="display: inline-block; margin-right: 10px;">
  <img src="./Documentation/Photo/스크린샷 2025-10-01 오후 9.13.38.png" width="250" alt="업그레이드 화면" style="display: inline-block;">
</div>

## 🛠 기술 스택

- **게임 엔진**: Unity 2021.3.15f1
- **개발 언어**: C#
- **플랫폼**: iOS
- **개발 환경**: macOS, Xcode
- **에셋**: TextMesh Pro, Unity uGUI

## 🎯 핵심 시스템

### 1. 고객 관리 시스템
- **랜덤 스폰**: 다양한 타입의 고객이 무작위로 등장
- **대기열 구현**: Queue 자료구조를 활용한 FIFO 방식의 고객 대기열
- **서비스 분기**: 테이크아웃 vs 매장 내 식사 로직 분리

### 2. 게임플레이 메커니즘
- **주문 처리**: 고객별 맞춤 주문 시스템
- **자원 관리**: 빵 생산과 재고 관리
- **수익 시스템**: 실시간 매출 계산 및 UI 반영

### 3. 업그레이드 시스템
- **장비 개선**: 오븐, 카운터 등 시설 업그레이드
- **효율성 증대**: 생산 속도 및 용량 개선
- **애니메이션 연동**: 업그레이드 시각적 피드백

## 🔧 기술적 구현 특징

### 대기열 시스템 (Queue Implementation)
```csharp
// 고객 대기열 관리를 위한 큐 자료구조 활용
Queue<Customer> customerQueue = new Queue<Customer>();

// 고객 추가 및 처리
customerQueue.Enqueue(newCustomer);
Customer currentCustomer = customerQueue.Dequeue();
```

<div align="center">
  <img src="./Documentation/Photo/스크린샷 2025-10-01 오후 9.14.30.png" width="300" alt="대기열 시스템 구현">
  <p><em>구현된 고객 대기열 시스템</em></p>
</div>

### 구현 특징
- **간단한 게임 로직**: 베이커리 시뮬레이션의 핵심 기능에 집중
- **모바일 터치 UI**: iOS 디바이스 터치 인터페이스 지원

## 📂 프로젝트 구조

```
SuperCent/
├── Documentation/          # 설계 문서
│   ├── PROJECT_DESIGN.md
│   └── 다이어그램 파일들
├── MobileGame/
│   ├── Assets/
│   │   ├── Scenes/         # 게임 씬
│   │   ├── Prefabs/        # 게임 오브젝트
│   │   └── Practice/       # 에셋 리소스
│   └── ProjectSettings/
└── Screenshots/            # 게임 스크린샷
```

## 🚀 설치 및 실행

### 요구사항
- Unity 2021.3.15f1 이상
- iOS Build Support 모듈
- Xcode (macOS에서 iOS 빌드 시)

### 실행 방법
1. 프로젝트 클론
```bash
git clone [repository-url]
cd SuperCent
```

2. Unity에서 MobileGame 폴더 열기
3. iOS 빌드 설정 후 디바이스 또는 시뮬레이터에서 실행

## 📈 개발 성과

- **큐 자료구조 실제 적용**: 게임 로직에 자료구조 개념을 성공적으로 구현
- **Unity 모바일 게임 개발**: iOS 플랫폼용 베이커리 시뮬레이션 게임 완성
- **간단하고 직관적인 게임플레이**: 핵심 기능에 집중한 심플한 게임 시스템

## 🎯 향후 계획

### 📱 상용화 목표
- [ ] **Unity Ads 연동**: 리워드 광고 및 배너 광고 시스템 구축
- [ ] **인앱 결제 시스템**: 프리미엄 기능 및 아이템 구매 기능
- [ ] **App Store 배포**: 완성된 게임을 Apple App Store에 정식 출시
- [ ] **수익화 모델 구축**: 광고 수익과 인앱 결제를 통한 지속 가능한 수익 구조

### 🎮 게임 콘텐츠 확장
- [ ] 더 다양한 빵 종류 및 레시피 추가
- [ ] 확장된 업그레이드 시스템 및 진행 요소
- [ ] 성취 시스템 및 일일 퀘스트
- [ ] 소셜 기능 (순위표, SNS 공유)

## 📧 연락처

개발자: [Mumyung]
이메일: [eoalsrud@naver.com]

---

*이 프로젝트는 Unity 모바일 게임 개발 역량을 보여주기 위한 포트폴리오 프로젝트입니다.*