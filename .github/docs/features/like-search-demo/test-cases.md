# LIKE検索デモ - テストケース一覧

## 文書情報
- **作成日**: 2026-03-12
- **最終更新**: 2026-03-12
- **バージョン**: 1.0

---

## 1. ユニットテスト（LikeSearchServiceTests）

### TC-01: セットアップ系

| No | テストケース | 前提条件 | 操作 | 期待結果 |
|----|------------|---------|------|---------|
| TC-01-01 | テーブル作成とデータ投入 | DBが空 | SetupAsync() | Success=true, RowCount=1000（テスト用） |
| TC-01-02 | 既存データがあればスキップ | データあり | SetupAsync() を2回 | 2回目も Success=true |
| TC-01-03 | 実行時間が記録される | DBが空 | SetupAsync() | ExecutionTimeMs > 0 |

---

### TC-02: 前方一致検索

| No | テストケース | 前提条件 | 操作 | 期待結果 |
|----|------------|---------|------|---------|
| TC-02-01 | 前方一致で検索できる | データあり | SearchPrefixAsync("山") | Name が "山" で始まるデータが返る |
| TC-02-02 | usesIndex = true | データあり | SearchPrefixAsync("山") | UsesIndex = true |
| TC-02-03 | キーワードパターンが正しい | - | SearchPrefixAsync("山") | Keyword = "山%" |
| TC-02-04 | searchType が正しい | - | SearchPrefixAsync("山") | SearchType = "prefix" |
| TC-02-05 | 実行時間が記録される | データあり | SearchPrefixAsync("山") | ExecutionTimeMs >= 0 |
| TC-02-06 | 件数が正しい | データあり | SearchPrefixAsync("山") | RowCount = Data.Count |
| TC-02-07 | キーワードなしで空結果 | データあり | SearchPrefixAsync("Z") | RowCount = 0 |

---

### TC-03: 中間一致検索

| No | テストケース | 前提条件 | 操作 | 期待結果 |
|----|------------|---------|------|---------|
| TC-03-01 | 中間一致で検索できる | データあり | SearchPartialAsync("山") | Name に "山" が含まれるデータが返る |
| TC-03-02 | usesIndex = false | データあり | SearchPartialAsync("山") | UsesIndex = false |
| TC-03-03 | キーワードパターンが正しい | - | SearchPartialAsync("山") | Keyword = "%山%" |
| TC-03-04 | searchType が正しい | - | SearchPartialAsync("山") | SearchType = "partial" |
| TC-03-05 | 実行時間が記録される | データあり | SearchPartialAsync("山") | ExecutionTimeMs >= 0 |
| TC-03-06 | 件数が正しい | データあり | SearchPartialAsync("山") | RowCount = Data.Count |

---

### TC-04: 前方一致 vs 中間一致の比較

| No | テストケース | 前提条件 | 操作 | 期待結果 |
|----|------------|---------|------|---------|
| TC-04-01 | 中間一致の方が件数が多い（または同等） | データあり | 両方実行して比較 | partial.RowCount >= prefix.RowCount |
| TC-04-02 | 前方一致は中間一致に含まれる | データあり | 両方実行して比較 | prefix の全結果が partial に含まれる |

---

## 2. 手動テスト（ローカル確認）

| No | テストケース | 手順 | 期待結果 |
|----|------------|------|---------|
| MT-01 | ページアクセス | /dotnet/Demo/LikeSearch にアクセス | 画面が正常表示される |
| MT-02 | セットアップ実行 | Step1「セットアップ実行」ボタン押下 | 10万件生成完了メッセージ |
| MT-03 | 前方一致検索 | キーワード「山」で前方一致実行 | 速い結果・インデックス使用表示 |
| MT-04 | 中間一致検索 | キーワード「山」で中間一致実行 | 遅い結果・フルスキャン表示 |
| MT-05 | 比較表示 | 両方実行後 | 実行時間・件数・インデックス使用状況が比較できる |

---

## 3. CI テスト

| No | 確認項目 | 期待結果 |
|----|---------|---------|
| CI-01 | dotnet test | 全テスト PASS |
| CI-02 | dotnet build | ビルド成功 |
