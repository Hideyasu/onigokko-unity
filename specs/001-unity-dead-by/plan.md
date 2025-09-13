# 実装計画: リアル鬼ごっこゲーム「鬼 vs 陰陽師」

**ブランチ**: `001-unity-dead-by` | **日付**: 2025-09-13 | **仕様書**: [spec.md](./spec.md)
**入力**: `/specs/001-unity-dead-by/spec.md` の機能仕様書

## 実行フロー (/plan コマンドスコープ)
```
1. 入力パスから機能仕様書をロード
   → 見つからない場合: ERROR "仕様書が {path} にありません"
2. 技術コンテキストを記入（要確認事項をスキャン）
   → コンテキストからプロジェクトタイプを検出（web=frontend+backend、mobile=app+api）
   → プロジェクトタイプに基づいて構造決定を設定
3. 下記の憲法チェックセクションを評価
   → 違反が存在する場合: 複雑性追跡に文書化
   → 正当化が不可能な場合: ERROR "まずアプローチを簡素化してください"
   → 進捗追跡を更新: 初期憲法チェック
4. Phase 0 を実行 → research.md
   → 要確認事項が残っている場合: ERROR "不明点を解決してください"
5. Phase 1 を実行 → contracts、data-model.md、quickstart.md、CLAUDE.md
6. 憲法チェックセクションを再評価
   → 新しい違反がある場合: 設計をリファクタリング、Phase 1 に戻る
   → 進捗追跡を更新: 設計後憲法チェック
7. Phase 2 を計画 → タスク生成アプローチを説明（tasks.md は作成しない）
8. 停止 - /tasks コマンドの準備完了
```

**重要**: /plan コマンドはステップ 7 で停止します。Phase 2-4 は他のコマンドで実行されます：
- Phase 2: /tasks コマンドが tasks.md を作成
- Phase 3-4: 実装の実行（手動またはツール経由）

## 概要
Dead by Daylight 風のリアル鬼ごっこゲームを Unity で開発。鬼（1人）vs 陰陽師（4人）の非対称対戦で、位置情報とリアルタイム通信を活用。心音判定システムをコア機能として、24時間ハッカソンで実装可能な MVP を目指す。

## 技術コンテキスト
**言語/バージョン**: C# / .NET Standard 2.1 (Unity 2022.3 LTS)
**主要依存関係**: Unity Engine、Firebase SDK (Realtime Database、Auth)、Google Maps SDK
**ストレージ**: Firebase Realtime Database（ゲームセッション、プレイヤー状態）
**テスティング**: Unity Test Framework、NUnit
**ターゲットプラットフォーム**: iOS 14+ / Android API 26+（モバイル）
**プロジェクトタイプ**: mobile - Unity モバイルアプリ + Firebase バックエンド
**パフォーマンス目標**:
  - 位置更新頻度: 1-5秒間隔
  - リアルタイム同期: <500ms 遅延
  - バッテリー持続: 30-60分のゲームプレイ
**制約**:
  - バックグラウンド位置追跡の制限（iOS/Android）
  - 24時間ハッカソンでの実装
  - GPS精度: ±5-10m の誤差を許容
**スケール/スコープ**:
  - 同時プレイヤー: 5人/セッション
  - 同時セッション数: 10-20（ハッカソンデモ用）
  - プレイエリア: 半径500m

## 憲法チェック
*ゲート: Phase 0 研究前に合格する必要があります。Phase 1 設計後に再チェック。*

**シンプリシティ**:
- プロジェクト数: 2（Unity モバイルアプリ、Firebase functions）
- フレームワークを直接使用？ はい（Unity + Firebase SDK 直接使用）
- 単一データモデル？ はい（Firebase Realtime Database のみ）
- パターンを回避？ はい（シンプルな MonoBehaviour ベース）

**アーキテクチャ**:
- すべての機能をライブラリとして？ Unity では Package として実装
- ライブラリリスト:
  - LocationTracking: GPS位置追跡と共有
  - HeartbeatSystem: 心音判定とバイブレーション
  - GameSession: ゲームセッション管理
  - PlayerActions: 攻撃/トラップシステム
- ライブラリごとの CLI: Unity Editor のカスタムツールとして提供
- ライブラリドキュメント: README.md + コード内コメント

**テスティング（交渉不可）**:
- RED-GREEN-Refactor サイクルを強制？ はい
- Git コミットは実装前にテストを表示？ はい
- 順序: Contract→Integration→E2E→Unit を厳密に遵守？ はい
- 実際の依存関係を使用？ Firebase エミュレータ使用
- 統合テスト対象: 位置同期、セッション管理、勝利条件
- 禁止事項: テスト前の実装、RED フェーズのスキップ

**オブザーバビリティ**:
- 構造化ログ含む？ Unity Debug.Log + Firebase Analytics
- フロントエンドログ → バックエンド？ Firebase Crashlytics に送信
- エラーコンテキスト十分？ はい（プレイヤーID、セッションID、位置情報含む）

**バージョニング**:
- バージョン番号割り当て？ 0.1.0（ハッカソン MVP）
- BUILD は変更ごとに増加？ はい
- 破壊的変更の処理？ N/A（初回リリース）

## プロジェクト構造

### ドキュメント（この機能）
```
specs/001-unity-dead-by/
├── plan.md              # このファイル（/plan コマンド出力）
├── research.md          # Phase 0 出力（/plan コマンド）
├── data-model.md        # Phase 1 出力（/plan コマンド）
├── quickstart.md        # Phase 1 出力（/plan コマンド）
├── contracts/           # Phase 1 出力（/plan コマンド）
└── tasks.md             # Phase 2 出力（/tasks コマンド - /plan では作成しない）
```

### ソースコード（リポジトリルート）
```
# Unity プロジェクト構造
Assets/
├── Scripts/
│   ├── Core/                    # コアゲームロジック
│   │   ├── GameManager.cs       # ゲーム全体の管理
│   │   ├── SessionManager.cs    # セッション管理
│   │   └── PlayerController.cs  # プレイヤー制御
│   ├── Location/                # 位置追跡システム
│   │   ├── LocationTracker.cs   # 位置追跡メイン
│   │   └── GPSService.cs        # GPS サービスラッパー
│   ├── Heartbeat/               # 心音システム
│   │   ├── HeartbeatSystem.cs   # 心音判定メイン
│   │   └── ProximityDetector.cs # 近接検出
│   ├── Combat/                  # 戦闘システム
│   │   ├── AttackSystem.cs      # 攻撃処理
│   │   └── TrapSystem.cs        # トラップ処理
│   └── UI/                      # UI コンポーネント
│       ├── GameHUD.cs           # ゲーム中 UI
│       └── LobbyUI.cs           # ロビー UI
├── Prefabs/                     # プレハブ
├── Materials/                   # マテリアル
├── Sounds/                      # サウンドアセット
│   └── Heartbeat/               # 心音効果音
└── Tests/                       # テスト
    ├── EditMode/                # エディタテスト
    └── PlayMode/                # プレイモードテスト

Packages/                        # カスタムパッケージ
├── com.onigokko.location/       # 位置追跡パッケージ
├── com.onigokko.heartbeat/      # 心音システムパッケージ
├── com.onigokko.session/        # セッション管理パッケージ
└── com.onigokko.combat/         # 戦闘システムパッケージ

ProjectSettings/                 # Unity 設定ファイル
└── [Unity 設定ファイル群]
```

**構造決定**: オプション 3（Mobile + API）- Unity モバイルアプリと Firebase バックエンド

## Phase 0: アウトラインと研究
1. **技術コンテキストから不明点を抽出**:
   - Unity での GPS バックグラウンド追跡の実装方法
   - Firebase Realtime Database でのリアルタイム位置同期
   - Bluetooth/NearBy API vs GPS のみでの距離測定
   - iOS/Android の位置情報権限の取得フロー
   - バッテリー消費を抑える位置更新戦略

2. **研究エージェントの生成と実行**:
   ```
   Task: "Unity でのバックグラウンド位置追跡を iOS/Android で研究"
   Task: "Unity での Firebase Realtime Database ベストプラクティスを調査"
   Task: "近接検出方法の研究（GPS vs Bluetooth）"
   Task: "位置情報ベースモバイルゲームのバッテリー最適化を調査"
   Task: "Unity モバイル通知システムのバックグラウンドアラート研究"
   ```

3. **research.md での調査結果の統合**:
   - 決定: [選択した内容]
   - 根拠: [選択理由]
   - 検討した代替案: [評価した他の選択肢]

**出力**: すべての要確認事項が解決された research.md

## Phase 1: 設計と契約
*前提条件: research.md 完了*

1. **機能仕様書からエンティティを抽出** → `data-model.md`:
   - Player（id、role、location、health、status）
   - GameSession（id、players、state、startTime、ritualProgress）
   - Trap（id、location、radius、duration、owner）
   - Ritual（id、location、progress、requiredPlayers）
   - Footprint（id、playerId、path、timestamp、duration）

2. **機能要件から API 契約を生成**:
   - POST /sessions/create - ゲームセッション作成
   - POST /sessions/{id}/join - セッション参加
   - POST /sessions/{id}/start - ゲーム開始
   - PUT /players/{id}/location - 位置更新
   - POST /players/{id}/attack - 攻撃実行
   - POST /traps/place - トラップ設置
   - GET /sessions/{id}/state - ゲーム状態取得

3. **契約からコントラクトテストを生成**:
   - セッション作成とプレイヤー割り当てのテスト
   - 位置更新と近接計算のテスト
   - 攻撃とトラップメカニクスのテスト
   - 勝利条件のテスト

4. **ユーザーストーリーからテストシナリオを抽出**:
   - 5人のプレイヤーでゲーム開始
   - 心音システムの距離判定（50m、30m、10m）
   - 攻撃とトラップの相互作用
   - 勝利条件の判定

5. **CLAUDE.md ファイルの更新**:
   - Unity と Firebase の設定情報
   - ハッカソン向けの実装優先順位
   - テスト実行コマンド

**出力**: data-model.md、/contracts/*、失敗するテスト、quickstart.md、CLAUDE.md

## Phase 2: タスク計画アプローチ
*このセクションは /tasks コマンドが実行する内容を説明 - /plan では実行しない*

**タスク生成戦略**:
- Unity プロジェクトの初期設定タスク
- Firebase 連携設定タスク
- 各 Package の作成タスク（LocationTracking、HeartbeatSystem など）
- UI 画面の実装タスク（ロビー、ゲーム HUD）
- 各機能に対するテストタスクの生成

**順序付け戦略**:
1. プロジェクト設定とパッケージ構成
2. コアシステム（GameManager、SessionManager）
3. 位置追跡システム
4. 心音判定システム（MVP のコア）
5. 戦闘システム
6. UI 実装
7. 統合テスト

**推定出力**: tasks.md に 30-35 個の番号付き、順序付けされたタスク

**重要**: このフェーズは /tasks コマンドによって実行され、/plan では実行されません

## Phase 3+: 将来の実装
*これらのフェーズは /plan コマンドの範囲外*

**Phase 3**: タスク実行（/tasks コマンドが tasks.md を作成）
**Phase 4**: 実装（憲法原則に従って tasks.md を実行）
**Phase 5**: 検証（テスト実行、quickstart.md 実行、パフォーマンス検証）

## 複雑性追跡
*憲法チェックの違反がある場合のみ記入*

| 違反 | 必要な理由 | よりシンプルな代替案が却下された理由 |
|------|----------|-----------------------------------|
| - | - | - |

## 進捗追跡
*このチェックリストは実行フロー中に更新される*

**フェーズステータス**:
- [x] Phase 0: 研究完了（/plan コマンド）
- [x] Phase 1: 設計完了（/plan コマンド）
- [x] Phase 2: タスク計画完了（/plan コマンド - アプローチの説明のみ）
- [ ] Phase 3: タスク生成（/tasks コマンド）
- [ ] Phase 4: 実装完了
- [ ] Phase 5: 検証合格

**ゲートステータス**:
- [x] 初期憲法チェック: 合格
- [x] 設計後憲法チェック: 合格
- [x] すべての要確認事項解決
- [x] 複雑性の逸脱を文書化（なし）

---
*Constitution v2.1.1 に基づく - `/memory/constitution.md` を参照*