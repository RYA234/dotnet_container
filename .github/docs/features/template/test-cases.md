# [機能名] - テストケース

## 文書情報
- **作成日**: YYYY-MM-DD
- **最終更新**: YYYY-MM-DD
- **バージョン**: 1.0
- **ステータス**: Draft

## 変更履歴
| 日付 | バージョン | 変更者 | 変更内容 |
|------|----------|--------|---------|
| YYYY-MM-DD | 1.0 | - | 初版作成 |

---

## 1. 単体テスト

### 1.1 FeatureServiceTests

#### テストケース一覧

| No | テストケース名 | 入力 | 期待結果 | 優先度 |
|----|-------------|------|---------|--------|
| T-01 | DoSomething_正常系 | 正常なリクエスト | Resultが"success" | 高 |
| T-02 | DoSomething_異常系_NullInput | null | ArgumentNullException | 高 |
| T-03 | DoSomething_境界値_最小値 | 最小値 | 正常処理 | 中 |
| T-04 | DoSomething_境界値_最大値 | 最大値 | 正常処理 | 中 |

---

#### T-01: DoSomething_正常系

**目的**: 正常系の動作確認

**前提条件**:
- データベースに初期データが存在

**テストデータ**:
```csharp
var request = new Request
{
    Param1 = "value1",
    Param2 = 123
};
```

**実装例**:
```csharp
[Fact]
public async Task DoSomething_正常系_Resultがsuccessである()
{
    // Arrange
    var request = new Request { Param1 = "value1", Param2 = 123 };

    // Act
    var result = await _service.DoSomething(request);

    // Assert
    Assert.Equal("success", result.Result);
}
```

**期待結果**:
- `result.Result` が "success"
- `result.Data` が null でない

---

#### T-02: DoSomething_異常系_NullInput

**目的**: null入力時の例外処理確認

**テストデータ**:
```csharp
Request request = null;
```

**実装例**:
```csharp
[Fact]
public async Task DoSomething_異常系_NullInput_ArgumentNullExceptionがスローされる()
{
    // Arrange
    Request request = null;

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(async () =>
    {
        await _service.DoSomething(request);
    });
}
```

**期待結果**:
- `ArgumentNullException` がスローされる

---

## 2. 統合テスト

### 2.1 FeatureControllerTests

#### テストケース一覧

| No | テストケース名 | HTTPメソッド | パス | 期待ステータス |
|----|-------------|------------|------|--------------|
| T-10 | Action_正常系 | GET | /api/feature/action | 200 OK |
| T-11 | Action_異常系_InvalidInput | GET | /api/feature/action?param=invalid | 400 Bad Request |
| T-12 | Action_異常系_NotFound | GET | /api/feature/action?id=9999 | 404 Not Found |

---

#### T-10: Action_正常系

**実装例**:
```csharp
[Fact]
public async Task Action_正常系_200OKが返る()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/feature/action?param1=value1&param2=123");

    // Assert
    response.EnsureSuccessStatusCode();
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<Response>(content);
    Assert.Equal("success", result.Result);
}
```

---

## 3. E2Eテスト

### 3.1 Playwright テスト

#### テストケース一覧

| No | テストケース名 | 概要 |
|----|-------------|------|
| T-20 | 画面表示_正常系 | 画面が正常に表示される |
| T-21 | ボタンクリック_正常系 | ボタンクリックで結果が表示される |

---

#### T-20: 画面表示_正常系

**実装例**:
```typescript
test('画面表示_正常系', async ({ page }) => {
  // Arrange & Act
  await page.goto('https://rya234.com/dotnet/Feature/Index');

  // Assert
  await expect(page.locator('h1')).toHaveText('[機能名]');
  await expect(page.locator('#input-field')).toBeVisible();
  await expect(page.locator('#submit-button')).toBeVisible();
});
```

---

#### T-21: ボタンクリック_正常系

**実装例**:
```typescript
test('ボタンクリック_正常系', async ({ page }) => {
  // Arrange
  await page.goto('https://rya234.com/dotnet/Feature/Index');

  // Act
  await page.fill('#input-field', 'value1');
  await page.click('#submit-button');

  // Assert
  await page.waitForSelector('#result');
  const result = await page.textContent('#result');
  expect(result).toContain('"result":"success"');
});
```

---

## 4. パフォーマンステスト

### 4.1 ベンチマーク

**実装例**:
```csharp
[MemoryDiagnoser]
public class FeatureBenchmark
{
    private FeatureService _service;

    [GlobalSetup]
    public void Setup()
    {
        _service = new FeatureService(...);
    }

    [Benchmark]
    public async Task DoSomething_Benchmark()
    {
        await _service.DoSomething(new Request { ... });
    }
}
```

**目標値**:

| 指標 | 目標値 |
|------|--------|
| 平均実行時間 | < 100ms |
| メモリ使用量 | < 10MB |
| スループット | > 100 req/s |

---

## 5. セキュリティテスト

### 5.1 SQLインジェクションテスト

**テストケース**:
```csharp
[Theory]
[InlineData("1' OR '1'='1")]
[InlineData("1; DROP TABLE Users--")]
public async Task DoSomething_SQLインジェクション_安全に処理される(string maliciousInput)
{
    // Arrange
    var request = new Request { Param1 = maliciousInput };

    // Act
    var result = await _service.DoSomething(request);

    // Assert
    // パラメータ化クエリにより、SQLインジェクションが防がれる
    Assert.NotNull(result);
}
```

---

## 6. テストカバレッジ

### 6.1 目標値

| 項目 | 目標値 |
|------|--------|
| 行カバレッジ | 80%以上 |
| 分岐カバレッジ | 70%以上 |
| メソッドカバレッジ | 90%以上 |

### 6.2 測定方法

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage
```

---

## 7. 参考

- [外部設計書](external-design.md)
- [内部設計書](internal-design.md)
