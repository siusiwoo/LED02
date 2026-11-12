# LED02 — 랜덤 계산 반복 (심화 과제)

기업 현장실습(AMS) 세 번째 프로젝트 (12~17일차, 2026.11.03 ~ 11.10)

PC가 설정(반복 횟수 · 간격 · A/B 범위)을 보내면 보드가 랜덤 계산을 반복하고, 결과를 PC로 보내 표와 CSV에 기록합니다.

## 주요 내용
- 명령: `Status_READ 0x01` · `CMD_CALC 0x10` · `CMD_START 0x11` · `CMD_STOP 0x12` · `CMD_DONE 0x13` · `ACK` / `NAK`
- 가변 길이 값: `[칸 수][값 바이트]` (예: 5 → `01 05`, 300 → `02 01 2C`, -2 → `01 FE`)
  - PC `AddInt` / `ReadValue`, 보드 `write_value` / `read_value`
- 입력 검사(빈칸 · 형식 · 범위 · MIN ≤ MAX), 설정 저장 / 되돌리기(`settings.json`), 검사마다 새 CSV(`logs/`)
- 리팩터링: `Packet`(프로토콜 · CRC · 값 읽기/쓰기) · `Communication/SerialLink`(포트 연결/해제 · 스레드 · 이벤트) · `Models`(설정)

## 폴더
- `Board/` — 🟩 C (STM32CubeIDE). 작성한 코드는 `Core/Src/main.c`의 `USER CODE` 구역
- `PC/` — 🟦 C# WPF (.NET)

> `Drivers` 폴더는 용량 때문에 빼 두었습니다. `.ioc` 파일을 STM32CubeMX로 열어 코드를 생성하면 다시 만들어집니다.
