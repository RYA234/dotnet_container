# 外部インターフェース設計

## 外部システム連携一覧

| No | 外部システム名 | 用途 | 接続方法 | ステータス |
|----|-------------|------|---------|----------|
| IF-01 | Supabase | 認証、データベース | REST API | ✅ 実装済み |
| IF-02 | AWS Secrets Manager | 秘密情報管理 | AWS SDK | ✅ 実装済み |

---

## IF-01: Supabase

### 概要
- **用途**: PostgreSQLデータベース、認証（将来実装）
- **接続方法**: REST API / PostgreSQL Protocol
- **エンドポイント**: https://jfopjsynoorupqptjlep.supabase.co

### 認証
```json
{
  "Authorization": "Bearer {SUPABASE_ANON_KEY}"
}
```

### 接続テストエンドポイント
**パス**: `GET /supabase/test`

**リクエスト**: なし

**レスポンス（成功時）**:
```json
{
  "success": true,
  "message": "Supabase connection successful"
}
```

**レスポンス（失敗時）**:
```json
{
  "success": false,
  "message": "Connection failed: [エラー内容]"
}
```

### 環境変数
| 変数名 | 説明 | 取得元 |
|-------|------|--------|
| SUPABASE_URL | SupabaseエンドポイントURL | AWS Secrets Manager |
| SUPABASE_ANON_KEY | Supabase Anonymous Key | AWS Secrets Manager |

### エラーハンドリング
- **接続失敗時**: HTTP 500 エラー、ログ出力
- **タイムアウト**: 30秒
- **リトライ**: なし（教育用デモのため）

---

## IF-02: AWS Secrets Manager

### 概要
- **用途**: 秘密情報の一元管理
- **接続方法**: AWS SDK for .NET
- **リージョン**: ap-northeast-1

### 秘密情報一覧

#### ecs/dotnet-container/supabase
```json
{
  "url": "https://jfopjsynoorupqptjlep.supabase.co",
  "anon_key": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### ecs/typescript-container/supabase
```json
{
  "url": "https://jfopjsynoorupqptjlep.supabase.co",
  "anon_key": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 取得方法
```csharp
var secretString = await client.GetSecretValueAsync(new GetSecretValueRequest
{
    SecretId = "ecs/dotnet-container/supabase",
    VersionStage = "AWSCURRENT"
});
```

### エラーハンドリング
- **ResourceNotFoundException**: シークレットが存在しない
- **InvalidRequestException**: リクエストパラメータエラー
- **DecryptionFailure**: 復号化失敗

### セキュリティ設計
- **アクセス制御**: ECS Task Role で制限
- **暗号化**: KMS 暗号化（AWS管理キー）
- **ローテーション**: 手動（将来自動化を検討）

---

## セキュリティ設計

### 通信暗号化
- **HTTPS必須**: TLS 1.2以上
- **証明書検証**: 有効

### 秘密情報の扱い
- ❌ **環境変数に直接記載しない**
- ❌ **コードにハードコードしない**
- ✅ **AWS Secrets Manager から実行時に取得**
- ✅ **ログに秘密情報を出力しない**

### 入力検証
- **SQLインジェクション対策**: パラメータ化クエリ
- **XSS対策**: HTMLエンコーディング
- **CSRF対策**: Anti-CSRFトークン（将来実装）

---

## エラーレスポンス形式

### 共通エラー形式
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

---

## 参考

- [運用設計手順書 - インシデント対応](../operations.md#4-インシデント対応手順)
- 実例: 2025-12-10 anon_key 問題（[operations.md](../operations.md) 参照）
