# API設計 テスト仕様書

## 文書情報
- **作成日**: 2026-03-07
- **最終更新**: 2026-03-07
- **バージョン**: 1.0
- **ステータス**: ドラフト
- **関連設計書**: [API設計](api-design.md)

---

## 1. テスト対象

### 1.1 テスト対象コンポーネント

| コンポーネント | 説明 |
|-------------|------|
| URL規約 | リソース名・バージョニング・命名規則の遵守 |
| HTTPメソッド | GET / POST / PUT / DELETE の正しい使い分けと冪等性 |
| HTTPステータスコード | 成功・エラーの各ケースで正しいコードが返ること |
| レスポンス形式 | 成功レスポンス（単件・一覧）・エラーレスポンスの統一形式 |
| デモ用エンドポイント | `/api/v1/demo/{feature}/bad` / `good` の動作確認 |

---

## 2. テスト計画

### 2.1 テスト方針

1. **URL規約の遵守**: エンドポイントが命名規則・バージョニングに従っていること
2. **HTTPメソッドの正確性**: 各メソッドが仕様通りのステータスコードを返すこと
3. **レスポンス形式の統一**: 成功・エラー問わず定義した JSON 形式で返ること
4. **デモエンドポイントの差異**: bad / good パターンが明確に異なる動作をすること

---

### 2.2 テストレベル

| テストレベル | 対象 | 目的 |
|------------|------|------|
| 統合テスト | Controller + Service + インメモリDB | ステータスコード・レスポンス形式の確認 |
| E2E テスト | APIエンドポイント全体 | 実際の HTTP リクエスト・レスポンスの確認 |

---

### 2.3 テストカバレッジ目標

| カテゴリ | 目標カバレッジ | 備考 |
|---------|--------------|------|
| HTTPステータスコード | 100% | 定義した全コード（200/201/204/400/404/409/500）をカバー |
| レスポンス形式 | 100% | 成功・エラーの全フィールドを確認 |
| デモエンドポイント | 100% | bad / good の全パターンをカバー |
| クエリパラメータ | 80% | ページング・ソートの主要パターンをカバー |

---

## 3. HTTPメソッド・ステータスコードのテストケース

### 3.1 GET（リソース取得）

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-AP-001 | リソースが存在する場合、200 を返す | 対象リソースがDBに存在する | `GET /api/v1/{resource}/{id}` | StatusCode=200、レスポンスボディに対象リソースの JSON | 高 |
| TC-AP-002 | リソースが存在しない場合、404 を返す | 対象 ID がDBに存在しない | `GET /api/v1/{resource}/9999` | StatusCode=404 | 高 |
| TC-AP-003 | 一覧取得が成功する場合、200 を返す | DBにデータが存在する | `GET /api/v1/{resource}` | StatusCode=200、`data` 配列・`totalCount` を含む JSON | 高 |
| TC-AP-004 | データが0件の場合、200 と空配列を返す | DBにデータが存在しない | `GET /api/v1/{resource}` | StatusCode=200、`data=[]`・`totalCount=0` | 中 |

---

### 3.2 POST（リソース作成）

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-AP-005 | 正常なリクエストの場合、201 を返す | 入力データが有効 | `POST /api/v1/{resource}` + 有効な JSON ボディ | StatusCode=201 | 高 |
| TC-AP-006 | バリデーションエラーの場合、400 を返す | 必須フィールドが欠損 | `POST /api/v1/{resource}` + 不正な JSON ボディ | StatusCode=400、`type="ValidationError"` | 高 |
| TC-AP-007 | 重複リソースの場合、409 を返す | 同じコードが既に存在する | `POST /api/v1/{resource}` + 重複コードを含むボディ | StatusCode=409 | 高 |

---

### 3.3 PUT（リソース更新）

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-AP-008 | 正常なリクエストの場合、200 を返す | 対象リソースが存在し、入力データが有効 | `PUT /api/v1/{resource}/{id}` + 有効な JSON ボディ | StatusCode=200 | 高 |
| TC-AP-009 | 存在しないリソースへの更新は 404 を返す | 対象 ID がDBに存在しない | `PUT /api/v1/{resource}/9999` | StatusCode=404 | 高 |
| TC-AP-010 | バリデーションエラーの場合、400 を返す | 入力データが不正 | `PUT /api/v1/{resource}/{id}` + 不正な JSON ボディ | StatusCode=400 | 高 |

---

### 3.4 DELETE（リソース削除）

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-AP-011 | 正常なリクエストの場合、204 を返す | 対象リソースが存在する | `DELETE /api/v1/{resource}/{id}` | StatusCode=204、レスポンスボディなし | 高 |
| TC-AP-012 | 存在しないリソースへの削除は 404 を返す | 対象 ID がDBに存在しない | `DELETE /api/v1/{resource}/9999` | StatusCode=404 | 高 |

---

## 4. レスポンス形式のテストケース

### 4.1 成功レスポンス（単件）

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-AP-013 | 単件取得のレスポンスにラッパーがない | リソースが存在する | `GET /api/v1/{resource}/{id}` | レスポンスがリソースのフィールドを直接含む（`data` ラッパーなし） | 高 |
| TC-AP-014 | Content-Type が application/json | — | GET リクエスト | `Content-Type: application/json` | 中 |

---

### 4.2 成功レスポンス（一覧）

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-AP-015 | 一覧レスポンスに `data` 配列が存在する | データが存在する | `GET /api/v1/{resource}` | `response.data` が配列 | 高 |
| TC-AP-016 | 一覧レスポンスに `totalCount` が存在する | データが存在する | `GET /api/v1/{resource}` | `response.totalCount` が数値 | 高 |
| TC-AP-017 | 一覧レスポンスに `page` が存在する | ページングあり | `GET /api/v1/{resource}?page=1` | `response.page=1` | 中 |
| TC-AP-018 | 一覧レスポンスに `pageSize` が存在する | ページングあり | `GET /api/v1/{resource}?pageSize=20` | `response.pageSize=20` | 中 |

---

### 4.3 エラーレスポンス

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-AP-019 | エラーレスポンスに `type` フィールドが存在する | エラー発生 | 不正リクエスト | `response.type` が文字列 | 高 |
| TC-AP-020 | エラーレスポンスに `errors` 配列が存在する | バリデーションエラー発生 | 必須フィールド欠損 | `response.errors` が配列 | 高 |
| TC-AP-021 | エラーレスポンスに `timestamp` が存在する | エラー発生 | 不正リクエスト | `response.timestamp` が ISO 8601 形式 | 高 |
| TC-AP-022 | `errors` の各要素に `field` と `message` が存在する | バリデーションエラー発生 | 必須フィールド欠損 | `errors[0].field` と `errors[0].message` が文字列 | 高 |

---

## 5. デモ用エンドポイントのテストケース

### 5.1 N+1 デモ

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-AP-023 | bad エンドポイントが 200 を返す | DBにデータが存在する | `GET /api/v1/demo/n-plus-one/bad` | StatusCode=200 | 高 |
| TC-AP-024 | good エンドポイントが 200 を返す | DBにデータが存在する | `GET /api/v1/demo/n-plus-one/good` | StatusCode=200 | 高 |
| TC-AP-025 | bad と good で実行クエリ数が異なる | DBに10件以上のデータが存在する | bad / good を各1回呼び出し | bad のクエリ数 > good のクエリ数 | 高 |

---

### 5.2 SELECT * デモ

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-AP-026 | bad エンドポイントが 200 を返す | DBにデータが存在する | `GET /api/v1/demo/select-star/bad` | StatusCode=200 | 高 |
| TC-AP-027 | good エンドポイントが 200 を返す | DBにデータが存在する | `GET /api/v1/demo/select-star/good` | StatusCode=200 | 高 |
| TC-AP-028 | good のレスポンスに不要フィールドが含まれない | DBにデータが存在する | `GET /api/v1/demo/select-star/good` | 必要カラムのみ返る（全カラム返却なし） | 中 |

---

### 5.3 LIKE 検索デモ

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-AP-029 | bad エンドポイントが 200 を返す | DBにデータが存在する | `GET /api/v1/demo/like-search/bad` | StatusCode=200 | 高 |
| TC-AP-030 | good エンドポイントが 200 を返す | DBにデータが存在する | `GET /api/v1/demo/like-search/good` | StatusCode=200 | 高 |

---

### 5.4 全件取得デモ

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-AP-031 | bad エンドポイントが 200 を返す | DBにデータが存在する | `GET /api/v1/demo/full-table-scan/bad` | StatusCode=200 | 高 |
| TC-AP-032 | good エンドポイントが 200 を返す | DBにデータが存在する | `GET /api/v1/demo/full-table-scan/good` | StatusCode=200 | 高 |
| TC-AP-033 | good のレスポンスにページング情報が含まれる | DBにデータが存在する | `GET /api/v1/demo/full-table-scan/good` | `page`・`pageSize`・`totalCount` を含む | 高 |

---

## 6. テスト実装ガイドライン

### 6.1 テストツール推奨

| 用途 | 推奨ツール |
|------|----------|
| テストフレームワーク | xUnit |
| アサーション | FluentAssertions |
| HTTP クライアント | `WebApplicationFactory` + `HttpClient` |
| インメモリDB | Microsoft.Data.Sqlite（`:memory:`） |

---

### 6.2 テストデータ管理

1. **各テストで独立したデータを用意**: テスト間でデータを共有しない
2. **デモテストは件数を意識したデータ設計**: N+1 検証には10件以上のデータを用意する
3. **テスト終了後にDBを破棄**: インメモリDBを使用し、テスト間の干渉を防ぐ

---

## 7. テスト実行計画

### 7.1 実行順序

1. HTTPメソッド・ステータスコードのテスト（TC-AP-001 〜 TC-AP-012）
2. レスポンス形式のテスト（TC-AP-013 〜 TC-AP-022）
3. デモ用エンドポイントのテスト（TC-AP-023 〜 TC-AP-033）

---

### 7.2 テスト環境

| 項目 | 要件 |
|------|------|
| データベース | インメモリ DB |
| HTTP サーバー | `WebApplicationFactory`（統合テスト） / 実サーバー（E2E テスト） |
| 外部依存 | なし |

---

## 8. 参考

- [API設計](api-design.md)
- [エラーハンドリング設計](error-handling.md)
- [バリデーション設計](validation.md)
- [テスト設計](testing.md)

### Microsoft Learn 公式リファレンス

| 内容 | URL |
|------|-----|
| ASP.NET Core の統合テスト | https://learn.microsoft.com/ja-jp/aspnet/core/test/integration-tests |
| WebApplicationFactory | https://learn.microsoft.com/ja-jp/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1 |
| xUnit でのテスト（.NET） | https://learn.microsoft.com/ja-jp/dotnet/core/testing/unit-testing-with-dotnet-test |
| ASP.NET Core の HTTP クライアント（HttpClient） | https://learn.microsoft.com/ja-jp/dotnet/fundamentals/networking/http/httpclient |
