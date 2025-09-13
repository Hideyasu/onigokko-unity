# Tasks: リアル鬼ごっこゲーム「鬼 vs 陰陽師」

**入力**: `/specs/001-unity-dead-by/` の設計文書
**前提条件**: plan.md、data-model.md、contracts/firebase-api.json、quickstart.md

## 実行フロー (main)
```
1. plan.md から機能ディレクトリをロード
   → 見つからない場合: ERROR "実装計画が見つかりません"
   → 抽出: 技術スタック、ライブラリ、構造
2. オプション設計文書をロード:
   → data-model.md: エンティティ抽出 → モデルタスク
   → contracts/: 各ファイル → 契約テストタスク
   → quickstart.md: テストシナリオ抽出
3. カテゴリ別にタスクを生成:
   → セットアップ: プロジェクト初期化、依存関係、linting
   → テスト: 契約テスト、統合テスト
   → コア: モデル、サービス、コンポーネント
   → 統合: Firebase、UI、ログ
   → 仕上げ: ユニットテスト、パフォーマンス、ドキュメント
4. タスクルールを適用:
   → 異なるファイル = [P] で並列化マーク
   → 同じファイル = 順次実行（[P]なし）
   → 実装前にテスト（TDD）
5. タスクを連番で番号付け（T001、T002...）
6. 依存関係グラフを生成
7. 並列実行例を作成
8. タスク完全性を検証:
   → 全契約にテストがある？
   → 全エンティティにモデルがある？
   → 全エンドポイントが実装される？
9. 返却: SUCCESS（タスク実行準備完了）
```

## フォーマット: `[ID] [P?] Description`
- **[P]**: 並列実行可能（異なるファイル、依存関係なし）
- 説明に正確なファイルパスを含める

## パス規約
Unity プロジェクト構造に基づく:
- **Assets/Scripts/**: メインスクリプト
- **Assets/Tests/**: テストファイル
- **Packages/**: カスタムパッケージ

## Phase 3.1: セットアップ
- [ ] T001 Unity プロジェクト構造を実装計画に従って作成
- [ ] T002 Firebase SDK for Unity とその他依存関係を初期化
- [ ] T003 [P] Unity エディタスクリプトとプロジェクト設定を構成
- [ ] T004 [P] iOS/Android ビルド設定と権限を構成
- [ ] T005 Firebase設定ファイルをAssets/フォルダに配置

## Phase 3.2: テストファースト（TDD）⚠️ 3.3より前に完了必須
**重要: これらのテストは実装前に書かれ、失敗しなければならない**

### Contractテスト（Firebase API）
- [ ] T006 [P] セッション作成契約テスト: Assets/Tests/Contract/SessionCreateTest.cs
- [ ] T007 [P] セッション参加契約テスト: Assets/Tests/Contract/SessionJoinTest.cs
- [ ] T008 [P] プレイヤー位置更新契約テスト: Assets/Tests/Contract/LocationUpdateTest.cs
- [ ] T009 [P] 攻撃アクション契約テスト: Assets/Tests/Contract/AttackActionTest.cs
- [ ] T010 [P] トラップ設置契約テスト: Assets/Tests/Contract/TrapPlaceTest.cs

### 統合テスト（ユーザーストーリー）
- [ ] T011 [P] ゲームフロー統合テスト: Assets/Tests/Integration/GameFlowTest.cs
- [ ] T012 [P] 心音システム統合テスト: Assets/Tests/Integration/HeartbeatSystemTest.cs
- [ ] T013 [P] 位置追跡統合テスト: Assets/Tests/Integration/LocationTrackingTest.cs
- [ ] T014 [P] 戦闘システム統合テスト: Assets/Tests/Integration/CombatSystemTest.cs
- [ ] T015 [P] 勝利条件統合テスト: Assets/Tests/Integration/WinConditionTest.cs

## Phase 3.3: コア実装（テスト失敗後のみ）

### データモデル
- [ ] T016 [P] Playerモデル: Assets/Scripts/Core/Data/Player.cs
- [ ] T017 [P] GameSessionモデル: Assets/Scripts/Core/Data/GameSession.cs
- [ ] T018 [P] Trapモデル: Assets/Scripts/Core/Data/Trap.cs
- [ ] T019 [P] Ritualモデル: Assets/Scripts/Core/Data/Ritual.cs
- [ ] T020 [P] Footprintモデル: Assets/Scripts/Core/Data/Footprint.cs

### コアシステム
- [ ] T021 GameManager: Assets/Scripts/Core/GameManager.cs
- [ ] T022 SessionManager: Assets/Scripts/Core/SessionManager.cs
- [ ] T023 PlayerController: Assets/Scripts/Core/PlayerController.cs

### 位置追跡システム（com.onigokko.location パッケージ）
- [ ] T024 [P] LocationTracker: Packages/com.onigokko.location/Runtime/LocationTracker.cs
- [ ] T025 [P] GPSService: Packages/com.onigokko.location/Runtime/GPSService.cs

### 心音システム（com.onigokko.heartbeat パッケージ）
- [ ] T026 [P] HeartbeatSystem: Packages/com.onigokko.heartbeat/Runtime/HeartbeatSystem.cs
- [ ] T027 [P] ProximityDetector: Packages/com.onigokko.heartbeat/Runtime/ProximityDetector.cs

### 戦闘システム（com.onigokko.combat パッケージ）
- [ ] T028 [P] AttackSystem: Packages/com.onigokko.combat/Runtime/AttackSystem.cs
- [ ] T029 [P] TrapSystem: Packages/com.onigokko.combat/Runtime/TrapSystem.cs

### Firebase APIエンドポイント実装
- [ ] T030 セッション作成エンドポイント実装: Assets/Scripts/Firebase/SessionAPI.cs
- [ ] T031 プレイヤー位置更新エンドポイント実装: Assets/Scripts/Firebase/PlayerAPI.cs
- [ ] T032 攻撃アクションエンドポイント実装: Assets/Scripts/Firebase/CombatAPI.cs
- [ ] T033 トラップ管理エンドポイント実装: Assets/Scripts/Firebase/TrapAPI.cs

## Phase 3.4: 統合

### Firebase統合
- [ ] T034 Firebase Realtime Database接続とリスナー設定
- [ ] T035 Firebase認証（匿名）統合
- [ ] T036 リアルタイムデータ同期とエラーハンドリング

### UIシステム
- [ ] T037 [P] LobbyUI: Assets/Scripts/UI/LobbyUI.cs
- [ ] T038 [P] GameHUD: Assets/Scripts/UI/GameHUD.cs
- [ ] T039 UIイベントハンドリングとゲームロジック接続

### システム統合
- [ ] T040 LocationTrackerとHeartbeatSystem統合
- [ ] T041 CombatSystemとUI統合
- [ ] T042 全システム間の通信とイベント管理

## Phase 3.5: 仕上げ

### ユニットテスト
- [ ] T043 [P] 距離計算ユニットテスト: Assets/Tests/Unit/DistanceCalculationTest.cs
- [ ] T044 [P] データ検証ユニットテスト: Assets/Tests/Unit/DataValidationTest.cs
- [ ] T045 [P] ゲームルールユニットテスト: Assets/Tests/Unit/GameRulesTest.cs

### パフォーマンス最適化
- [ ] T046 30fps制限とバッテリー最適化実装
- [ ] T047 位置更新頻度の動的調整実装
- [ ] T048 メモリ使用量とガベージコレクション最適化

### ドキュメントと最終調整
- [ ] T049 [P] パッケージREADME更新: Packages/*/README.md
- [ ] T050 コード重複除去とリファクタリング
- [ ] T051 quickstart.mdの手順検証とテスト実行

## 依存関係
- セットアップ（T001-T005）→ テスト（T006-T015）→ 実装（T016-T042）→ 仕上げ（T043-T051）
- T021（GameManager）は T022（SessionManager）をブロック
- T024-T025（位置追跡）は T026-T027（心音）をブロック
- T030-T033（Firebase API）は T034-T036（Firebase統合）をブロック
- T037-T038（UI）は T039（UIイベント）をブロック

## 並列実行例

### Phase 3.2 テスト（T006-T015）を同時実行:
```
Task: "セッション作成契約テスト: Assets/Tests/Contract/SessionCreateTest.cs"
Task: "セッション参加契約テスト: Assets/Tests/Contract/SessionJoinTest.cs"
Task: "プレイヤー位置更新契約テスト: Assets/Tests/Contract/LocationUpdateTest.cs"
Task: "攻撃アクション契約テスト: Assets/Tests/Contract/AttackActionTest.cs"
Task: "トラップ設置契約テスト: Assets/Tests/Contract/TrapPlaceTest.cs"
```

### Phase 3.3 データモデル（T016-T020）を同時実行:
```
Task: "Playerモデル作成: Assets/Scripts/Core/Data/Player.cs"
Task: "GameSessionモデル作成: Assets/Scripts/Core/Data/GameSession.cs"
Task: "Trapモデル作成: Assets/Scripts/Core/Data/Trap.cs"
Task: "Ritualモデル作成: Assets/Scripts/Core/Data/Ritual.cs"
Task: "Footprintモデル作成: Assets/Scripts/Core/Data/Footprint.cs"
```

### Phase 3.3 パッケージシステム（T024-T029）を同時実行:
```
Task: "LocationTracker実装: Packages/com.onigokko.location/Runtime/LocationTracker.cs"
Task: "GPSService実装: Packages/com.onigokko.location/Runtime/GPSService.cs"
Task: "HeartbeatSystem実装: Packages/com.onigokko.heartbeat/Runtime/HeartbeatSystem.cs"
Task: "ProximityDetector実装: Packages/com.onigokko.heartbeat/Runtime/ProximityDetector.cs"
Task: "AttackSystem実装: Packages/com.onigokko.combat/Runtime/AttackSystem.cs"
Task: "TrapSystem実装: Packages/com.onigokko.combat/Runtime/TrapSystem.cs"
```

## 注意事項
- [P] タスク = 異なるファイル、依存関係なし
- 実装前にテストが失敗することを確認
- 各タスク後にコミット
- 回避: 曖昧なタスク、同じファイルでの競合

## タスク生成ルール
*main()実行中に適用*

1. **契約から**:
   - 各契約ファイル → 契約テストタスク [P]
   - 各エンドポイント → 実装タスク

2. **データモデルから**:
   - 各エンティティ → モデル作成タスク [P]
   - 関係性 → サービス層タスク

3. **ユーザーストーリーから**:
   - 各ストーリー → 統合テスト [P]
   - クイックスタートシナリオ → 検証タスク

4. **順序**:
   - セットアップ → テスト → モデル → サービス → エンドポイント → 仕上げ
   - 依存関係は並列実行をブロック

## 検証チェックリスト
*main()が返却前にチェック*

- [x] 全契約に対応するテストがある
- [x] 全エンティティにモデルタスクがある
- [x] 全テストが実装前にある
- [x] 並列タスクが真に独立している
- [x] 各タスクが正確なファイルパスを指定
- [x] [P]タスクが同じファイルを変更しない

## 24時間ハッカソン特別考慮事項

### MVP優先順位（時間不足の場合）
1. **必須**: T001-T005（セットアップ）、T021-T023（コア）、T026-T027（心音）
2. **重要**: T024-T025（位置追跡）、T030-T031（Firebase基本API）
3. **オプション**: T028-T029（戦闘）、T037-T038（UI改善）

### 緊急時短縮戦略
- 統合テスト（T011-T015）を手動テストに置き換え可能
- ユニットテスト（T043-T045）を後回し可能
- UI（T037-T038）を最小限の実装に縮小可能