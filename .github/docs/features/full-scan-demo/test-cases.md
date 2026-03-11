# フルテーブルスキャンデモ - テストケース

## 文書情報
- **作成日**: 2026-03-11
- **最終更新**: 2026-03-11
- **バージョン**: 1.0
- **ステータス**: Draft

---

## 1. 単体テスト

### 1.1 FullScanServiceTests

> **注意**: テストでは100万件ではなく1000件の小規模データを使用する

#### テストケース一覧

| No | テストケース名 | 入力 | 期待結果 | 優先度 |
|----|-------------|------|---------|--------|
| T-01 | SetupAsync_正常系 | なし | RowCount = 1000 | 高 |
| T-02 | SetupAsync_冪等性 | 2回呼び出し | 2回目もRowCount = 1000（重複なし） | 高 |
| T-03 | SearchWithoutIndex_ヒットあり | 存在するEmail | RowCount = 1, hasIndex = false | 高 |
| T-04 | SearchWithoutIndex_ヒットなし | 存在しないEmail | RowCount = 0, hasIndex = false | 高 |
| T-05 | CreateIndex_正常系 | なし | Success = true | 高 |
| T-06 | CreateIndex_冪等性 | 2回呼び出し | エラーにならない | 中 |
| T-07 | SearchWithIndex_ヒットあり | 存在するEmail | RowCount = 1, hasIndex = true | 高 |
| T-08 | SearchWithIndex_ヒットなし | 存在しないEmail | RowCount = 0, hasIndex = true | 高 |
| T-09 | WithIndexFasterThanWithout | 同じEmailで両方検索 | インデックスありの方がSQLCount等価で高速傾向 | 低 |

---

#### T-01: SetupAsync_正常系

**目的**: セットアップで指定件数のデータが生成されることを確認

**実装例**:
```csharp
[Fact]
public async Task SetupAsync_正常系_指定件数のデータが生成される()
{
    // Act
    var result = await _service.SetupAsync();

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RowCount.Should().Be(1000); // テスト用1000件
}
```

---

#### T-02: SetupAsync_冪等性

**目的**: 2回呼んでも重複データが入らないことを確認

**実装例**:
```csharp
[Fact]
public async Task SetupAsync_冪等性_2回呼んでも重複しない()
{
    // Act
    await _service.SetupAsync();
    var result = await _service.SetupAsync();

    // Assert
    result.RowCount.Should().Be(1000);
}
```

---

#### T-03: SearchWithoutIndex_ヒットあり

**目的**: 存在するEmailでフルスキャン検索できることを確認

**実装例**:
```csharp
[Fact]
public async Task SearchWithoutIndex_ヒットあり_1件返る()
{
    // Arrange
    await _service.SetupAsync();

    // Act
    var result = await _service.SearchWithoutIndexAsync("user0000500@example.com");

    // Assert
    result.RowCount.Should().Be(1);
    result.HasIndex.Should().BeFalse();
    result.Data.Should().HaveCount(1);
    result.Data[0].Email.Should().Be("user0000500@example.com");
    result.Message.Should().Contain("インデックスなし");
}
```

---

#### T-04: SearchWithoutIndex_ヒットなし

**目的**: 存在しないEmailで0件が返ることを確認

**実装例**:
```csharp
[Fact]
public async Task SearchWithoutIndex_ヒットなし_0件返る()
{
    // Arrange
    await _service.SetupAsync();

    // Act
    var result = await _service.SearchWithoutIndexAsync("notexist@example.com");

    // Assert
    result.RowCount.Should().Be(0);
    result.HasIndex.Should().BeFalse();
    result.Data.Should().BeEmpty();
}
```

---

#### T-05: CreateIndex_正常系

**目的**: インデックスが正常に作成されることを確認

**実装例**:
```csharp
[Fact]
public async Task CreateIndex_正常系_成功する()
{
    // Arrange
    await _service.SetupAsync();

    // Act
    var result = await _service.CreateIndexAsync();

    // Assert
    result.Success.Should().BeTrue();
    result.Message.Should().Contain("インデックス");
}
```

---

#### T-07: SearchWithIndex_ヒットあり

**目的**: インデックスあり検索で正しく1件返ることを確認

**実装例**:
```csharp
[Fact]
public async Task SearchWithIndex_ヒットあり_1件返る()
{
    // Arrange
    await _service.SetupAsync();
    await _service.CreateIndexAsync();

    // Act
    var result = await _service.SearchWithIndexAsync("user0000500@example.com");

    // Assert
    result.RowCount.Should().Be(1);
    result.HasIndex.Should().BeTrue();
    result.Data.Should().HaveCount(1);
    result.Message.Should().Contain("インデックスあり");
}
```

---

#### T-08: SearchWithIndex_ヒットなし

**目的**: インデックスあり検索で存在しないEmailに0件が返ることを確認

**実装例**:
```csharp
[Fact]
public async Task SearchWithIndex_ヒットなし_0件返る()
{
    // Arrange
    await _service.SetupAsync();
    await _service.CreateIndexAsync();

    // Act
    var result = await _service.SearchWithIndexAsync("notexist@example.com");

    // Assert
    result.RowCount.Should().Be(0);
    result.HasIndex.Should().BeTrue();
    result.Data.Should().BeEmpty();
}
```

---

## 2. 統合テスト

### 2.1 DemoControllerTests

#### テストケース一覧

| No | テストケース名 | HTTPメソッド | パス | 期待ステータス |
|----|-------------|------------|------|--------------|
| T-10 | Setup_正常系 | POST | /api/demo/full-scan/setup | 200 OK |
| T-11 | WithoutIndex_正常系 | GET | /api/demo/full-scan/without-index?email=xxx | 200 OK |
| T-12 | WithoutIndex_パラメータなし | GET | /api/demo/full-scan/without-index | 400 Bad Request |
| T-13 | CreateIndex_正常系 | POST | /api/demo/full-scan/create-index | 200 OK |
| T-14 | WithIndex_正常系 | GET | /api/demo/full-scan/with-index?email=xxx | 200 OK |

---

## 3. E2Eテスト

### 3.1 Playwright テスト

#### テストケース一覧

| No | テストケース名 | 概要 |
|----|-------------|------|
| T-20 | 画面表示_正常系 | フルスキャンデモ画面が表示される |
| T-21 | セットアップ実行_正常系 | セットアップボタンで100万件生成完了が表示される |
| T-22 | インデックスなし検索_正常系 | フルスキャン結果が表示される |
| T-23 | インデックス作成_正常系 | インデックス作成完了が表示される |
| T-24 | インデックスあり検索_正常系 | 高速検索結果が表示される |

---

## 4. パフォーマンステスト

### 4.1 目標値（100万件）

| 操作 | 期待実行時間 |
|------|------------|
| インデックスなし検索 | 数百ms〜数秒 |
| インデックスあり検索 | < 10ms |
| インデックス作成 | 数秒 |

---

## 5. テストカバレッジ

### 5.1 目標値

| 項目 | 目標値 |
|------|--------|
| 行カバレッジ | 80%以上 |
| 分岐カバレッジ | 70%以上 |
| メソッドカバレッジ | 90%以上 |

---

## 6. 参考

- [外部設計書](external-design.md)
- [内部設計書](internal-design.md)
- [全体テスト設計](../../common/logging-testing.md)
