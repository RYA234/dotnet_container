# 機能別設計書

## 概要

このディレクトリには、機能ごとの設計書が含まれています。
各機能は独立したフォルダで管理され、以下の構成になっています。

---

## フォルダ構成

```
features/
├── README.md (このファイル)
├── template/ (テンプレート)
│   ├── README.md
│   ├── requirements.md
│   ├── external-design.md
│   ├── internal-design.md
│   └── test-cases.md
│
├── n-plus-one-demo/ (N+1問題デモ) ✅ 実装済み
│   ├── README.md
│   ├── requirements.md
│   ├── external-design.md
│   ├── internal-design.md
│   └── test-cases.md
│
├── error-handling-demo/ (エラーハンドリングデモ) 🚧 未実装
├── security-demo/ (セキュリティデモ) 🚧 未実装
├── calculator/ (電卓) ✅ 実装済み
├── inventory/ (在庫管理) 🚧 未実装
├── sales/ (販売管理) 🚧 未実装
└── production/ (生産管理) 🚧 未実装
```

---

## 機能一覧

### エンジニア教育用デモ

| No | 機能名 | フォルダ | ステータス | 優先度 |
|----|--------|---------|----------|--------|
| E-01 | N+1問題デモ | [n-plus-one-demo/](n-plus-one-demo/) | ✅ 実装済み | 1 |
| E-02 | エラーハンドリングデモ | [error-handling-demo/](error-handling-demo/) | 🚧 未実装 | 2 |
| E-03 | セキュリティデモ | [security-demo/](security-demo/) | 🚧 未実装 | 3 |
| E-04 | データ構造デモ | [data-structure-demo/](data-structure-demo/) | 🚧 未実装 | 4 |

### 基幹システム

| No | 機能名 | フォルダ | ステータス | 優先度 |
|----|--------|---------|----------|--------|
| B-01 | 在庫管理 | [inventory/](inventory/) | 🚧 未実装 | 5 |
| B-02 | 販売管理 | [sales/](sales/) | 🚧 未実装 | 6 |
| B-03 | 生産管理 | [production/](production/) | 🚧 未実装 | 7 |

### WinFormsマイグレーション

| No | 機能名 | フォルダ | ステータス | 優先度 |
|----|--------|---------|----------|--------|
| W-01 | 電卓 | [calculator/](calculator/) | ✅ 実装済み | - |

---

## 新機能追加手順

### 1. テンプレートをコピー
```bash
cp -r template/ new-feature/
```

### 2. ドキュメント作成
- `README.md`: 機能概要
- `requirements.md`: 要件定義
- `external-design.md`: 外部設計（画面、API、DB論理）
- `internal-design.md`: 内部設計（クラス、シーケンス、DB物理）
- `test-cases.md`: テストケース

### 3. 実装
- `Features/NewFeature/` フォルダに実装
- 設計書に沿ってコーディング

### 4. ドキュメント更新
- このREADME.mdに機能を追加
- 各設計書を実装に合わせて更新

---

## 参考

- [テンプレート](template/) - 新機能作成時のテンプレート
- [N+1問題デモ](n-plus-one-demo/) - 実装済みの参考例
- [全体設計書](../) - システム全体の設計書
