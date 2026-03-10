# フルテーブルスキャンデモ

## 機能概要

インデックスなし（フルスキャン）とインデックスあり検索を比較できるデモ機能。
実行時間、スキャン方式を可視化し、インデックスの重要性をエンジニア教育に活用。

---

## ステータス

- **実装状況**: 🚧 実装中
- **優先度**: 高（教育用デモ）
- **担当者**: -
- **実装完了日**: -

---

## ドキュメント一覧

| ドキュメント | 概要 | ステータス |
|------------|------|----------|
| [要件定義書](requirements.md) | 機能要件、非機能要件 | ✅ |
| [外部設計書](external-design.md) | 画面、API、DB論理設計 | ✅ |
| [内部設計書](internal-design.md) | クラス、シーケンス、DB物理設計 | ✅ |
| [テストケース](test-cases.md) | 単体、統合、E2Eテスト | ✅ |

---

## 実装ファイル

### コード
- `Features/Demo/DemoController.cs` - Controller（エンドポイント追加）
- `Features/Demo/Services/FullScanService.cs` - Service
- `Features/Demo/Services/IFullScanService.cs` - Interface
- `Features/Demo/DTOs/FullScanResponse.cs` - DTO

### データベース
- `Data/full_scan_demo.db` - SQLite データベース
- `LargeUsers` テーブル（100万件）

### テスト
- `Tests/Features/Demo/FullScanServiceTests.cs` - 単体テスト

---

## デモ結果例

### インデックスなし（フルスキャン）
```json
{
  "executionTimeMs": 3000,
  "rowCount": 1,
  "hasIndex": false,
  "message": "インデックスなし: 100万件を全件スキャンしました"
}
```

### インデックスあり
```json
{
  "executionTimeMs": 5,
  "rowCount": 1,
  "hasIndex": true,
  "message": "インデックスあり: インデックスを使って高速に取得しました"
}
```

### 改善効果
- **実行時間**: 3000ms → 5ms（約99.8%削減）
- **スキャン方式**: フルスキャン → インデックス使用

---

## 教育目的

### 学習ポイント
1. **フルテーブルスキャンとは**: インデックスなしで全件走査する性能問題
2. **影響度**: データ件数が増えると線形に悪化
3. **解決策**: 検索条件カラムにインデックスを作成
4. **実測値**: 実際の実行時間を可視化

### 対象者
- **新人エンジニア**: インデックスの基礎とパフォーマンス
- **中堅エンジニア**: 大規模データでの性能チューニング
- **教育担当者**: デモを使って口頭説明の負担軽減

---

## 関連機能

- [N+1問題デモ](../n-plus-one-demo/) - 実装済み
- [セキュリティデモ](../security-demo/) - 未実装

---

## 参考リンク

- [ADR-001: SQLiteを教育用デモに採用](../../adr/001-use-sqlite-for-education.md)
- [ADR-002: ORMを使わず素のSQLを採用](../../adr/002-avoid-orm-use-raw-sql.md)
- [全体設計書](../../)
