# Claude Code 設定: 鬼vs陰陽師ゲーム

**プロジェクト**: リアル鬼ごっこゲーム「鬼 vs 陰陽師」
**更新日**: 2025-09-13
**Unity Version**: 2022.3 LTS

## プロジェクト概要

Dead by Daylight風のリアルタイム鬼ごっこゲームをUnityで開発。鬼（1人）vs 陰陽師（4人）の非対称対戦。24時間ハッカソンで実装可能なMVP構成。

### コア機能
- 心音判定システム（距離: 50m/30m/10m の3段階）
- リアルタイム位置共有（Firebase Realtime Database）
- 攻撃/トラップシステム
- 儀式完了による勝利条件

## 技術スタック

### 開発環境
```yaml
Unity: 2022.3 LTS
Language: C# (.NET Standard 2.1)
IDE: Unity Editor + VS Code/Rider
Platform: iOS 14+ / Android API 26+
```

### 依存関係
```yaml
Primary:
  - Firebase SDK for Unity (Realtime Database, Auth, Analytics)
  - Unity Location Services (内蔵)
  - Unity Mobile Notifications

Testing:
  - Unity Test Framework
  - NUnit
  - Firebase Emulator Suite
```

### アーキテクチャ
```yaml
Pattern: MonoBehaviour + ScriptableObject
Packages: 4つのカスタムパッケージ
  - com.onigokko.location
  - com.onigokko.heartbeat
  - com.onigokko.session
  - com.onigokko.combat
```

## 実装優先順位（ハッカソン向け）

### Phase 1: 基盤システム（優先度: 最高）
```csharp
// 1. GameManager - ゲーム全体制御
// 2. SessionManager - Firebase接続とセッション管理
// 3. LocationTracker - GPS位置追跡
// 4. PlayerController - プレイヤー基本制御
```

### Phase 2: コア機能（優先度: 高）
```csharp
// 5. HeartbeatSystem - 距離計算と心音再生（MVP核心機能）
// 6. ProximityDetector - 近接検出とイベント発火
// 7. AttackSystem - 鬼の攻撃処理
// 8. UI システム（LobbyUI, GameHUD）
```

### Phase 3: 追加機能（優先度: 中）
```csharp
// 9. TrapSystem - 陰陽師のトラップ設置
// 10. RitualSystem - 儀式実行システム
// 11. FootprintSystem - 足跡表示
// 12. NotificationSystem - バックグラウンド通知
```

## ファイル構造

### スクリプト配置
```
Assets/Scripts/
├── Core/
│   ├── GameManager.cs       # メインゲームループ
│   ├── SessionManager.cs    # Firebase セッション管理
│   └── PlayerController.cs  # プレイヤー入力処理
├── Location/
│   ├── LocationTracker.cs   # GPS追跡メイン
│   └── GPSService.cs        # プラットフォーム固有GPS処理
├── Heartbeat/
│   ├── HeartbeatSystem.cs   # 心音判定とオーディオ
│   └── ProximityDetector.cs # 距離計算
├── Combat/
│   ├── AttackSystem.cs      # 攻撃判定
│   └── TrapSystem.cs        # トラップ処理
└── UI/
    ├── LobbyUI.cs           # ロビー画面
    └── GameHUD.cs           # ゲーム中UI
```

### 設定ファイル
```
Assets/
├── google-services.json      # Android Firebase設定
├── GoogleService-Info.plist  # iOS Firebase設定
├── StreamingAssets/
│   └── firebase-config.json  # Firebase設定
└── Resources/
    ├── GameSettings.asset    # ゲーム設定（ScriptableObject）
    └── Sounds/Heartbeat/     # 心音オーディオファイル
```

## テスト戦略

### テスト実行コマンド
```bash
# Unity Test Runner使用
# Window > General > Test Runner

# EditModeテスト
# PlayModeテスト（実機推奨）

# Firebase Emulator（開発環境）
firebase emulators:start --only database
```

### 重要テストケース
```csharp
// 1. LocationTrackerTest - GPS精度と更新頻度
// 2. HeartbeatSystemTest - 距離計算の正確性
// 3. SessionManagerTest - Firebase同期とエラーハンドリング
// 4. GameFlowTest - 完全なゲームフロー統合テスト
```

## 設定値（調整可能）

### ゲームバランス
```csharp
public static class GameConstants
{
    // 心音システム
    public const float HEARTBEAT_RANGE_FAR = 50f;    // 遠距離心音
    public const float HEARTBEAT_RANGE_MID = 30f;    // 中距離心音
    public const float HEARTBEAT_RANGE_NEAR = 10f;   // 近距離心音

    // 戦闘システム
    public const float ATTACK_RANGE = 5f;            // 攻撃範囲
    public const float TRAP_RANGE = 5f;              // トラップ発動範囲
    public const float RITUAL_RANGE = 3f;            // 儀式実行範囲

    // ゲーム設定
    public const int TOTAL_RITUALS = 5;              // 必要儀式数
    public const int TRAP_DURATION = 30;             // トラップ持続時間（秒）
    public const int FOOTPRINT_DURATION = 30;        // 足跡表示時間（秒）

    // パフォーマンス
    public const float GPS_UPDATE_INTERVAL = 2f;     // GPS更新間隔（秒）
    public const int TARGET_FRAMERATE = 30;          // 目標FPS
}
```

### Firebase設定
```json
{
  "rules": {
    "sessions": {
      ".write": "auth != null",
      ".indexOn": ["state", "host_id"]
    },
    "players": {
      ".write": "auth != null",
      ".indexOn": ["session_id", "status"]
    }
  }
}
```

## デバッグとログ

### ログレベル
```csharp
public enum LogLevel { Debug, Info, Warning, Error }

// 使用例
Logger.Log(LogLevel.Info, "GPS accuracy: " + gpsAccuracy + "m");
Logger.Log(LogLevel.Warning, "Firebase sync delay: " + delay + "ms");
```

### デバッグUI表示項目
```
- FPS表示
- GPS精度
- Firebase接続状態
- バッテリー残量
- 現在位置（緯度経度）
- 最寄りプレイヤーとの距離
```

## パフォーマンス目標

### 測定指標
```yaml
FPS: 30fps以上維持
Battery: 30分で20%以下の消費
Memory: 150MB以下
Network: 1MB/分以下のデータ使用量
GPS_Accuracy: 5-10m以内
Sync_Delay: 500ms以下
```

### 最適化ポイント
```csharp
// 1. GPU最適化: 30fpsに制限
Application.targetFrameRate = 30;

// 2. 位置更新頻度調整
LocationTracker.UpdateInterval = 2f; // 2秒間隔

// 3. Firebase同期最適化
// - 必要なデータのみリッスン
// - バッチ更新の活用
```

## トラブルシューティング

### よくある問題と解決策
```yaml
位置情報取得エラー:
  - iOS: Info.plist の権限設定確認
  - Android: AndroidManifest.xml の権限確認
  - 実機テスト必須

Firebase接続エラー:
  - 設定ファイル配置確認
  - Bundle ID / Package Name一致確認
  - ネットワーク接続確認

心音が聞こえない:
  - AudioSource設定確認
  - デバイス音量確認
  - バイブレーション権限確認
```

## 最近の変更履歴

### v0.1.0 (2025-09-13)
- 初期プロジェクト設定
- 基本的なFirebase統合
- 心音システム実装
- GPS位置追跡機能

---

**注意**: このプロジェクトは24時間ハッカソン用のMVPです。本格運用には追加の最適化とテストが必要です。