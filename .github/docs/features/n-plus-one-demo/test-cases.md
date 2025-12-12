# N+1問題デモ - テストケース

## 文書情報
- **作成日**: 2025-12-10
- **最終更新**: 2025-12-10
- **バージョン**: 1.0
- **ステータス**: Draft（テスト未実装）

---

## 1. 単体テスト

### 1.1 NPlusOneServiceTests

#### テストケース一覧

| No | テストケース名 | 入力 | 期待結果 | 優先度 |
|----|-------------|------|---------|--------|
| T-01 | GetUsersBad_正常系 | なし | SqlCount = 101 | 高 |
| T-02 | GetUsersGood_正常系 | なし | SqlCount = 1 | 高 |
| T-03 | GetUsersBad_性能比較 | なし | ExecutionTimeMs > GetUsersGood | 高 |
| T-04 | GetUsersBad_データ件数 | なし | RowCount = 100 | 中 |
| T-05 | GetUsersGood_データ件数 | なし | RowCount = 100 | 中 |
| T-06 | GetUsersBad_データサイズ | なし | DataSize > 0 | 低 |

---

#### T-01: GetUsersBad_正常系

**目的**: N+1問題版が101回のクエリを実行することを確認

**実装例**:
```csharp
[Fact]
public async Task GetUsersBad_正常系_SqlCountが101である()
{
    // Arrange
    var service = CreateService();

    // Act
    var result = await service.GetUsersBad();

    // Assert
    Assert.Equal(101, result.SqlCount);
}
```

**期待結果**:
- `result.SqlCount == 101`
- `result.RowCount == 100`
- `result.ExecutionTimeMs > 0`

---

#### T-02: GetUsersGood_正常系

**目的**: 最適化版が1回のクエリで取得することを確認

**実装例**:
```csharp
[Fact]
public async Task GetUsersGood_正常系_SqlCountが1である()
{
    // Arrange
    var service = CreateService();

    // Act
    var result = await service.GetUsersGood();

    // Assert
    Assert.Equal(1, result.SqlCount);
}
```

**期待結果**:
- `result.SqlCount == 1`
- `result.RowCount == 100`
- `result.ExecutionTimeMs > 0`

---

#### T-03: GetUsersBad_性能比較

**目的**: Bad版がGood版より遅いことを確認

**実装例**:
```csharp
[Fact]
public async Task GetUsersBad_性能比較_BadがGoodより遅い()
{
    // Arrange
    var service = CreateService();

    // Act
    var badResult = await service.GetUsersBad();
    var goodResult = await service.GetUsersGood();

    // Assert
    Assert.True(badResult.ExecutionTimeMs > goodResult.ExecutionTimeMs,
        $"Bad: {badResult.ExecutionTimeMs}ms, Good: {goodResult.ExecutionTimeMs}ms");
}
```

**期待結果**:
- `badResult.ExecutionTimeMs > goodResult.ExecutionTimeMs`
- 目安: Bad版は45ms、Good版は12ms程度

---

#### T-04: GetUsersBad_データ件数

**目的**: 100件のユーザーデータを取得することを確認

**実装例**:
```csharp
[Fact]
public async Task GetUsersBad_データ件数_100件である()
{
    // Arrange
    var service = CreateService();

    // Act
    var result = await service.GetUsersBad();

    // Assert
    Assert.Equal(100, result.RowCount);
    Assert.Equal(100, result.Data.Count);
}
```

---

## 2. 統合テスト

### 2.1 DemoControllerTests

#### テストケース一覧

| No | テストケース名 | HTTPメソッド | パス | 期待ステータス |
|----|-------------|------------|------|--------------|
| T-10 | NPlusOneBad_正常系 | GET | /api/demo/n-plus-one/bad | 200 OK |
| T-11 | NPlusOneGood_正常系 | GET | /api/demo/n-plus-one/good | 200 OK |
| T-12 | NPlusOneBad_レスポンス形式 | GET | /api/demo/n-plus-one/bad | JSON形式 |

---

#### T-10: NPlusOneBad_正常系

**実装例**:
```csharp
[Fact]
public async Task NPlusOneBad_正常系_200OKが返る()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/demo/n-plus-one/bad");

    // Assert
    response.EnsureSuccessStatusCode();
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<NPlusOneResponse>(content);
    Assert.Equal(101, result.SqlCount);
}
```

---

## 3. E2Eテスト

### 3.1 Playwright テスト

#### テストケース一覧

| No | テストケース名 | 概要 |
|----|-------------|------|
| T-20 | 画面表示_正常系 | デモ画面が正常に表示される |
| T-21 | Bad版実行_正常系 | Bad版ボタンクリックで結果が表示される |
| T-22 | Good版実行_正常系 | Good版ボタンクリックで結果が表示される |

---

#### T-20: 画面表示_正常系

**実装例**:
```typescript
test('画面表示_正常系', async ({ page }) => {
  // Arrange & Act
  await page.goto('https://rya234.com/dotnet/Demo/Performance');

  // Assert
  await expect(page.locator('h1')).toHaveText('SQLパフォーマンスデモ');
  await expect(page.locator('#run-bad-button')).toBeVisible();
  await expect(page.locator('#run-good-button')).toBeVisible();
});
```

---

#### T-21: Bad版実行_正常系

**実装例**:
```typescript
test('Bad版実行_正常系', async ({ page }) => {
  // Arrange
  await page.goto('https://rya234.com/dotnet/Demo/Performance');

  // Act
  await page.click('#run-bad-button');

  // Assert
  await page.waitForSelector('#result-bad');
  const result = await page.textContent('#result-bad');
  expect(result).toContain('"sqlCount":101');
  expect(result).toContain('N+1問題あり');
});
```

---

#### T-22: Good版実行_正常系

**実装例**:
```typescript
test('Good版実行_正常系', async ({ page }) => {
  // Arrange
  await page.goto('https://rya234.com/dotnet/Demo/Performance');

  // Act
  await page.click('#run-good-button');

  // Assert
  await page.waitForSelector('#result-good');
  const result = await page.textContent('#result-good');
  expect(result).toContain('"sqlCount":1');
  expect(result).toContain('最適化済み');
});
```

---

## 4. パフォーマンステスト

### 4.1 ベンチマーク

**実装例**:
```csharp
[MemoryDiagnoser]
public class NPlusOneBenchmark
{
    private NPlusOneService _service;

    [GlobalSetup]
    public void Setup()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"ConnectionStrings:DemoDatabase", "Data Source=demo.db"}
            })
            .Build();

        var logger = new Mock<ILogger<NPlusOneService>>().Object;
        _service = new NPlusOneService(configuration, logger);
    }

    [Benchmark]
    public async Task GetUsersBad_Benchmark()
    {
        await _service.GetUsersBad();
    }

    [Benchmark]
    public async Task GetUsersGood_Benchmark()
    {
        await _service.GetUsersGood();
    }
}
```

**目標値**:

| メソッド | 平均実行時間 | メモリ使用量 | クエリ回数 |
|---------|------------|------------|----------|
| GetUsersBad | 45ms | 512KB | 101回 |
| GetUsersGood | 12ms | 256KB | 1回 |

---

## 5. テストカバレッジ

### 5.1 目標値

| 項目 | 目標値 |
|------|--------|
| 行カバレッジ | 80%以上 |
| 分岐カバレッジ | 70%以上 |
| メソッドカバレッジ | 90%以上 |

### 5.2 測定方法

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage
```

---

## 6. 参考

- [外部設計書](external-design.md)
- [内部設計書](internal-design.md)
- [全体テスト設計](../../internal-design/logging-testing.md)
