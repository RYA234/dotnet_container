# パフォーマンスチューニング

## 概要
アプリケーションのパフォーマンスを最適化する方法を説明します。

## パフォーマンス計測

### 現状の把握

#### レスポンスタイム測定
```bash
# ALBのレスポンスタイム
aws cloudwatch get-metric-statistics \
  --namespace AWS/ApplicationELB \
  --metric-name TargetResponseTime \
  --dimensions Name=LoadBalancer,Value=<load-balancer-full-name> \
  --start-time $(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 3600 \
  --statistics Average,Maximum,Minimum \
  --region ap-northeast-1

# エンドポイント別のレスポンスタイム（Logs Insights）
# fields @timestamp, path, duration
# | stats avg(duration), max(duration), count() by path
# | sort avg(duration) desc
```

#### リソース使用状況
```bash
# CPU使用率
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name CPUUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 3600 \
  --statistics Average,Maximum \
  --region ap-northeast-1

# メモリ使用率
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name MemoryUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 3600 \
  --statistics Average,Maximum \
  --region ap-northeast-1
```

---

## データベース最適化

### N+1問題の解決

#### 問題のあるコード
```csharp
// N+1問題: 各ユーザーごとにクエリが実行される
var users = context.Users.ToList();
foreach (var user in users)
{
    var orders = user.Orders.ToList(); // N回のクエリ
}
```

#### 改善されたコード
```csharp
// Eager Loading: 1回のクエリで全て取得
var users = context.Users
    .Include(u => u.Orders)
    .ToList();
```

### インデックスの追加

```sql
-- よく検索されるカラムにインデックス
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_orders_user_id ON orders(user_id);
CREATE INDEX idx_orders_created_at ON orders(created_at);

-- 複合インデックス
CREATE INDEX idx_orders_user_created ON orders(user_id, created_at);
```

### クエリの最適化

```csharp
// 悪い例: 全カラムを取得
var users = context.Users.ToList();

// 良い例: 必要なカラムのみ取得
var users = context.Users
    .Select(u => new { u.Id, u.Name, u.Email })
    .ToList();

// ページネーション
var users = context.Users
    .OrderBy(u => u.Id)
    .Skip(page * pageSize)
    .Take(pageSize)
    .ToList();
```

### 接続プールの設定

```csharp
// 接続文字列に pooling parameters を追加
var connectionString =
    "Host=...;Database=...;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;Connection Lifetime=0";

services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

---

## キャッシュの活用

### メモリキャッシュ

```csharp
// Startup.cs / Program.cs
services.AddMemoryCache();

// Controller
public class ProductController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _context;

    public ProductController(IMemoryCache cache, AppDbContext context)
    {
        _cache = cache;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var cacheKey = "all_products";

        if (!_cache.TryGetValue(cacheKey, out List<Product> products))
        {
            products = await _context.Products.ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

            _cache.Set(cacheKey, products, cacheOptions);
        }

        return Ok(products);
    }
}
```

### 分散キャッシュ（Redis）

```csharp
// Startup.cs / Program.cs
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis-host:6379";
    options.InstanceName = "DotNetApp_";
});

// Service
public class CacheService
{
    private readonly IDistributedCache _cache;

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null)
    {
        var cached = await _cache.GetStringAsync(key);

        if (cached != null)
        {
            return JsonSerializer.Deserialize<T>(cached);
        }

        var value = await factory();

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
        };

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(value),
            options);

        return value;
    }
}
```

### レスポンスキャッシュ

```csharp
// Startup.cs / Program.cs
services.AddResponseCaching();
app.UseResponseCaching();

// Controller
[HttpGet]
[ResponseCache(Duration = 300)] // 5分間キャッシュ
public IActionResult GetStaticData()
{
    return Ok(data);
}
```

---

## 非同期処理

### 同期→非同期への変換

```csharp
// 悪い例: 同期処理
public IActionResult GetData()
{
    var data = _service.GetData(); // ブロッキング
    return Ok(data);
}

// 良い例: 非同期処理
public async Task<IActionResult> GetDataAsync()
{
    var data = await _service.GetDataAsync(); // 非ブロッキング
    return Ok(data);
}
```

### 並列処理

```csharp
// 悪い例: 順次実行
var user = await _userService.GetUserAsync(userId);
var orders = await _orderService.GetOrdersAsync(userId);
var profile = await _profileService.GetProfileAsync(userId);

// 良い例: 並列実行
var userTask = _userService.GetUserAsync(userId);
var ordersTask = _orderService.GetOrdersAsync(userId);
var profileTask = _profileService.GetProfileAsync(userId);

await Task.WhenAll(userTask, ordersTask, profileTask);

var user = await userTask;
var orders = await ordersTask;
var profile = await profileTask;
```

### バックグラウンドタスク

```csharp
// 重い処理をバックグラウンドで実行
public class EmailService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // メール送信キューから取得して送信
            await ProcessEmailQueueAsync();

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}

// Startup.cs
services.AddHostedService<EmailService>();
```

---

## HTTPクライアントの最適化

### HttpClientFactory の使用

```csharp
// Startup.cs
services.AddHttpClient("ExternalAPI", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "DotNetApp");
});

// Service
public class ExternalApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetDataAsync()
    {
        var client = _httpClientFactory.CreateClient("ExternalAPI");
        var response = await client.GetAsync("/data");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
```

### タイムアウトとリトライ

```csharp
// Polly を使用したリトライポリシー
using Polly;
using Polly.Extensions.Http;

// Startup.cs
services.AddHttpClient("ExternalAPI")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
```

---

## リソース最適化

### CPU/メモリの増強

```json
// タスク定義
{
  "family": "dotnet-task",
  "cpu": "512",      // 256 → 512 (0.5 vCPU)
  "memory": "1024",  // 512 → 1024 MB

  // または、さらに増やす
  "cpu": "1024",     // 1 vCPU
  "memory": "2048"   // 2 GB
}
```

### 水平スケーリング

```bash
# タスク数を増やす
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --desired-count 2 \
  --region ap-northeast-1

# Auto Scaling の設定
aws application-autoscaling register-scalable-target \
  --service-namespace ecs \
  --resource-id service/app-cluster/dotnet-service \
  --scalable-dimension ecs:service:DesiredCount \
  --min-capacity 1 \
  --max-capacity 4 \
  --region ap-northeast-1

# CPU使用率に基づくスケーリングポリシー
aws application-autoscaling put-scaling-policy \
  --service-namespace ecs \
  --resource-id service/app-cluster/dotnet-service \
  --scalable-dimension ecs:service:DesiredCount \
  --policy-name cpu-scaling-policy \
  --policy-type TargetTrackingScaling \
  --target-tracking-scaling-policy-configuration file://scaling-policy.json \
  --region ap-northeast-1
```

---

## コード最適化

### LINQ クエリの最適化

```csharp
// 悪い例: 複数回の列挙
var users = context.Users.Where(u => u.IsActive).ToList();
var count = users.Count();
var firstUser = users.FirstOrDefault();

// 良い例: 1回の列挙
var users = context.Users.Where(u => u.IsActive);
var count = await users.CountAsync();
var firstUser = await users.FirstOrDefaultAsync();
```

### 文字列操作の最適化

```csharp
// 悪い例: 文字列の連結
string result = "";
for (int i = 0; i < 1000; i++)
{
    result += i.ToString();
}

// 良い例: StringBuilder
var sb = new StringBuilder();
for (int i = 0; i < 1000; i++)
{
    sb.Append(i);
}
var result = sb.ToString();
```

### オブジェクトのプーリング

```csharp
using System.Buffers;

// ArrayPool を使用してメモリ割り当てを削減
var pool = ArrayPool<byte>.Shared;
byte[] buffer = pool.Rent(1024);
try
{
    // buffer を使用
}
finally
{
    pool.Return(buffer);
}
```

---

## 圧縮の有効化

### Gzip 圧縮

```csharp
// Program.cs
using Microsoft.AspNetCore.ResponseCompression;

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

var app = builder.Build();
app.UseResponseCompression();
```

---

## パフォーマンステスト

### Apache Bench (ab)

```bash
# 100リクエスト、同時接続10
ab -n 100 -c 10 https://rya234.com/dotnet/api/products

# 結果の確認
# Requests per second:    XXX [#/sec] (mean)
# Time per request:       XXX [ms] (mean)
```

### wrk

```bash
# 30秒間、10スレッド、100接続
wrk -t10 -c100 -d30s https://rya234.com/dotnet/api/products
```

### JMeter

GUIベースの負荷テストツール。複雑なシナリオのテストに適している。

---

## 監視とプロファイリング

### Application Insights

```csharp
// Application Insights を追加（本番環境推奨）
services.AddApplicationInsightsTelemetry();
```

### dotTrace / dotMemory

JetBrains のプロファイリングツールでメモリリーク検出。

### MiniProfiler

```csharp
// 開発環境でのプロファイリング
services.AddMiniProfiler(options =>
{
    options.RouteBasePath = "/profiler";
}).AddEntityFramework();

app.UseMiniProfiler();
```

---

## チェックリスト

### パフォーマンス改善チェックリスト

- [ ] データベースクエリの最適化（N+1問題の解消）
- [ ] インデックスの追加
- [ ] キャッシュの導入
- [ ] 非同期処理への変換
- [ ] HTTPクライアントの最適化
- [ ] 圧縮の有効化
- [ ] 不要なログの削減
- [ ] リソースの最適化（CPU/メモリ）
- [ ] 水平スケーリングの検討
- [ ] CDNの導入（静的ファイル）

---

## 関連ドキュメント

- [よくある問題](common-issues.md)
- [ログ解析ガイド](log-analysis.md)
- [メトリクス監視](../monitoring/metrics.md)

---

**最終更新日**: 2025-12-17
