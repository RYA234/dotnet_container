# セキュリティ設計

## 文書情報
- **作成日**: 2025-12-12
- **最終更新**: 2025-12-12
- **バージョン**: 1.0
- **ステータス**: 実装中

---

## 1. セキュリティ基本方針

### 1.1 OWASP Top 10 対策

このプロジェクトは **OWASP Top 10 2021** に基づいたセキュリティ対策を実施します。

| No | 脆弱性 | 対策状況 |
|----|--------|---------|
| A01 | Broken Access Control | ✅ 実装済み |
| A02 | Cryptographic Failures | ✅ 実装済み |
| A03 | Injection | ✅ 実装済み |
| A04 | Insecure Design | ✅ 実装済み |
| A05 | Security Misconfiguration | 🚧 実装中 |
| A06 | Vulnerable Components | 🚧 実装中 |
| A07 | Authentication Failures | ✅ 実装済み |
| A08 | Software and Data Integrity | 🚧 実装中 |
| A09 | Security Logging Failures | ✅ 実装済み |
| A10 | Server-Side Request Forgery | ✅ 実装済み |

---

## 2. 認証・認可

### 2.1 Supabase 認証統合

```csharp
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

/// <summary>
/// Supabase認証サービス
/// </summary>
public class SupabaseAuthService : IAuthService
{
    private readonly IGotrueClient<User, Session> _authClient;
    private readonly ILogger<SupabaseAuthService> _logger;

    public SupabaseAuthService(
        IGotrueClient<User, Session> authClient,
        ILogger<SupabaseAuthService> logger)
    {
        _authClient = authClient;
        _logger = logger;
    }

    /// <summary>
    /// メール・パスワード認証
    /// </summary>
    public async Task<AuthResult> SignInWithPassword(string email, string password)
    {
        try
        {
            var session = await _authClient.SignIn(email, password);

            if (session?.AccessToken == null)
            {
                throw new UnauthorizedException("Invalid credentials");
            }

            _logger.LogInformation("User signed in: {Email}", email);

            return new AuthResult
            {
                AccessToken = session.AccessToken,
                RefreshToken = session.RefreshToken,
                User = MapUser(session.User),
                ExpiresAt = session.ExpiresAt
            };
        }
        catch (GotrueException ex)
        {
            _logger.LogWarning(ex, "Authentication failed: {Email}", email);
            throw new UnauthorizedException("Invalid credentials");
        }
    }

    /// <summary>
    /// トークンリフレッシュ
    /// </summary>
    public async Task<AuthResult> RefreshToken(string refreshToken)
    {
        try
        {
            var session = await _authClient.RefreshToken(refreshToken);

            return new AuthResult
            {
                AccessToken = session.AccessToken,
                RefreshToken = session.RefreshToken,
                User = MapUser(session.User),
                ExpiresAt = session.ExpiresAt
            };
        }
        catch (GotrueException ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            throw new UnauthorizedException("Invalid refresh token");
        }
    }

    /// <summary>
    /// サインアウト
    /// </summary>
    public async Task SignOut()
    {
        await _authClient.SignOut();
        _logger.LogInformation("User signed out");
    }

    private static UserInfo MapUser(User user)
    {
        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmedAt.HasValue,
            CreatedAt = user.CreatedAt
        };
    }
}
```

---

### 2.2 JWT トークン検証

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Program.cs での設定
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSecret = builder.Configuration["Supabase:JwtSecret"];
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("JWT secret not configured");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Supabase:Url"],
            ValidAudience = builder.Configuration["Supabase:Url"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero  // トークン有効期限の猶予なし
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication failed: {Exception}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validated for user: {UserId}",
                    context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
```

---

### 2.3 認可ポリシー

```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    // 管理者のみ
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));

    // メール確認済みユーザーのみ
    options.AddPolicy("EmailConfirmed", policy =>
        policy.RequireClaim("email_confirmed", "true"));

    // 特定の権限が必要
    options.AddPolicy("CanDeleteUser", policy =>
        policy.RequireClaim("permissions", "users.delete"));
});

// Controller での使用
[Authorize(Policy = "AdminOnly")]
[HttpDelete("api/users/{id}")]
public async Task<IActionResult> DeleteUser(int id)
{
    await _userService.DeleteUser(id);
    return NoContent();
}
```

---

## 3. SQLインジェクション対策

### 3.1 パラメータ化クエリ（必須）

```csharp
// ❌ NG: 文字列連結（SQLインジェクションのリスク）
var sql = $"SELECT * FROM Users WHERE Email = '{email}'";

// ✅ OK: パラメータ化クエリ
var sql = "SELECT * FROM Users WHERE Email = @Email";
using var command = new SqliteCommand(sql, connection);
command.Parameters.AddWithValue("@Email", email);
```

---

### 3.2 動的SQLの安全な実装

```csharp
/// <summary>
/// 検索条件を動的に構築（安全な実装）
/// </summary>
public async Task<List<User>> SearchUsers(UserSearchRequest request)
{
    var conditions = new List<string>();
    var parameters = new Dictionary<string, object>();

    // 名前検索
    if (!string.IsNullOrEmpty(request.Name))
    {
        conditions.Add("Name LIKE @Name");
        parameters["@Name"] = $"%{request.Name}%";
    }

    // メール検索
    if (!string.IsNullOrEmpty(request.Email))
    {
        conditions.Add("Email = @Email");
        parameters["@Email"] = request.Email;
    }

    // 日付範囲
    if (request.CreatedAfter.HasValue)
    {
        conditions.Add("CreatedAt >= @CreatedAfter");
        parameters["@CreatedAfter"] = request.CreatedAfter.Value;
    }

    var whereClause = conditions.Count > 0
        ? "WHERE " + string.Join(" AND ", conditions)
        : "";

    var sql = $"SELECT Id, Name, Email, CreatedAt FROM Users {whereClause}";

    using var connection = GetConnection();
    await connection.OpenAsync();

    using var command = new SqliteCommand(sql, connection);
    foreach (var param in parameters)
    {
        command.Parameters.AddWithValue(param.Key, param.Value);
    }

    // ...
}
```

---

### 3.3 ホワイトリスト検証（ORDER BY、LIMIT等）

```csharp
/// <summary>
/// ソート順の安全な実装
/// </summary>
public async Task<List<User>> GetUsers(string sortBy, string sortOrder)
{
    // ホワイトリストで許可されたカラムのみ
    var allowedSortColumns = new HashSet<string> { "Id", "Name", "Email", "CreatedAt" };
    if (!allowedSortColumns.Contains(sortBy))
    {
        throw new ValidationException("Invalid sort column");
    }

    // ホワイトリストで許可された順序のみ
    var allowedSortOrders = new HashSet<string> { "ASC", "DESC" };
    if (!allowedSortOrders.Contains(sortOrder.ToUpper()))
    {
        throw new ValidationException("Invalid sort order");
    }

    // 安全にSQL構築
    var sql = $"SELECT Id, Name, Email, CreatedAt FROM Users ORDER BY {sortBy} {sortOrder.ToUpper()}";

    // ... クエリ実行
}
```

---

## 4. XSS（クロスサイトスクリプティング）対策

### 4.1 Razor View の自動エスケープ

```cshtml
@* Razor View は自動的にHTMLエスケープされる *@
<p>ユーザー名: @Model.Name</p>  @* 安全 *@

@* 明示的にHTMLをレンダリングする場合は注意 *@
<div>@Html.Raw(Model.Description)</div>  @* 危険: XSSリスク *@

@* サニタイズ関数を使用 *@
<div>@Html.Sanitize(Model.Description)</div>  @* 安全 *@
```

---

### 4.2 HTML サニタイゼーション

```csharp
using Ganss.Xss;

/// <summary>
/// HTMLサニタイゼーションサービス
/// </summary>
public class HtmlSanitizer : IHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        // 許可するタグ
        _sanitizer.AllowedTags = new HashSet<string>
        {
            "p", "br", "strong", "em", "u", "a", "ul", "ol", "li"
        };

        // 許可する属性
        _sanitizer.AllowedAttributes = new HashSet<string>
        {
            "href", "title"
        };

        // 許可するスキーム
        _sanitizer.AllowedSchemes = new HashSet<string>
        {
            "http", "https"
        };
    }

    public string Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        return _sanitizer.Sanitize(html);
    }
}
```

---

### 4.3 Content Security Policy (CSP)

```csharp
// Program.cs
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "connect-src 'self' https://your-supabase-url.supabase.co");

    await next();
});
```

---

## 5. CSRF（クロスサイトリクエストフォージェリ）対策

### 5.1 AntiForgery トークン

```csharp
// Program.cs
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Controller
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    // ...
}
```

---

### 5.2 SameSite Cookie 設定

```csharp
// Program.cs
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.HttpOnly = true;
});
```

---

## 6. 秘密情報管理

### 6.1 AWS Secrets Manager 統合

```csharp
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

/// <summary>
/// AWS Secrets Manager サービス
/// </summary>
public class SecretsManagerService : ISecretsService
{
    private readonly IAmazonSecretsManager _secretsManager;
    private readonly ILogger<SecretsManagerService> _logger;

    public SecretsManagerService(
        IAmazonSecretsManager secretsManager,
        ILogger<SecretsManagerService> logger)
    {
        _secretsManager = secretsManager;
        _logger = logger;
    }

    /// <summary>
    /// シークレット取得
    /// </summary>
    public async Task<string> GetSecret(string secretName)
    {
        try
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretName
            };

            var response = await _secretsManager.GetSecretValueAsync(request);
            return response.SecretString;
        }
        catch (ResourceNotFoundException)
        {
            _logger.LogError("Secret not found: {SecretName}", secretName);
            throw new InfrastructureException(
                "SecretsManager",
                $"Secret '{secretName}' not found",
                new Exception());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret: {SecretName}", secretName);
            throw;
        }
    }

    /// <summary>
    /// データベース接続文字列取得
    /// </summary>
    public async Task<string> GetDatabaseConnectionString()
    {
        var secretJson = await GetSecret("prod/database/connectionstring");
        var secret = JsonSerializer.Deserialize<DatabaseSecret>(secretJson);
        return secret?.ConnectionString ?? throw new InvalidOperationException("Invalid secret format");
    }
}

public class DatabaseSecret
{
    public string ConnectionString { get; set; } = string.Empty;
}
```

---

### 6.2 環境変数の安全な使用

```csharp
// ❌ NG: appsettings.json にパスワードを保存
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mydb;Username=admin;Password=password123"
  }
}

// ✅ OK: 環境変数または AWS Secrets Manager から取得
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "${DATABASE_CONNECTION_STRING}"
  }
}

// Program.cs
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? await secretsService.GetDatabaseConnectionString();
```

---

### 6.3 ログでの秘密情報マスキング

```csharp
/// <summary>
/// ログ出力時に秘密情報をマスキング
/// </summary>
public class SecretMaskingLogger : ILogger
{
    private readonly ILogger _innerLogger;
    private readonly string[] _secretPatterns = new[]
    {
        "password", "secret", "token", "apikey", "connectionstring"
    };

    public SecretMaskingLogger(ILogger innerLogger)
    {
        _innerLogger = innerLogger;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var maskedMessage = MaskSecrets(message);

        _innerLogger.Log(logLevel, eventId, maskedMessage, exception, (s, e) => s.ToString());
    }

    private string MaskSecrets(string message)
    {
        foreach (var pattern in _secretPatterns)
        {
            // "password=abc123" → "password=***"
            var regex = new Regex($@"{pattern}=[^;&\s]+", RegexOptions.IgnoreCase);
            message = regex.Replace(message, $"{pattern}=***");
        }

        return message;
    }

    // その他のILoggerメソッド実装...
}
```

---

## 7. HTTPSとセキュアヘッダー

### 7.1 HTTPS リダイレクト

```csharp
// Program.cs
app.UseHttpsRedirection();

// HSTS (HTTP Strict Transport Security)
app.UseHsts();

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});
```

---

### 7.2 セキュリティヘッダー

```csharp
// Program.cs - セキュリティヘッダーミドルウェア
app.Use(async (context, next) =>
{
    // XSS Protection
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

    // Content Type Options
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

    // Frame Options
    context.Response.Headers.Add("X-Frame-Options", "DENY");

    // Referrer Policy
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

    // Permissions Policy
    context.Response.Headers.Add("Permissions-Policy",
        "geolocation=(), microphone=(), camera=()");

    await next();
});
```

---

## 8. レート制限

### 8.1 AspNetCoreRateLimit を使用

```csharp
using AspNetCoreRateLimit;

// Program.cs
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 100  // 1分間に100リクエストまで
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/*",
            Period = "1m",
            Limit = 20  // POST リクエストは1分間に20リクエストまで
        }
    };
});

builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

app.UseIpRateLimiting();
```

---

## 9. 入力検証

### 9.1 Data Annotations

```csharp
using System.ComponentModel.DataAnnotations;

/// <summary>
/// ユーザー作成リクエスト
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// ユーザー名（必須、2-50文字）
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// メールアドレス（必須、有効な形式）
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// パスワード（必須、8文字以上、英数字記号含む）
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must be at least 8 characters and contain uppercase, lowercase, number, and special character")]
    public string Password { get; set; } = string.Empty;
}
```

---

### 9.2 カスタムバリデーション

```csharp
using System.ComponentModel.DataAnnotations;

/// <summary>
/// 年齢検証（18歳以上）
/// </summary>
public class MinimumAgeAttribute : ValidationAttribute
{
    private readonly int _minimumAge;

    public MinimumAgeAttribute(int minimumAge)
    {
        _minimumAge = minimumAge;
        ErrorMessage = $"Must be at least {minimumAge} years old";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTime birthDate)
        {
            var age = DateTime.Today.Year - birthDate.Year;
            if (birthDate > DateTime.Today.AddYears(-age))
            {
                age--;
            }

            if (age >= _minimumAge)
            {
                return ValidationResult.Success;
            }
        }

        return new ValidationResult(ErrorMessage);
    }
}

// 使用例
public class UserProfileRequest
{
    [MinimumAge(18)]
    public DateTime BirthDate { get; set; }
}
```

---

## 10. セキュリティチェックリスト

### 10.1 認証・認可
- [ ] すべての保護されたエンドポイントに `[Authorize]` 属性を付与
- [ ] JWT トークンの有効期限を適切に設定（15分〜1時間）
- [ ] リフレッシュトークンを安全に保存
- [ ] パスワードポリシーを強制（8文字以上、英数字記号含む）

### 10.2 SQLインジェクション
- [ ] すべてのSQLクエリをパラメータ化
- [ ] 動的SQLはホワイトリスト検証を実施
- [ ] ORM（Entity Framework）を使用する場合も注意

### 10.3 XSS
- [ ] Razor View の自動エスケープを活用
- [ ] `Html.Raw()` の使用を最小限に
- [ ] ユーザー入力をHTMLレンダリングする場合はサニタイズ
- [ ] Content Security Policy (CSP) を設定

### 10.4 CSRF
- [ ] すべてのPOST/PUT/DELETEに `[ValidateAntiForgeryToken]` を付与
- [ ] SameSite Cookie を設定

### 10.5 秘密情報
- [ ] パスワード、API キーを appsettings.json に保存しない
- [ ] AWS Secrets Manager または環境変数を使用
- [ ] ログに秘密情報を出力しない

### 10.6 HTTPS
- [ ] 本番環境で HTTPS を強制
- [ ] HSTS を有効化
- [ ] セキュリティヘッダーを設定

### 10.7 レート制限
- [ ] API エンドポイントにレート制限を設定
- [ ] 認証エンドポイントに厳しいレート制限

### 10.8 入力検証
- [ ] すべてのユーザー入力を検証
- [ ] ホワイトリスト方式で検証
- [ ] ファイルアップロードのサイズ・形式を制限

---

## 11. 参考

- [エラーハンドリング設計](error-handling.md)
- [ログ設計](logging.md)
- [API設計規約](api-design.md)
- [OWASP Top 10 2021](https://owasp.org/Top10/)
- [ASP.NET Core セキュリティ](https://learn.microsoft.com/en-us/aspnet/core/security/)
