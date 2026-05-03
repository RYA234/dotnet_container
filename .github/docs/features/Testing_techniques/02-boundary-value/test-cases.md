# 境界値分析デモ - テストケース

## 文書情報
- **作成日**: 2026-05-03
- **最終更新**: 2026-05-03
- **バージョン**: 1.0
- **ステータス**: Draft

---

## 1. 単体テスト

### 1.1 BoundaryValueServiceTests

境界値分析では各境界点の「境界値-1 / 境界値 / 境界値+1」を全てテストする。

| No | テストケース名 | 入力(age) | 期待(category) | 境界 | 優先度 |
|----|-------------|-----------|--------------|------|--------|
| T-01 | ClassifyAge_マイナス1_エラー | -1 | エラー | 最小値境界-1 | 高 |
| T-02 | ClassifyAge_ゼロ_子供 | 0 | 子供 | 最小値境界 | 高 |
| T-03 | ClassifyAge_1_子供 | 1 | 子供 | 最小値境界+1 | 高 |
| T-04 | ClassifyAge_12_子供 | 12 | 子供 | 子供/一般境界-1 | 高 |
| T-05 | ClassifyAge_13_一般 | 13 | 一般 | 子供/一般境界 | 高 |
| T-06 | ClassifyAge_14_一般 | 14 | 一般 | 子供/一般境界+1 | 高 |
| T-07 | ClassifyAge_64_一般 | 64 | 一般 | 一般/シニア境界-1 | 高 |
| T-08 | ClassifyAge_65_シニア | 65 | シニア | 一般/シニア境界 | 高 |
| T-09 | ClassifyAge_66_シニア | 66 | シニア | 一般/シニア境界+1 | 高 |
| T-10 | ClassifyAge_null_エラー | null | エラー | - | 中 |
| T-11 | ClassifyAge_距離計算_境界上 | 13 | distanceToBoundary=0 | 子供/一般境界 | 中 |
| T-12 | RunBatchTest_9件全合格 | - | 9件 passed=true | 全境界 | 高 |

#### T-01〜T-03: 最小値境界

```csharp
[Theory]
[InlineData(-1, "エラー")]
[InlineData( 0, "子供")]
[InlineData( 1, "子供")]
public void ClassifyAge_最小値境界(int age, string expected)
{
    var service = new BoundaryValueService();
    var result = service.ClassifyAge(age);
    result.Category.Should().Be(expected);
}
```

#### T-04〜T-06: 子供/一般境界

```csharp
[Theory]
[InlineData(12, "子供")]
[InlineData(13, "一般")]
[InlineData(14, "一般")]
public void ClassifyAge_子供一般境界(int age, string expected)
{
    var service = new BoundaryValueService();
    var result = service.ClassifyAge(age);
    result.Category.Should().Be(expected);
}
```

#### T-07〜T-09: 一般/シニア境界

```csharp
[Theory]
[InlineData(64, "一般")]
[InlineData(65, "シニア")]
[InlineData(66, "シニア")]
public void ClassifyAge_一般シニア境界(int age, string expected)
{
    var service = new BoundaryValueService();
    var result = service.ClassifyAge(age);
    result.Category.Should().Be(expected);
}
```

#### T-11: 距離計算（境界上）

```csharp
[Fact]
public void ClassifyAge_境界上_距離0を返す()
{
    var service = new BoundaryValueService();
    var result = service.ClassifyAge(13);
    result.DistanceToBoundary.Should().Be(0);
    result.NearestBoundary.Should().Contain("13");
}
```

#### T-12: バッチテスト全合格

```csharp
[Fact]
public void RunBatchTest_9境界値が全合格する()
{
    var service = new BoundaryValueService();
    var results = service.RunBatchTest();
    results.Should().HaveCount(9);
    results.Should().AllSatisfy(r => r.Passed.Should().BeTrue());
}
```

---

## 2. 統合テスト

### 2.1 BoundaryValueControllerTests

| No | テストケース名 | HTTPメソッド | パス | 期待ステータス | 確認項目 |
|----|-------------|------------|------|--------------|---------|
| T-20 | Classify_境界上_距離0 | GET | /api/demo/testing/boundary?age=13 | 200 OK | distanceToBoundary=0 |
| T-21 | Classify_境界直前_子供 | GET | /api/demo/testing/boundary?age=12 | 200 OK | category=子供 |
| T-22 | Classify_負数_エラー | GET | /api/demo/testing/boundary?age=-1 | 200 OK | isValid=false |
| T-23 | BatchTest_9件全合格 | POST | /api/demo/testing/boundary/batch | 200 OK | 9件全passed=true |

---

## 3. E2Eテスト

| No | テストケース名 | 概要 |
|----|-------------|------|
| T-30 | 画面表示_正常系 | 境界値分析デモ画面が表示される |
| T-31 | 境界値入力_距離表示 | age=13を入力→境界値13, 距離0が表示される |
| T-32 | 境界外入力_距離表示 | age=10を入力→境界値0か13に対する距離が表示される |
| T-33 | バッチ実行_9件全合格 | 境界値テストケース自動生成→9件全て✅ |

---

## 4. テストカバレッジ

| 項目 | 目標値 |
|------|--------|
| 行カバレッジ | 80%以上 |
| メソッドカバレッジ | 90%以上 |
| 境界値網羅率 | 100%（3境界×3点=9点全て） |

---

## 5. 参考

- [外部設計書](external-design.md)
- [内部設計書](internal-design.md)
