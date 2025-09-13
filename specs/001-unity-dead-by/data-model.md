# データモデル設計: 鬼vs陰陽師ゲーム

**プロジェクト**: リアル鬼ごっこゲーム「鬼 vs 陰陽師」
**日付**: 2025-09-13
**Firebase Realtime Database 構造**

## エンティティ概要

本ゲームのデータモデルは Firebase Realtime Database を使用し、リアルタイム同期を重視した設計となっています。24時間ハッカソン制約下での実装を考慮し、シンプルかつ効率的な構造を採用しました。

## 1. Player（プレイヤー）

```json
{
  "players": {
    "player_001": {
      "id": "player_001",
      "name": "田中太郎",
      "role": "oni",  // "oni" | "onmyoji"
      "status": "alive",  // "alive" | "dead" | "spectating"
      "health": 3,  // 鬼: 無限, 陰陽師: 3
      "location": {
        "latitude": 35.6762,
        "longitude": 139.6503,
        "accuracy": 5.0,
        "timestamp": 1694598000000
      },
      "session_id": "session_001",
      "last_active": 1694598000000,
      "device_info": {
        "platform": "iOS",
        "version": "17.0"
      }
    }
  }
}
```

### フィールド詳細
- **id**: プレイヤーの一意識別子
- **name**: 表示名
- **role**: ゲーム内役割（鬼または陰陽師）
- **status**: プレイヤー状態（生存/死亡/観戦）
- **health**: 体力値（鬼は無限、陰陽師は3）
- **location**: GPS位置情報と精度・タイムスタンプ
- **session_id**: 参加しているゲームセッションID
- **last_active**: 最終アクティブ時刻
- **device_info**: プラットフォーム情報（デバッグ用）

## 2. GameSession（ゲームセッション）

```json
{
  "sessions": {
    "session_001": {
      "id": "session_001",
      "host_id": "player_001",
      "state": "playing",  // "waiting" | "starting" | "playing" | "ended"
      "created_at": 1694598000000,
      "started_at": 1694598120000,
      "duration_minutes": 30,
      "players": {
        "player_001": {
          "role": "oni",
          "joined_at": 1694598000000
        },
        "player_002": {
          "role": "onmyoji",
          "joined_at": 1694598015000
        }
      },
      "game_area": {
        "center_lat": 35.6762,
        "center_lon": 139.6503,
        "radius_meters": 500
      },
      "ritual_progress": {
        "completed": 2,
        "total": 5,
        "rituals": {
          "ritual_001": {
            "completed": true,
            "completed_by": "player_002",
            "completed_at": 1694598300000
          }
        }
      },
      "winner": null,  // "oni" | "onmyoji" | null
      "ended_at": null
    }
  }
}
```

### フィールド詳細
- **id**: セッションの一意識別子
- **host_id**: ホストプレイヤーのID
- **state**: ゲーム状態（待機/開始中/進行中/終了）
- **players**: 参加プレイヤーリストと役割
- **game_area**: プレイエリアの中心座標と半径
- **ritual_progress**: 儀式の進行状況
- **winner**: 勝利陣営

## 3. Trap（トラップ）

```json
{
  "traps": {
    "trap_001": {
      "id": "trap_001",
      "owner_id": "player_002",
      "session_id": "session_001",
      "location": {
        "latitude": 35.6765,
        "longitude": 139.6500,
        "accuracy": 3.0
      },
      "radius_meters": 5,
      "duration_seconds": 30,
      "created_at": 1694598200000,
      "expires_at": 1694598230000,
      "is_triggered": false,
      "triggered_by": null,
      "triggered_at": null
    }
  }
}
```

### フィールド詳細
- **owner_id**: トラップを設置した陰陽師のID
- **location**: 設置位置
- **radius_meters**: 発動半径（5メートル）
- **duration_seconds**: 効果持続時間
- **is_triggered**: 発動状態
- **triggered_by**: 発動させたプレイヤーID

## 4. Ritual（儀式）

```json
{
  "rituals": {
    "ritual_001": {
      "id": "ritual_001",
      "session_id": "session_001",
      "location": {
        "latitude": 35.6770,
        "longitude": 139.6510
      },
      "radius_meters": 3,
      "required_players": 1,
      "progress": {
        "current_players": [],
        "completion_percentage": 0,
        "started_at": null,
        "estimated_completion": null
      },
      "is_completed": false,
      "completed_by": [],
      "completed_at": null
    }
  }
}
```

### フィールド詳細
- **location**: 儀式実行位置
- **required_players**: 必要プレイヤー数
- **progress**: 現在の進行状況
- **completed_by**: 完了に貢献したプレイヤーリスト

## 5. Footprint（足跡）

```json
{
  "footprints": {
    "footprint_001": {
      "id": "footprint_001",
      "player_id": "player_002",
      "session_id": "session_001",
      "path": [
        {
          "latitude": 35.6762,
          "longitude": 139.6503,
          "timestamp": 1694598180000
        },
        {
          "latitude": 35.6763,
          "longitude": 139.6504,
          "timestamp": 1694598185000
        }
      ],
      "created_at": 1694598180000,
      "expires_at": 1694598210000,
      "is_visible": true
    }
  }
}
```

### フィールド詳細
- **path**: 移動経路の座標配列
- **expires_at**: 足跡の消失時刻（30秒後）
- **is_visible**: 鬼に対する表示状態

## データベース最適化

### インデックス戦略
```javascript
// Firebase Realtime Database Rules
{
  "rules": {
    "sessions": {
      ".indexOn": ["state", "host_id"]
    },
    "players": {
      ".indexOn": ["session_id", "status"]
    },
    "traps": {
      ".indexOn": ["session_id", "expires_at"]
    },
    "rituals": {
      ".indexOn": ["session_id", "is_completed"]
    }
  }
}
```

### リアルタイム更新戦略
1. **高頻度更新**: プレイヤー位置（1-5秒間隔）
2. **中頻度更新**: ゲーム状態、トラップ状態（10秒間隔）
3. **低頻度更新**: 儀式進行、セッション情報（30秒間隔）

## バリデーションルール

### 位置情報検証
- GPS精度 5-10m 以内を有効とする
- 前回位置から の移動距離上限: 100m/秒（人間の移動速度制限）
- プレイエリア境界チェック

### ゲームロジック検証
- 鬼の攻撃範囲: 5m以内
- トラップ発動範囲: 5m以内
- 儀式実行範囲: 3m以内
- 同時接続数制限: 20セッション（ハッカソンデモ用）

## 状態遷移

### ゲームセッション状態遷移
```
waiting → starting → playing → ended
```

### プレイヤー状態遷移
```
alive → dead → spectating
```

### トラップ状態遷移
```
created → triggered → expired
```

この設計により、24時間ハッカソン制約下でも実装可能な、効率的かつ拡張性のあるデータ構造を提供します。