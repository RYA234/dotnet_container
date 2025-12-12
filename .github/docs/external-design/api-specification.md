# API仕様書

## エンドポイント一覧

### 教育用デモ

| No | メソッド | パス | 概要 | レスポンス |
|----|---------|------|------|----------|
| A-01 | GET | /api/demo/n-plus-one/bad | N+1問題（非効率版） | NPlusOneResponse |
| A-02 | GET | /api/demo/n-plus-one/good | N+1問題（最適化版） | NPlusOneResponse |

### 基幹システム

| No | メソッド | パス | 概要 | レスポンス |
|----|---------|------|------|----------|
| A-03 | GET | /healthz | ヘルスチェック | HealthResponse |
| A-04 | GET | /supabase/test | Supabase接続テスト | ConnectionTestResponse |

### WinFormsマイグレーション

| No | メソッド | パス | 概要 | レスポンス |
|----|---------|------|------|----------|
| A-05 | POST | /dotnet/Calculator/Calculate | 四則演算 | CalculateResponse |

---

## API詳細仕様

### A-01: N+1問題（非効率版）

#### エンドポイント
```
GET /api/demo/n-plus-one/bad
```

#### リクエスト
なし

#### レスポンス
```json
{
  "executionTimeMs": 45,
  "sqlCount": 101,
  "dataSize": 5621,
  "rowCount": 100,
  "message": "N+1問題あり: ループ内で部署情報を100回個別に取得しています（合計101クエリ）",
  "data": [
    {
      "id": 1,
      "name": "User 1",
      "department": {
        "id": 1,
        "name": "Department 1"
      }
    }
  ]
}
```

#### ステータスコード
| コード | 意味 | 説明 |
|-------|------|------|
| 200 | OK | 正常終了 |
| 500 | Internal Server Error | サーバーエラー |

---

### A-02: N+1問題（最適化版）

#### エンドポイント
```
GET /api/demo/n-plus-one/good
```

#### リクエスト
なし

#### レスポンス
```json
{
  "executionTimeMs": 12,
  "sqlCount": 1,
  "dataSize": 5621,
  "rowCount": 100,
  "message": "最適化済み: 1回のJOINクエリで全データを取得しています",
  "data": [
    {
      "id": 1,
      "name": "User 1",
      "department": {
        "id": 1,
        "name": "Department 1"
      }
    }
  ]
}
```

#### ステータスコード
| コード | 意味 | 説明 |
|-------|------|------|
| 200 | OK | 正常終了 |
| 500 | Internal Server Error | サーバーエラー |

---

### A-03: ヘルスチェック

#### エンドポイント
```
GET /healthz
```

#### リクエスト
なし

#### レスポンス
```json
{
  "status": "healthy"
}
```

#### ステータスコード
| コード | 意味 | 説明 |
|-------|------|------|
| 200 | OK | 正常稼働中 |
| 503 | Service Unavailable | サービス停止中 |

---

### A-04: Supabase接続テスト

#### エンドポイント
```
GET /supabase/test
```

#### リクエスト
なし

#### レスポンス（成功時）
```json
{
  "success": true,
  "message": "Supabase connection successful"
}
```

#### レスポンス（失敗時）
```json
{
  "success": false,
  "message": "Connection failed: [エラー内容]"
}
```

#### ステータスコード
| コード | 意味 | 説明 |
|-------|------|------|
| 200 | OK | 接続成功 |
| 500 | Internal Server Error | 接続失敗 |

---

### A-05: 四則演算

#### エンドポイント
```
POST /dotnet/Calculator/Calculate
```

#### リクエスト
```
Content-Type: application/x-www-form-urlencoded

a=10&operation=+&b=5
```

| パラメータ | 型 | 必須 | 説明 |
|-----------|-----|------|------|
| a | decimal | ✅ | 第一オペランド |
| operation | string | ✅ | 演算子 (+, -, *, /) |
| b | decimal | ✅ | 第二オペランド |

#### レスポンス
```json
{
  "result": 15
}
```

#### ステータスコード
| コード | 意味 | 説明 |
|-------|------|------|
| 200 | OK | 計算成功 |
| 400 | Bad Request | パラメータエラー |
| 500 | Internal Server Error | サーバーエラー |

---

## データ型定義

### NPlusOneResponse
```csharp
public class NPlusOneResponse
{
    public long ExecutionTimeMs { get; set; }
    public int SqlCount { get; set; }
    public int DataSize { get; set; }
    public int RowCount { get; set; }
    public string Message { get; set; }
    public List<UserWithDepartment> Data { get; set; }
}
```

### UserWithDepartment
```csharp
public class UserWithDepartment
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DepartmentInfo Department { get; set; }
}
```

### DepartmentInfo
```csharp
public class DepartmentInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

---

## エラーレスポンス共通仕様

### エラーレスポンス形式
```json
{
  "error": "エラーメッセージ",
  "code": "ERROR_CODE",
  "timestamp": "2025-12-10T12:00:00Z"
}
```

### HTTPステータスコード
| コード | 意味 | 使用例 |
|-------|------|--------|
| 200 | OK | 正常レスポンス |
| 400 | Bad Request | 入力エラー |
| 500 | Internal Server Error | サーバーエラー |
| 503 | Service Unavailable | サービス停止中 |
