# N+1問題デモ

## 機能概要

N+1問題の悪い例（Bad版）と良い例（Good版）を比較できるデモ機能。
実行時間、SQL実行回数、データサイズを可視化し、エンジニア教育に活用。

---

## ステータス

- **実装状況**: ✅ 実装済み
- **優先度**: 高（教育用デモの中で最優先）
- **担当者**: -
- **実装完了日**: 2025-12-10

---

## ドキュメント一覧

| ドキュメント | 概要 | ステータス |
|------------|------|----------|
| [要件定義書](requirements.md) | 機能要件、非機能要件 | ✅ |
| [外部設計書](external-design.md) | 画面、API、DB論理設計 | ✅ |
| [内部設計書](internal-design.md) | クラス、シーケンス、DB物理設計 | ✅ |
| [テストケース](test-cases.md) | 単体、統合、E2Eテスト | ✅ |
| [コードサンプル](code-example.cs) | XMLコメント付きのサンプルコード | ✅ ⭐ |

---

## 実装ファイル

### コード
- `Features/Demo/DemoController.cs` - Controller
- `Features/Demo/Services/NPlusOneService.cs` - Service
- `Features/Demo/Models/NPlusOneResponse.cs` - DTO

### ビュー
- `Views/Demo/Performance.cshtml` - メイン画面

### データベース
- `demo.db` - SQLite データベース
- `Users` テーブル（100件）
- `Departments` テーブル（5件）

### テスト
- `Tests/NPlusOneServiceTests.cs` - 単体テスト（未実装）

---

## デモ結果例

### Bad版（N+1問題あり）
```json
{
  "executionTimeMs": 45,
  "sqlCount": 101,
  "dataSize": 5621,
  "rowCount": 100,
  "message": "N+1問題あり: ループ内で部署情報を100回個別に取得しています（合計101クエリ）"
}
```

### Good版（最適化済み）
```json
{
  "executionTimeMs": 12,
  "sqlCount": 1,
  "dataSize": 5621,
  "rowCount": 100,
  "message": "最適化済み: 1回のJOINクエリで全データを取得しています"
}
```

### 改善効果
- **実行時間**: 45ms → 12ms（約75%削減）
- **SQL実行回数**: 101回 → 1回（約99%削減）
- **データサイズ**: 同一

---

## 教育目的

### 学習ポイント
1. **N+1問題とは**: ループ内でSQLを実行すると発生する性能問題
2. **影響度**: クエリ回数が100倍になると実行時間も大幅増加
3. **解決策**: JOINを使って1回のクエリで取得
4. **実測値**: 実際の実行時間とクエリ回数を可視化

### 対象者
- **新人エンジニア**: SQLの基礎とパフォーマンス
- **中堅エンジニア**: 性能チューニングの重要性
- **教育担当者**: デモを使って口頭説明の負担軽減

---

## 関連機能

- [エラーハンドリングデモ](../error-handling-demo/) - 次に実装予定
- [セキュリティデモ](../security-demo/) - 未実装

---

## 参考リンク

- [ADR-001: SQLiteを教育用デモに採用](../../adr/001-use-sqlite-for-education.md)
- [ADR-002: ORMを使わず素のSQLを採用](../../adr/002-avoid-orm-use-raw-sql.md)
- [全体設計書](../../)
