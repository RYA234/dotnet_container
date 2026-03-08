# セキュリティ実装設計

## SQLインジェクション対策

### パラメータ化クエリ

#### ❌ Bad: 文字列連結
```csharp
var userId = Request.Query["id"];
var sql = $"SELECT * FROM Users WHERE Id = {userId}";
var command = new SqliteCommand(sql, connection);
```

**問題点**:
- ユーザー入力 `userId = "1 OR 1=1"` で全データが取得される
- ユーザー入力 `userId = "1; DROP TABLE Users--"` でテーブル削除される

#### ✅ Good: パラメータ化クエリ
```csharp
var userId = Request.Query["id"];
var sql = "SELECT * FROM Users WHERE Id = @UserId";
var command = new SqliteCommand(sql, connection);
command.Parameters.AddWithValue("@UserId", userId);
```

**理由**:
- プレースホルダ `@UserId` により、ユーザー入力がSQL文として解釈されない
- 安全にエスケープされる

---

## 秘密情報管理

### AWS Secrets Manager

#### ❌ Bad: ハードコード
```csharp
var supabaseUrl = "https://jfopjsynoorupqptjlep.supabase.co";
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
```

**問題点**:
- コードに秘密情報が含まれる
- Gitにコミットされる
- ローテーション時にコード修正が必要

#### ✅ Good: AWS Secrets Manager
```csharp
var client = new AmazonSecretsManagerClient(RegionEndpoint.APNortheast1);
var request = new GetSecretValueRequest
{
    SecretId = "ecs/dotnet-container/supabase",
    VersionStage = "AWSCURRENT"
};

var response = await client.GetSecretValueAsync(request);
var secret = JsonSerializer.Deserialize<SupabaseSecret>(response.SecretString);
var supabaseUrl = secret.Url;
var supabaseKey = secret.AnonKey;
```

**理由**:
- 秘密情報がコードに含まれない
- IAMロールで アクセス制御
- ローテーション時にコード修正不要

---

## XSS対策

### HTMLエンコーディング

#### ❌ Bad: 生のHTML出力
```csharp
@Model.UserInput
```

**問題点**:
- ユーザー入力 `<script>alert('XSS')</script>` がそのまま実行される

#### ✅ Good: HTMLエンコーディング
```csharp
@Html.Encode(Model.UserInput)
```

**理由**:
- `<` → `&lt;`
- `>` → `&gt;`
- `"` → `&quot;`
- スクリプトが実行されない

---

## CSRF対策（将来実装）

### Anti-CSRFトークン

#### フォーム
```html
<form method="post" asp-action="Calculate">
    @Html.AntiForgeryToken()
    <input type="text" name="a" />
    <input type="text" name="operation" />
    <input type="text" name="b" />
    <button type="submit">計算</button>
</form>
```

#### Controller
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Calculate(decimal a, string operation, decimal b)
{
    // ...
}
```

**理由**:
- フォーム送信時にトークンを検証
- 外部サイトからのフォーム送信を防止

---

## 通信暗号化

### HTTPS必須

#### Program.cs
```csharp
app.UseHttpsRedirection();
```

### TLS 1.2以上
- **証明書**: Let's Encrypt（ALB経由）
- **暗号化スイート**: TLS_AES_128_GCM_SHA256
- **証明書検証**: 有効

---

## 認証・認可（将来実装）

### Supabase Auth

#### ログイン
```csharp
var response = await supabaseClient.Auth.SignIn(email, password);
var token = response.AccessToken;
```

#### JWT検証
```csharp
[Authorize]
public class InventoryController : Controller
{
    // JWT必須
}
```

---

## ログに秘密情報を出力しない

### ❌ Bad: 秘密情報をログ出力
```csharp
_logger.LogInformation("Supabase URL: {Url}, Key: {Key}", supabaseUrl, supabaseKey);
```

**問題点**:
- CloudWatch Logs に秘密情報が記録される
- ログを見た人が秘密情報を取得できる

### ✅ Good: マスク
```csharp
_logger.LogInformation("Supabase URL: {Url}, Key: {Key}", supabaseUrl, MaskSecret(supabaseKey));

private string MaskSecret(string secret)
{
    if (string.IsNullOrEmpty(secret) || secret.Length < 8)
        return "***";
    return secret.Substring(0, 4) + "***" + secret.Substring(secret.Length - 4);
}
```

**出力例**:
```
Supabase URL: https://jfopjsynoorupqptjlep.supabase.co, Key: eyJh***XVCJ9
```

---

## セキュリティヘッダー

### Program.cs
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    await next();
});
```

**ヘッダー一覧**:

| ヘッダー | 値 | 意味 |
|---------|-----|------|
| X-Content-Type-Options | nosniff | MIMEタイプスニッフィング防止 |
| X-Frame-Options | DENY | クリックジャッキング防止 |
| X-XSS-Protection | 1; mode=block | XSS防止（古いブラウザ用） |
| Strict-Transport-Security | max-age=31536000 | HTTPS強制 |

---

## 入力検証

### データアノテーション

```csharp
public class CalculateRequest
{
    [Required]
    [Range(-1000000, 1000000)]
    public decimal A { get; set; }

    [Required]
    [RegularExpression(@"^[+\-*/]$")]
    public string Operation { get; set; }

    [Required]
    [Range(-1000000, 1000000)]
    public decimal B { get; set; }
}
```

### Controller
```csharp
[HttpPost]
public IActionResult Calculate([FromForm] CalculateRequest request)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }
    // ...
}
```

---

## セキュリティスキャン（将来実装）

### OWASP Dependency-Check
```bash
dotnet tool install --global dotnet-retire
dotnet retire
```

### Trivy（Dockerイメージスキャン）
```bash
trivy image 110221759530.dkr.ecr.ap-northeast-1.amazonaws.com/dotnet-app:latest
```

---

## セキュリティ教育用デモ（未実装）

### OWASP Top 10

| No | 脆弱性 | デモ | ステータス |
|----|-------|------|----------|
| 1 | SQLインジェクション | Bad版とGood版の比較 | 🚧 未実装 |
| 2 | XSS | Bad版とGood版の比較 | 🚧 未実装 |
| 3 | CSRF | トークンあり/なしの比較 | 🚧 未実装 |
| 4 | 認証の不備 | パスワード平文保存 vs ハッシュ化 | 🚧 未実装 |
| 5 | 不適切なアクセス制御 | 権限チェックあり/なし | 🚧 未実装 |

---

## 実例: 2025-12-10 Secrets Manager キー名不一致

### 発生したエラー
```
retrieved secret from Secrets Manager did not contain json key anon_key
```

### 原因
- Secrets Manager のキー名: `anonKey` (camelCase)
- タスク定義の参照: `anon_key` (snake_case)

### 対応
```bash
aws secretsmanager update-secret \
  --secret-id ecs/typescript-container/supabase \
  --secret-string '{"url":"...","anon_key":"..."}' \
  --region ap-northeast-1
```

### 教訓
- **命名規則統一**: snake_case に統一
- **環境変数検証**: 起動時にチェック

詳細: [運用設計手順書 - インシデント対応](../operations.md#ケース1-ecs-タスクが起動しない)

---

## 参考

- [外部IF設計](external-interface.md)
- [エラー処理設計](error-handling.md)
- [運用設計手順書](../operations.md)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
