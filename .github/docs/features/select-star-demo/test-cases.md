# SELECT * 無駄遣いデモ - テストケース一覧

## 文書情報
- **作成日**: 2026-03-12
- **最終更新**: 2026-03-12
- **バージョン**: 1.0

---

## 1. ユニットテスト（SelectStarServiceTests）

### TC-01: セットアップ系

| No | テストケース | 前提条件 | 操作 | 期待結果 |
|----|------------|---------|------|---------|
| TC-01-01 | テーブル作成とデータ投入 | DBが空 | SetupAsync() | Success=true, RowCount=100（テスト用） |
| TC-01-02 | 既存データがあればスキップ | データあり | SetupAsync() を2回 | 2回目も Success=true、データ件数は変わらない |
| TC-01-03 | 実行時間が記録される | DBが空 | SetupAsync() | ExecutionTimeMs > 0 |

---

### TC-02: SELECT * 全カラム取得

| No | テストケース | 前提条件 | 操作 | 期待結果 |
|----|------------|---------|------|---------|
| TC-02-01 | 全カラムを返す | データあり | GetAllColumnsAsync() | Bio / Preferences / ActivityLog を含む |
| TC-02-02 | 件数が正しい | データ100件 | GetAllColumnsAsync() | RowCount = 100 |
| TC-02-03 | データサイズが大きい | データあり | GetAllColumnsAsync() | DataSize > 1KB（大容量カラムのため） |
| TC-02-04 | AWS転送料が計算される | データあり | GetAllColumnsAsync() | AwsCostEstimate > 0 |
| TC-02-05 | 実行時間が記録される | データあり | GetAllColumnsAsync() | ExecutionTimeMs >= 0 |
| TC-02-06 | SQLが正しい | - | GetAllColumnsAsync() | Sql = "SELECT * FROM Profiles" |

---

### TC-03: 必要カラムのみ取得

| No | テストケース | 前提条件 | 操作 | 期待結果 |
|----|------------|---------|------|---------|
| TC-03-01 | Id/Name/Email のみ返す | データあり | GetSpecificColumnsAsync() | Bio / Preferences / ActivityLog を含まない |
| TC-03-02 | 件数が正しい | データ100件 | GetSpecificColumnsAsync() | RowCount = 100 |
| TC-03-03 | データサイズが小さい | データあり | GetSpecificColumnsAsync() | DataSize < GetAllColumnsAsync().DataSize |
| TC-03-04 | AWS転送料が計算される | データあり | GetSpecificColumnsAsync() | AwsCostEstimate > 0 |
| TC-03-05 | 実行時間が記録される | データあり | GetSpecificColumnsAsync() | ExecutionTimeMs >= 0 |
| TC-03-06 | SQLが正しい | - | GetSpecificColumnsAsync() | Sql = "SELECT Id, Name, Email FROM Profiles" |

---

### TC-04: サイズ比較

| No | テストケース | 前提条件 | 操作 | 期待結果 |
|----|------------|---------|------|---------|
| TC-04-01 | 全カラムの方がサイズが大きい | データ100件 | 両方実行して比較 | allColumns.DataSize > specificColumns.DataSize |
| TC-04-02 | サイズ差が大幅 | データ100件 | 両方実行して比較 | allColumns.DataSize > specificColumns.DataSize × 10 |
| TC-04-03 | AWS転送料も全カラムの方が高い | データ100件 | 両方実行して比較 | allColumns.AwsCostEstimate > specificColumns.AwsCostEstimate |

---

## 2. 手動テスト（ローカル確認）

### MT-01: 画面確認

| No | テストケース | 手順 | 期待結果 |
|----|------------|------|---------|
| MT-01-01 | ページアクセス | /dotnet/Demo/SelectStar にアクセス | 画面が正常表示される |
| MT-01-02 | セットアップ実行 | Step1「セットアップ実行」ボタン押下 | 1万件生成完了メッセージ |
| MT-01-03 | SELECT * 実行 | Step2「SELECT * 実行」ボタン押下 | 実行時間・データサイズ・AWS転送料が表示される |
| MT-01-04 | 必要カラムのみ実行 | Step3「必要カラムのみ実行」ボタン押下 | SELECT * より小さいサイズが表示される |
| MT-01-05 | 比較表示 | Step2・Step3 実行後 | サイズ差・コスト差が一覧で比較できる |

### MT-02: API確認

| No | テストケース | 手順 | 期待結果 |
|----|------------|------|---------|
| MT-02-01 | セットアップAPI | POST /api/demo/select-star/setup | 200 OK, success=true |
| MT-02-02 | 全カラムAPI | GET /api/demo/select-star/all-columns | 200 OK, dataSize > 100MB |
| MT-02-03 | 必要カラムAPI | GET /api/demo/select-star/specific-columns | 200 OK, dataSize < 1MB |

---

## 3. CI テスト

| No | 確認項目 | 期待結果 |
|----|---------|---------|
| CI-01 | dotnet test | 全テスト PASS |
| CI-02 | dotnet build | ビルド成功（警告なし） |
