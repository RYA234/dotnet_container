# 同値分割デモ - テストケース

## 文書情報
- **作成日**: 2026-05-03
- **最終更新**: 2026-05-03
- **バージョン**: 1.0
- **ステータス**: Draft

---

## 1. 単体テスト

### 1.1 EquivalencePartitioningServiceTests

| No | テストケース名 | 入力(age) | 期待結果(category) | 対象同値クラス | 優先度 |
|----|-------------|-----------|-----------------|--------------|--------|
| T-01 | ClassifyAge_負数_エラー | -1 | エラー | 無効クラス（負） | 高 |
| T-02 | ClassifyAge_ゼロ_子供 | 0 | 子供 | 有効クラス（子供） | 高 |
| T-03 | ClassifyAge_子供代表値_子供 | 6 | 子供 | 有効クラス（子供） | 高 |
| T-04 | ClassifyAge_一般代表値_一般 | 30 | 一般 | 有効クラス（一般） | 高 |
| T-05 | ClassifyAge_シニア代表値_シニア | 70 | シニア | 有効クラス（シニア） | 高 |
| T-06 | ClassifyAge_null_エラー | null | エラー | 無効クラス（非数値） | 高 |
| T-07 | ClassifyAge_大きい値_シニア | 100 | シニア | 有効クラス（シニア） | 中 |
| T-08 | RunBatchTest_全ケース合格 | - | 全4件 passed=true | 全同値クラス | 高 |

#### T-01: 負数入力

```csharp
[Fact]
public void ClassifyAge_負数_エラーを返す()
{
    // Arrange
    var service = new EquivalencePartitioningService();

    // Act
    var result = service.ClassifyAge(-1);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Category.Should().Be("エラー");
    result.EquivalenceClass.Should().Contain("無効クラス（負）");
}
```

#### T-03: 子供代表値

```csharp
[Theory]
[InlineData(0,  "子供")]
[InlineData(6,  "子供")]
[InlineData(12, "子供")]
public void ClassifyAge_子供範囲_子供を返す(int age, string expected)
{
    var service = new EquivalencePartitioningService();
    var result = service.ClassifyAge(age);
    result.Category.Should().Be(expected);
    result.IsValid.Should().BeTrue();
}
```

#### T-08: バッチテスト全合格

```csharp
[Fact]
public void RunBatchTest_全代表値が合格する()
{
    var service = new EquivalencePartitioningService();
    var results = service.RunBatchTest();
    results.Should().HaveCount(4);
    results.Should().AllSatisfy(r => r.Passed.Should().BeTrue());
}
```

---

## 2. 統合テスト

### 2.1 EquivalencePartitioningControllerTests

| No | テストケース名 | HTTPメソッド | パス | 期待ステータス | 備考 |
|----|-------------|------------|------|--------------|------|
| T-10 | Classify_子供_200OK | GET | /api/demo/testing/equivalence?age=6 | 200 OK | category=子供 |
| T-11 | Classify_一般_200OK | GET | /api/demo/testing/equivalence?age=30 | 200 OK | category=一般 |
| T-12 | Classify_シニア_200OK | GET | /api/demo/testing/equivalence?age=70 | 200 OK | category=シニア |
| T-13 | Classify_負数_200OK | GET | /api/demo/testing/equivalence?age=-1 | 200 OK | isValid=false |
| T-14 | Classify_パラメータなし_200OK | GET | /api/demo/testing/equivalence | 200 OK | isValid=false |
| T-15 | BatchTest_全件合格_200OK | POST | /api/demo/testing/equivalence/batch | 200 OK | 4件全passed=true |

---

## 3. E2Eテスト

| No | テストケース名 | 概要 |
|----|-------------|------|
| T-20 | 画面表示_正常系 | 同値分割デモ画面が表示される |
| T-21 | 手動入力_子供_判定結果表示 | age=6を入力→「子供」が表示される |
| T-22 | 手動入力_一般_判定結果表示 | age=30を入力→「一般」が表示される |
| T-23 | 手動入力_負数_エラー表示 | age=-1を入力→エラーが表示される |
| T-24 | バッチ実行_全ケース合格 | テストケース自動生成ボタン→4件全て✅ |

---

## 4. テストカバレッジ

| 項目 | 目標値 |
|------|--------|
| 行カバレッジ | 80%以上 |
| メソッドカバレッジ | 90%以上 |
| 同値クラス網羅率 | 100%（5クラス全て） |

---

## 5. 参考

- [外部設計書](external-design.md)
- [内部設計書](internal-design.md)
