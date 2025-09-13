# クイックスタートガイド: 鬼vs陰陽師ゲーム

**プロジェクト**: リアル鬼ごっこゲーム「鬼 vs 陰陽師」
**対象**: 開発者、テスター
**推定時間**: 15-30分

## 前提条件

### 必要なソフトウェア
- Unity 2022.3 LTS
- Xcode 15+ (iOS) または Android Studio (Android)
- Firebase CLI
- Git

### Firebase プロジェクト設定
1. Firebase Console でプロジェクト作成
2. Realtime Database を有効化
3. Authentication を設定（匿名認証）
4. iOS/Android アプリを登録
5. `google-services.json` / `GoogleService-Info.plist` をダウンロード

## セットアップ手順

### 1. リポジトリクローン
```bash
git clone <repository-url>
cd onigokko-unity
git checkout 001-unity-dead-by
```

### 2. Unity プロジェクト設定
```bash
# Unity で プロジェクトを開く
# Package Manager から以下をインストール:
# - Firebase SDK for Unity
# - Unity Test Framework
# - Location Services (内蔵)
```

### 3. Firebase 設定ファイル配置
```bash
# Assets/ フォルダに配置
cp path/to/google-services.json Assets/
cp path/to/GoogleService-Info.plist Assets/
```

### 4. 位置情報権限設定

#### iOS (Info.plist)
```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>ゲーム中の位置追跡に使用します</string>
<key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
<string>リアル鬼ごっこゲームに必要です</string>
```

#### Android (AndroidManifest.xml)
```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />
```

## 実行手順

### 1. 開発環境での実行

#### Unity Editor
```bash
# 1. Unity でプロジェクトを開く
# 2. Scene: Assets/Scenes/MainMenu.unity を開く
# 3. Play ボタンをクリック
# 4. GPS シミュレーション設定:
#    - Window > Location Services > Enable Location Services
#    - 任意の座標を設定（例: 東京駅周辺）
```

#### 実機テスト（推奨）
```bash
# iOS
# 1. Build Settings > iOS に切り替え
# 2. Player Settings で Bundle ID, Team 設定
# 3. Build and Run

# Android
# 1. Build Settings > Android に切り替え
# 2. Player Settings で Package Name 設定
# 3. Build and Run
```

### 2. ゲーム開始フロー

#### ステップ 1: ロビー作成
```
1. アプリ起動
2. 「ホストとしてゲーム作成」をタップ
3. プレイエリア設定（半径500m）
4. ゲームルーム作成完了
```

#### ステップ 2: プレイヤー参加
```
1. 他のプレイヤーがアプリ起動
2. 「ゲームに参加」をタップ
3. ルームコード入力（6桁）
4. 自動役割割り当て（鬼 or 陰陽師）
```

#### ステップ 3: ゲーム開始
```
1. ホストが「ゲーム開始」をタップ
2. 5秒カウントダウン
3. ゲーム開始！
```

## 基本的な操作方法

### 鬼（Oni）の操作
```
目標: 全ての陰陽師を捕まえる

1. マップで陰陽師の位置を確認
2. 陰陽師に近づく（5m以内）
3. 「攻撃」ボタンをタップ
4. 魂を吸収（1回で陰陽師除外）
```

### 陰陽師（Onmyoji）の操作
```
目標: 5つの儀式を完了して鬼を封印

1. マップで儀式ポイントを確認
2. 儀式ポイントに移動（3m以内）
3. 儀式を実行（10秒間その場にいる）
4. トラップ設置（鬼を30秒無力化）
```

### 心音システム
```
陰陽師のみに作用:
- 50m以内: 低い心音（ドクドク）
- 30m以内: 中程度の心音 + バイブレーション
- 10m以内: 激しい心音 + 画面警告
```

## テストシナリオ

### テストケース 1: 基本ゲームフロー
```
前提: 5人のプレイヤー（鬼1、陰陽師4）
手順:
1. ロビー作成・参加
2. ゲーム開始
3. 役割確認
4. 位置情報共有確認
5. 心音システム動作確認（距離別）
6. 攻撃実行テスト
7. 儀式実行テスト
8. 勝利条件確認

期待結果: エラーなく完了
```

### テストケース 2: 接続復旧テスト
```
前提: ゲーム進行中
手順:
1. 1人のプレイヤーがアプリを終了
2. 30秒後に再起動
3. 自動再接続確認
4. ゲーム状態同期確認

期待結果: ゲーム状態が正しく復元される
```

### テストケース 3: 境界テスト
```
前提: ゲーム進行中
手順:
1. プレイエリア境界（500m）に移動
2. 境界警告表示確認
3. 境界外に出る
4. ペナルティ/警告確認

期待結果: 適切な境界制御
```

## トラブルシューティング

### よくある問題

#### 位置情報が取得できない
```
原因: 権限が許可されていない
解決策:
1. 設定 > プライバシー > 位置情報サービスを確認
2. アプリ個別の位置情報権限を確認
3. 「常に許可」に設定
```

#### Firebase 接続エラー
```
原因: 設定ファイルが正しく配置されていない
解決策:
1. google-services.json の配置確認
2. Bundle ID / Package Name の一致確認
3. Firebase Console でアプリ登録確認
```

#### 心音が聞こえない
```
原因: 音量設定 or バイブレーション設定
解決策:
1. デバイスの音量確認
2. アプリ内音量設定確認
3. バイブレーション権限確認
```

#### 他のプレイヤーが見えない
```
原因: リアルタイム同期の問題
解決策:
1. ネットワーク接続確認
2. Firebase Console でデータ確認
3. アプリ再起動
```

## パフォーマンス確認

### 監視項目
```
1. フレームレート: 30fps 以上を維持
2. バッテリー消費: 30分で 20% 以下
3. ネットワーク使用量: 1MB/分 以下
4. メモリ使用量: 150MB 以下
```

### ログ確認
```bash
# Unity Console でのログ確認
- [Location] GPS精度: X.Xm
- [Firebase] 同期遅延: XXXms
- [Battery] 消費率: XX%/分
- [Network] データ使用量: XX KB/分
```

## 次のステップ

このクイックスタートが完了したら：

1. **詳細テスト**: より複雑なシナリオでテスト
2. **パフォーマンス調整**: バッテリー最適化の実装
3. **UI/UX 改善**: ユーザビリティテストの実施
4. **本格運用**: プロダクション環境での動作確認

## サポート

問題が発生した場合:
1. ログファイルを確認
2. 開発チームに連絡
3. GitHub Issues に報告

**重要**: このゲームは位置情報を使用するため、屋外での安全な環境でテストを実施してください。