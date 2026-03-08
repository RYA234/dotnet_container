# API設計

## 文書情報
- **作成日**: 2026-03-07
- **バージョン**: 1.0
- **ステータス**: ドラフト

---

## 1. 基本方針

1. **REST 準拠**: リソース指向の URL 設計。動詞をURLに含めない
2. **統一レスポンス形式**: 成功・エラー問わず JSON で返す
3. **HTTPメソッドの正しい使い分け**: GET / POST / PUT / DELETE を意味通りに使う
4. **バージョニング**: URL パスにバージョンを含める（`/api/v1/`）

---

## 2. URL設計規約

### 2.1 基本構造

```
/api/v{バージョン}/{リソース名}/{ID}/{サブリソース}
```

| 例 | 説明 |
|----|------|
| `GET /api/v1/orders` | 受注一覧取得 |
| `GET /api/v1/orders/123` | 受注1件取得 |
| `POST /api/v1/orders` | 受注登録 |
| `PUT /api/v1/orders/123` | 受注更新 |
| `DELETE /api/v1/orders/123` | 受注削除 |
| `GET /api/v1/orders/123/items` | 受注明細一覧取得 |

---

### 2.2 命名規則

| 規則 | OK | NG |
|------|----|----|
| リソース名は複数形・小文字 | `/api/v1/orders` | `/api/v1/Order`, `/api/v1/getOrders` |
| 単語区切りはハイフン | `/api/v1/order-items` | `/api/v1/orderItems`, `/api/v1/order_items` |
| URLに動詞を含めない | `POST /api/v1/orders` | `/api/v1/createOrder` |
| デモ用エンドポイントは `/demo/` プレフィックス | `/api/v1/demo/n-plus-one` | `/api/v1/nPlusOne` |

---

### 2.3 クエリパラメータ

| 用途 | パラメータ名 | 例 |
|------|------------|-----|
| ページ番号 | `page` | `?page=1` |
| 1ページの件数 | `pageSize` | `?pageSize=20` |
| 検索キーワード | `q` | `?q=田中` |
| ソート項目 | `sortBy` | `?sortBy=createdAt` |
| ソート方向 | `sortOrder` | `?sortOrder=asc` |

---

## 3. HTTPメソッド規約

| メソッド | 用途 | 冪等性 | レスポンス |
|---------|------|--------|-----------|
| `GET` | リソース取得 | ○ | 200 OK |
| `POST` | リソース作成 | ✕ | 201 Created + Location ヘッダー |
| `PUT` | リソース全体更新 | ○ | 200 OK |
| `PATCH` | リソース部分更新 | ✕ | 200 OK |
| `DELETE` | リソース削除 | ○ | 204 No Content |

---

## 4. HTTPステータスコード規約

### 4.1 成功系

| コード | 用途 |
|--------|------|
| `200 OK` | GET / PUT / PATCH 成功 |
| `201 Created` | POST 成功（リソース作成） |
| `204 No Content` | DELETE 成功 |

### 4.2 エラー系

| コード | 用途 | 例 |
|--------|------|-----|
| `400 Bad Request` | バリデーションエラー・リクエスト不正 | 必須項目未入力 |
| `401 Unauthorized` | 未認証 | トークンなし |
| `403 Forbidden` | 権限不足 | 他ユーザーのリソースへのアクセス |
| `404 Not Found` | リソースが存在しない | 存在しない受注ID |
| `409 Conflict` | 重複・競合 | 重複コード登録 |
| `500 Internal Server Error` | サーバー内部エラー | 予期しない例外 |

---

## 5. レスポンス形式

### 5.1 成功レスポンス（単件）

```json
{
  "id": 1,
  "customerCode": "C001",
  "orderDate": "2026-03-07T00:00:00Z",
  "totalAmount": 10000
}
```

### 5.2 成功レスポンス（一覧）

```json
{
  "data": [
    { "id": 1, "customerCode": "C001" },
    { "id": 2, "customerCode": "C002" }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 20
}
```

### 5.3 エラーレスポンス

```json
{
  "type": "ValidationError",
  "errors": [
    { "field": "CustomerCode", "message": "顧客コードは必須です" }
  ],
  "timestamp": "2026-03-07T00:00:00Z"
}
```

> エラーレスポンス形式の詳細は [バリデーション設計](validation.md) / [エラーハンドリング設計](error-handling.md) を参照。

---

## 6. デモ用エンドポイント規約

このプロジェクトはSQLアンチパターンのデモを目的とする。デモ用エンドポイントは以下の規約に従う。

| 規約 | 内容 |
|------|------|
| URLプレフィックス | `/api/v1/demo/` |
| Bad パターン | `/api/v1/demo/{feature}/bad` |
| Good パターン | `/api/v1/demo/{feature}/good` |

```
GET /api/v1/demo/n-plus-one/bad    ← N+1発生パターン
GET /api/v1/demo/n-plus-one/good   ← N+1解決パターン
GET /api/v1/demo/select-star/bad   ← SELECT * パターン
GET /api/v1/demo/select-star/good  ← 必要カラム指定パターン
GET /api/v1/demo/like-search/bad   ← 後方LIKE検索パターン
GET /api/v1/demo/like-search/good  ← 前方LIKE検索パターン
GET /api/v1/demo/full-table-scan/bad   ← 全件取得パターン
GET /api/v1/demo/full-table-scan/good  ← ページングパターン
```

---

## 7. 未決事項

- [ ] バージョニング方法の確定（URLパス vs ヘッダー）
- [ ] 認証方式の選定（JWT / Cookie / API Key）
- [ ] ページングの最大 pageSize の上限値

---

## 8. 参考

- [クラス図](class-diagram.md)
- [エラーハンドリング設計](error-handling.md)
- [バリデーション設計](validation.md)

### Microsoft Learn 公式リファレンス

| 内容 | URL |
|------|-----|
| ASP.NET Core MVC の概要 | https://learn.microsoft.com/ja-jp/aspnet/core/mvc/overview |
| ASP.NET Core のルーティング | https://learn.microsoft.com/ja-jp/aspnet/core/fundamentals/routing |
| ASP.NET Core でコントローラーアクションの戻り値の型 | https://learn.microsoft.com/ja-jp/aspnet/core/web-api/action-return-types |
| REST API 設計のベストプラクティス（Azure Architecture） | https://learn.microsoft.com/ja-jp/azure/architecture/best-practices/api-design |
| HTTP ステータスコード（RFC 準拠） | https://learn.microsoft.com/ja-jp/dotnet/api/system.net.httpstatuscode |
