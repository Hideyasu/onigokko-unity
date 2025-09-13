# Xcode ビルド設定修正ガイド

## @import エラーの修正

### 方法1: ヘッダーファイル修正（推奨）
```
BLEBeaconPlugin.h で @import を #import に変更
✓ 完了済み
```

### 方法2: Xcode Build Settings 修正（必要に応じて）

#### Unity-iPhone ターゲット
```
Build Settings で以下を設定:

Enable Modules (C and Objective-C): YES
Enable C++ Modules: YES (必要に応じて)

または

CLANG_ENABLE_MODULES: YES
CLANG_ENABLE_CXX_MODULES: YES
```

#### 設定手順
```
1. Unity-iPhone ターゲットを選択
2. Build Settings タブをクリック
3. "Enable Modules" を検索
4. "Enable Modules (C and Objective-C)" を YES に設定
```

## トラブルシューティング

### コンパイルエラーが続く場合
```
1. Product > Clean Build Folder (⌘+Shift+K)
2. Derived Data を削除:
   Xcode > Preferences > Locations > Derived Data > 矢印をクリック
   該当プロジェクトフォルダを削除
3. Xcode を再起動
4. 再ビルド
```

### フレームワークリンクエラー
```
Build Phases > Link Binary With Libraries で確認:
- CoreLocation.framework ✓
- CoreBluetooth.framework ✓
- Foundation.framework ✓
- UIKit.framework ✓
```

### 署名エラー
```
Signing & Capabilities で確認:
- Team が正しく設定されている
- Bundle Identifier が一意
- Automatically manage signing がチェック済み
```