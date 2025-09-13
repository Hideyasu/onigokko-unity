# iOS ビルド修正ガイド

## 1. Unity 再ビルド
```
1. 既存のビルドフォルダを削除: /Users/hide/Desktop/ios_build/008m
2. Unity で Build Settings > Build
3. 新しいフォルダ名で保存（例: ios_build_fixed）
```

## 2. Xcode プロジェクト設定

### Signing & Capabilities
```
1. Xcodeでプロジェクトを開く
2. Unity-iPhone ターゲットを選択
3. Signing & Capabilities タブ:
   - Team: あなたのApple Developer Team を選択
   - Bundle Identifier: com.yourcompany.onigokkotest
   - Automatically manage signing: チェック
```

### Build Phases
```
1. Unity-iPhone ターゲット > Build Phases
2. Link Binary With Libraries に以下を追加:
   - CoreLocation.framework
   - CoreBluetooth.framework
   - Foundation.framework
   - UIKit.framework
   - SystemConfiguration.framework
   - Security.framework
```

### Build Settings
```
1. Unity-iPhone ターゲット > Build Settings
2. Framework Search Paths に追加:
   - $(inherited)
   - $(PROJECT_DIR)/Frameworks

3. Other Linker Flags:
   - -ObjC
   - -lc++
```

## 3. 手動フレームワーク追加（自動追加が失敗した場合）

```
1. Project Navigator で Unity-iPhone を選択
2. General タブ > Frameworks, Libraries, and Embedded Content
3. + ボタンをクリックして以下を追加:
   - CoreLocation.framework
   - CoreBluetooth.framework
   - Foundation.framework
   - UIKit.framework
```

## 4. Provisioning Profile エラーの解決

### 方法1: Automatic Signing（推奨）
```
1. Signing & Capabilities で:
   - Automatically manage signing: チェック
   - Team: 正しいDeveloper Teamを選択
2. Bundle Identifier を一意のものに変更
```

### 方法2: Manual Signing
```
1. Apple Developer Portal で Provisioning Profile を作成
2. Xcode で:
   - Automatically manage signing: チェック外す
   - Provisioning Profile: 作成したプロファイルを選択
```

## 5. ビルドエラーの解決順序

### Step 1: Clean Build
```
Product > Clean Build Folder (Cmd+Shift+K)
```

### Step 2: フレームワーク確認
```
Build Phases > Link Binary With Libraries で
必要なフレームワークが全て追加されているか確認
```

### Step 3: 設定確認
```
Build Settings で:
- iOS Deployment Target: 13.0以上
- Valid Architectures: arm64
```

### Step 4: ビルド実行
```
Product > Build (Cmd+B)
```

## トラブルシューティング

### "Undefined symbol" エラー
```
原因: フレームワークがリンクされていない
解決: Build Phases でフレームワークを追加
```

### "Provisioning profile" エラー
```
原因: 署名設定が不正
解決: Team ID と Bundle Identifier を設定
```

### "Code signing" エラー
```
原因: 証明書の問題
解決: Xcode > Preferences > Accounts でサインイン確認
```