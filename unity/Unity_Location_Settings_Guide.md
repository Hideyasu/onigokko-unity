# Unity Location Settings 設定ガイド

## Unity Editor での設定

### 1. Player Settings を開く
```
Edit > Project Settings > Player
```

### 2. iOS Settings タブを選択
```
プラットフォームアイコンから iOS を選択
```

### 3. Configuration セクション
```
Location Usage Description:
ゲーム中の位置追跡とプレイヤー間の距離測定に使用します

Camera Usage Description (もし必要な場合):
ゲーム内でカメラ機能を使用します
```

### 4. Other Settings セクション
```
Bundle Identifier:
com.yourcompany.onigokkotest (一意の識別子)

Target minimum iOS Version:
13.0

Architecture:
ARM64
```

## 設定確認方法

### ビルド前チェックリスト
- [ ] Location Usage Description が設定されている
- [ ] Bundle Identifier が設定されている
- [ ] Target iOS Version が 13.0 以上
- [ ] Architecture が ARM64

### ビルド後確認
Info.plist に以下が追加されているか確認:
- NSLocationUsageDescription
- NSLocationWhenInUseUsageDescription
- NSLocationAlwaysAndWhenInUseUsageDescription

## トラブルシューティング

### "Location Usage Description is empty" エラー
```
原因: Player Settings で Location Usage Description が未設定
解決: 上記の手順で説明文を設定
```

### 設定が反映されない場合
```
1. Unity Editor を再起動
2. Project Settings を再度確認
3. Clean Build を実行
```