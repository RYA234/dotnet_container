# 画面一覧

## 画面一覧表

| No | 画面ID | 画面名 | パス | ステータス |
|----|--------|--------|------|----------|
| 01 | HOME | ホーム画面 | /dotnet/Home/Index | ✅ |
| 02 | DEMO_PERF | SQLパフォーマンス | /dotnet/Demo/Performance | ✅ |
| 03 | DEMO_ERROR | エラーハンドリング | /dotnet/Demo/ErrorHandling | 🚧 |
| 04 | DEMO_SEC | セキュリティ | /dotnet/Demo/Security | 🚧 |
| 05 | DEMO_DATA | データ構造 | /dotnet/Demo/DataStructures | 🚧 |
| 06 | INV | 在庫管理 | /dotnet/Inventory/Index | 🚧 |
| 07 | SALES | 販売管理 | /dotnet/Sales/Index | 🚧 |
| 08 | PROD | 生産管理 | /dotnet/Production/Index | 🚧 |
| 09 | CALC | 電卓 | /dotnet/Calculator/Index | ✅ |

---

## 画面分類

### 🎓 エンジニア教育用
| 画面ID | 画面名 | ステータス |
|--------|--------|----------|
| DEMO_PERF | SQLパフォーマンス | ✅ 実装済み |
| DEMO_ERROR | エラーハンドリング | 🚧 未実装 |
| DEMO_SEC | セキュリティ | 🚧 未実装 |
| DEMO_DATA | データ構造 | 🚧 未実装 |

### 🏢 基幹システム
| 画面ID | 画面名 | ステータス |
|--------|--------|----------|
| INV | 在庫管理 | 🚧 未実装 |
| SALES | 販売管理 | 🚧 未実装 |
| PROD | 生産管理 | 🚧 未実装 |

### 🖥️ WinFormsマイグレーション
| 画面ID | 画面名 | ステータス |
|--------|--------|----------|
| CALC | 電卓 | ✅ 実装済み |

---

## 共通レイアウト

### レイアウトファイル
- **ファイル**: `Views/Shared/_Layout.cshtml`

### 構成
```
┌─────────────────────────────────┐
│ ヘッダー                         │
│ ┌─────────────────────────────┐ │
│ │ グローバルナビゲーション       │ │
│ │ Home | Calculator | Orders  │ │
│ │ Demo                         │ │
│ └─────────────────────────────┘ │
├─────────────────────────────────┤
│                                 │
│ コンテンツエリア                 │
│ （各画面の内容）                 │
│                                 │
├─────────────────────────────────┤
│ フッター                         │
│ © 2025 ASP.NET Core Demo        │
└─────────────────────────────────┘
```

---

## 画面遷移

詳細は [画面遷移図](../screen-transition.md) を参照してください。
