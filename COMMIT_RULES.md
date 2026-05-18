# Commit Rules

이 프로젝트는 Unity 튜토리얼 게임 프로젝트이므로, 커밋은 "무엇을 왜 바꿨는지"가 나중에 바로 보이도록 작게 나눕니다.

## 기본 원칙

1. 하나의 커밋에는 하나의 목적만 담습니다.
2. Unity 자동 생성 폴더는 커밋하지 않습니다.
3. 게임플레이 동작이 바뀌면 가능하면 Issue 또는 PR에 짧은 영상/GIF를 남깁니다.
4. 버그 수정은 재현 방법, 기대 동작, 실제 동작을 먼저 정리한 뒤 수정합니다.
5. 큰 에셋 파일은 Git LFS 추적 대상인지 확인합니다.

## 커밋 메시지 형식

```text
type: short summary
```

예시:

```text
fix: prevent player sliding on moving platform
feat: add rotating coin pickup
tune: adjust wall climb top-out distance
docs: add commit rules
```

## Type 목록

`feat`: 새 기능 추가

예: 코인, 체크포인트, 문 열기, 새로운 퍼즐 시스템

`fix`: 버그 수정

예: 벽타기 오류, 낙하 리스폰 오류, hazard 충돌 처리 오류

`tune`: 게임플레이 수치 조정

예: 이동 속도, 점프 힘, climb 속도, 카메라 거리, trigger 범위

`level`: 씬/스테이지 구성 변경

예: Floor 배치, wall 위치, box 배치, waypoint 수정

`art`: 아트/머티리얼/텍스처 변경

예: Material 색상, checker texture, 모델 교체

`audio`: 사운드 변경

예: 효과음, 배경음, 발소리

`ui`: UI 변경

예: coin counter, game over text, game clear text

`docs`: 문서 변경

예: README, 개발 규칙, 회의 메모

`chore`: 설정/정리 작업

예: `.gitignore`, Git LFS 설정, 패키지 설정

`refactor`: 동작은 유지하고 코드 구조만 개선

예: PlayerMovement 분리, GameManager 정리

## 브랜치 이름

```text
type/short-description
```

예시:

```text
fix/wall-climb-down
feat/coin-counter
tune/player-jump
level/floor-2-box-puzzle
docs/commit-rules
```

## Issue를 만들 기준

작은 수치 수정이나 오타는 바로 커밋해도 됩니다.

Issue를 만드는 것이 좋은 경우:

1. 재현 가능한 버그가 있다.
2. 플레이어 조작감이나 게임 규칙이 바뀐다.
3. 씬 구성이나 퍼즐 흐름이 바뀐다.
4. 작업 범위가 30분 이상 걸릴 것 같다.
5. 다른 팀원이 확인해야 한다.

## PR에 적을 내용

PR에는 최소한 아래 내용을 적습니다.

```markdown
## Summary
- 무엇을 바꿨는지

## Test
- Unity Play Mode에서 확인한 내용
- 실행한 빌드/검증 명령

## Notes
- 조작감, 영상/GIF, 남은 이슈
```

## Unity 파일 주의사항

커밋에 포함할 주요 폴더:

```text
Assets/
Packages/
ProjectSettings/
```

커밋하지 않는 폴더:

```text
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
```

큰 에셋 후보:

```text
*.fbx
*.blend
*.psd
*.png
*.tga
*.wav
*.mp3
*.mp4
*.mov
*.unitypackage
```

이 파일들은 Git LFS로 관리합니다.

## 추천 작업 흐름

```bash
git checkout main
git pull
git checkout -b fix/example-bug

# 수정 후
git status
git add .
git commit -m "fix: short summary"
git push -u origin HEAD
```

그 다음 GitHub에서 PR을 만들고, 확인이 끝나면 `main`에 merge합니다.
