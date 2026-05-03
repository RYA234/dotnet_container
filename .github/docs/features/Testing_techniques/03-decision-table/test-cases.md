# デシジョンテーブルデモ - テストケース

## 文書情報
- **作成日**: 2026-05-03
- **最終更新**: 2026-05-03
- **バージョン**: 1.0
- **ステータス**: Draft

---

## 1. 単体テスト

### 1.1 DecisionTableServiceTests

デシジョンテーブルの全6ケースを網羅する。

| No | テストケース名 | Gold | 1万以上 | クーポン | 期待割引率 | ケースNo | 優先度 |
|----|-------------|------|--------|---------|----------|---------|--------|
| T-01 | C1_Gold_1万以上_クーポンあり_20% | Y | Y | Y | 20% | C1 | 高 |
| T-02 | C2_Gold_1万以上_クーポンなし_15% | Y | Y | N | 15% | C2 | 高 |
| T-03 | C3_Gold_1万未満_10% | Y | N | - | 10% | C3 | 高 |
| T-04 | C4_非Gold_1万以上_クーポンあり_15% | N | Y | Y | 15% | C4 | 高 |
| T-05 | C5_非Gold_1万以上_クーポンなし_5% | N | Y | N | 5% | C5 | 高 |
| T-06 | C6_非Gold_1万未満_0% | N | N | - | 0% | C6 | 高 |
| T-07 | C3_Gold_1万未満_クーポンあり_10% | Y | N | Y | 10% | C3 | 中 |
| T-08 | C6_非Gold_1万未満_クーポンあり_0% | N | N | Y | 0% | C6 | 中 |
| T-09 | 割引額計算_C1_15000円_3000円引き | Y | Y | Y | 3000円割引 | C1 | 高 |
| T-10 | RunBatchTest_6件全合格 | - | - | - | 6件 passed=true | 全C | 高 |

#### T-01: C1 Gold+1万以上+クーポンあり → 20%

```csharp
[Fact]
public void CalculateDiscount_C1_Gold1万以上クーポンあり_20%()
{
    var service = new DecisionTableService();
    var request = new DiscountRequest
    {
        MemberRank = "Gold",
        PurchaseAmount = 15000,
        HasCoupon = true
    };

    var result = service.CalculateDiscount(request);

    result.DiscountRate.Should().Be(20);
    result.MatchedCase.Should().Be(1);
    result.DiscountAmount.Should().Be(3000);
    result.FinalAmount.Should().Be(12000);
}
```

#### T-06: C6 非Gold+1万未満 → 0%

```csharp
[Fact]
public void CalculateDiscount_C6_非Gold1万未満_0%()
{
    var service = new DecisionTableService();
    var request = new DiscountRequest
    {
        MemberRank = "一般",
        PurchaseAmount = 5000,
        HasCoupon = false
    };

    var result = service.CalculateDiscount(request);

    result.DiscountRate.Should().Be(0);
    result.MatchedCase.Should().Be(6);
    result.DiscountAmount.Should().Be(0);
    result.FinalAmount.Should().Be(5000);
}
```

#### T-07,T-08: C3/C6のクーポン条件無関係を確認

```csharp
[Theory]
[InlineData("Gold",  5000, true,  10)]  // C3: クーポンあり
[InlineData("Gold",  5000, false, 10)]  // C3: クーポンなし
[InlineData("一般", 5000, true,   0)]  // C6: クーポンあり
[InlineData("一般", 5000, false,  0)]  // C6: クーポンなし
public void CalculateDiscount_1万未満はクーポン無関係(
    string rank, decimal amount, bool coupon, int expected)
{
    var service = new DecisionTableService();
    var result = service.CalculateDiscount(new DiscountRequest
    {
        MemberRank = rank,
        PurchaseAmount = amount,
        HasCoupon = coupon
    });
    result.DiscountRate.Should().Be(expected);
}
```

#### T-10: バッチテスト全合格

```csharp
[Fact]
public void RunBatchTest_6ケース全合格する()
{
    var service = new DecisionTableService();
    var results = service.RunBatchTest();
    results.Should().HaveCount(6);
    results.Should().AllSatisfy(r => r.Passed.Should().BeTrue());
}
```

---

## 2. 統合テスト

### 2.1 DecisionTableControllerTests

| No | テストケース名 | HTTPメソッド | パス | Body | 期待ステータス | 確認項目 |
|----|-------------|------------|------|------|--------------|---------|
| T-20 | Calculate_C1_200OK | POST | /api/demo/testing/decision | Gold,15000,true | 200 OK | discountRate=20 |
| T-21 | Calculate_C6_200OK | POST | /api/demo/testing/decision | 一般,5000,false | 200 OK | discountRate=0 |
| T-22 | Calculate_負金額_400 | POST | /api/demo/testing/decision | Gold,-100,false | 400 BadRequest | エラーメッセージ |
| T-23 | BatchTest_6件全合格 | POST | /api/demo/testing/decision/batch | なし | 200 OK | 6件全passed=true |

---

## 3. E2Eテスト

| No | テストケース名 | 概要 |
|----|-------------|------|
| T-30 | 画面表示_正常系 | デシジョンテーブルデモ画面が表示される |
| T-31 | C1入力_20%表示 | Gold+15000+クーポンあり→20%が表示される |
| T-32 | C6入力_0%表示 | 一般+5000+クーポンなし→0%が表示される |
| T-33 | 全ケース自動実行 | 全ケース自動実行ボタン→6件全て✅ |

---

## 4. テストカバレッジ

| 項目 | 目標値 |
|------|--------|
| 行カバレッジ | 80%以上 |
| メソッドカバレッジ | 90%以上 |
| デシジョンテーブル網羅率 | 100%（C1〜C6全ケース） |

---

## 5. 参考

- [外部設計書](external-design.md)
- [内部設計書](internal-design.md)
