# iOS BLE Beacon 検証ガイド

## 必要な機材
- iOS デバイス 2台（iPhone/iPad）
- iOS 13.0 以上
- Bluetooth LE 対応
- 同一 Apple Developer アカウント

## 事前準備

### 1. Unity プロジェクト設定
```
1. Unity 2022.3 LTS でプロジェクトを開く
2. Build Settings → iOS に切り替え
3. Player Settings で以下を設定：
   - Bundle Identifier: com.yourcompany.onigokko
   - Target minimum iOS Version: 13.0
   - Architecture: ARM64
```

### 2. Xcode プロジェクト生成
```
1. Unity で Build Settings → Build
2. 出力フォルダを選択
3. Xcode プロジェクトが生成されるまで待機
```

### 3. Xcode 設定
```
1. 生成された .xcodeproj を Xcode で開く
2. Signing & Capabilities で：
   - Team を設定
   - Provisioning Profile を確認
   - Capabilities → Background Modes を追加
     ✓ Uses Bluetooth LE accessories
     ✓ Location updates
     ✓ Background processing
```

## テスト手順

### Phase 1: 単体デバイステスト

#### デバイス A (アドバタイザー)
```
1. アプリを起動
2. BLE Beacon画面が表示されることを確認
3. Debug UI で以下を確認：
   - Player ID: 1001
   - Session: 1
   - Advertising: OFF → ON
4. 「アドバタイズ」ボタンをタップ
5. Debug UI で「Adv: true」になることを確認
6. Xcode コンソールで以下ログを確認：
   "[BLE] Started advertising as iBeacon"
```

#### デバイス B (スキャナー)
```
1. アプリを起動
2. Player Settings で Player ID を 2001 に変更
3. Debug UI で以下を確認：
   - Player ID: 2001
   - Session: 1
   - Scanning: OFF → ON
4. 「スキャン」ボタンをタップ
5. Debug UI で「Scan: true」になることを確認
6. Xcode コンソールで以下ログを確認：
   "[BLE] Started scanning for beacons"
```

### Phase 2: 近接検出テスト

#### 距離テスト 1: 至近距離 (0.5m以内)
```
1. デバイス A でアドバタイジング開始
2. デバイス B でスキャン開始
3. 2台のデバイスを50cm以内に近づける
4. デバイス B の Debug UI で以下を確認：
   - 「Player 1001: X.Xm (RSSI: XXX)」表示
   - Distance が 0.3-0.8m 範囲内
   - RSSI が -40 ～ -60 範囲内
5. 5秒間継続して検出されることを確認
```

#### 距離テスト 2: 中距離 (5m)
```
1. デバイス間の距離を約5mに設定
2. デバイス B の Debug UI で：
   - Distance が 4-6m 範囲内
   - RSSI が -60 ～ -80 範囲内
3. 検出精度を記録
```

#### 距離テスト 3: 遠距離 (20m+)
```
1. デバイス間の距離を約20mに設定
2. デバイス B の Debug UI で：
   - Distance が 15-25m 範囲内
   - RSSI が -80 ～ -100 範囲内
3. 検出継続性を確認
```

### Phase 3: 動作検証テスト

#### 移動テスト
```
1. デバイス A を固定位置に置く
2. デバイス B を持って以下の動作：
   - 近づく (20m → 1m)
   - 遠ざかる (1m → 20m)
   - 左右に移動
3. 各位置での距離値を記録
4. 距離変化の応答時間を測定
```

#### 障害物テスト
```
1. デバイス間に障害物を設置：
   - 木製の扉
   - 金属製の壁
   - 人間の体
2. 各条件での検出状況を記録
3. RSSI値の変化を観測
```

### Phase 4: バッテリー・パフォーマンステスト

#### バッテリー消費テスト
```
1. 両デバイスでバッテリー残量を記録
2. 30分間連続動作
3. バッテリー消費量を計算
4. 目標：30分で20%以下の消費
```

#### CPU使用率テスト
```
1. Xcode Instruments でプロファイリング
2. CPU使用率を監視
3. メモリ使用量を記録
4. 目標：CPU 10%以下、メモリ 50MB以下
```

## 検証チェックリスト

### 基本機能
- [ ] アドバタイジング開始/停止
- [ ] スキャン開始/停止
- [ ] ビーコン検出表示
- [ ] 距離計算表示
- [ ] RSSI値表示

### 距離精度
- [ ] 0.5m以内: ±0.3m以内の精度
- [ ] 1-5m: ±1m以内の精度
- [ ] 5-20m: ±3m以内の精度
- [ ] 20m以上: 検出可能

### 安定性
- [ ] 5分間連続検出
- [ ] アプリのバックグラウンド動作
- [ ] デバイス再起動後の復旧
- [ ] 複数ビーコン同時検出

### パフォーマンス
- [ ] バッテリー消費: 20%/30分以下
- [ ] CPU使用率: 10%以下
- [ ] メモリ使用量: 50MB以下
- [ ] 応答時間: 2秒以内

## トラブルシューティング

### 検出できない場合
```
1. 位置情報権限確認
   設定 → プライバシー → 位置情報サービス → アプリ → 常に許可
2. Bluetooth権限確認
   設定 → プライバシー → Bluetooth → アプリ → ON
3. Bluetooth がオンになっているか確認
4. 同一UUIDを使用しているか確認
```

### 距離が不正確な場合
```
1. RSSI値を確認（-40～-100の範囲内か）
2. 周囲の電波干渉を確認
3. デバイスを再起動
4. アプリを再インストール
```

### パフォーマンスが悪い場合
```
1. 他のBluetooth機器を無効化
2. バックグラウンドアプリを終了
3. デバイスの再起動
4. iOS バージョンを確認
```

## 期待される結果

### 成功基準
- 0.5m以内で安定検出（±0.3m精度）
- 20m以内で継続検出可能
- バッテリー消費量が許容範囲内
- アプリのクラッシュなし

### ログ出力例
```
[BLE] BLEBeaconPlugin initialized
[BLE] Started advertising as iBeacon: 550e8400-e29b-41d4-a716-446655440000, Major: 1, Minor: 1001
[BLE] Started scanning for beacons: 550e8400-e29b-41d4-a716-446655440000
[BLE] Detected beacon - Major: 1, Minor: 1001, Distance: 2.3m, RSSI: -65
[BLE] ビーコン検出: Beacon(1.1001): 2.3m, RSSI:-65
```

## 次のステップ

検証完了後：
1. 距離計算アルゴリズムの調整
2. ハートビートシステムとの統合
3. マルチプレイヤー対応
4. ゲームロジックとの結合

---

**注意**: この検証は安全な屋内環境で実施してください。Bluetooth信号は環境により大きく影響を受けます。