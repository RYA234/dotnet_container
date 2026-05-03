# 状態遷移テストデモ - テストケース

## 文書情報
- **作成日**: 2026-05-03
- **最終更新**: 2026-05-03
- **バージョン**: 1.0
- **ステータス**: Draft

---

## 1. 単体テスト

### 1.1 StateTransitionServiceTests

#### 1.1.1 正常遷移（7遷移全カバー）

| No | テストケース名 | 初期状態 | イベント | 期待次状態 | 遷移ID | 優先度 |
|----|-------------|---------|---------|----------|--------|--------|
| T-01 | T1_注文受付→支払い処理→入金確認中 | S1 | 支払い処理 | S2 | T1 | 高 |
| T-02 | T2_入金確認中→入金OK→出荷準備中 | S2 | 入金確認OK | S3 | T2 | 高 |
| T-03 | T3_入金確認中→入金NG→キャンセル | S2 | 入金確認NG | S6 | T3 | 高 |
| T-04 | T4_出荷準備中→出荷完了→出荷済み | S3 | 出荷完了 | S4 | T4 | 高 |
| T-05 | T5_出荷済み→受取確認→完了 | S4 | 受取確認 | S5 | T5 | 高 |
| T-06 | T6_注文受付→キャンセル | S1 | キャンセル | S6 | T6 | 高 |
| T-07 | T7_出荷準備中→キャンセル | S3 | キャンセル | S6 | T7 | 高 |

#### 1.1.2 無効遷移（異常系）

| No | テストケース名 | 初期状態 | イベント | 期待結果 | 優先度 |
|----|-------------|---------|---------|---------|--------|
| T-10 | 終端状態から遷移不可_完了 | S5 | 任意 | success=false | 高 |
| T-11 | 終端状態から遷移不可_キャンセル | S6 | 任意 | success=false | 高 |
| T-12 | 出荷後キャンセル不可 | S4 | キャンセル | success=false | 高 |
| T-13 | 逆方向遷移不可 | S3 | 支払い処理 | success=false | 中 |
| T-14 | 未定義イベント | S1 | "受取確認" | success=false | 中 |

#### 1.1.3 パステスト（状態遷移パス全網羅）

| No | テストケース名 | 遷移パス | 期待最終状態 | 優先度 |
|----|-------------|---------|------------|--------|
| T-20 | 正常完了パス | S1→S2→S3→S4→S5 | 完了 | 高 |
| T-21 | 入金NG→キャンセル | S1→S2→S6 | キャンセル | 高 |
| T-22 | 注文直後キャンセル | S1→S6 | キャンセル | 高 |
| T-23 | 出荷前キャンセル | S1→S2→S3→S6 | キャンセル | 高 |

#### T-01: 正常遷移 T1

```csharp
[Fact]
public void Transition_T1_注文受付から入金確認中へ()
{
    var service = new StateTransitionService();

    var result = service.Transition("支払い処理");

    result.Success.Should().BeTrue();
    result.FromState.Should().Be("注文受付");
    result.ToState.Should().Be("入金確認中");
    service.GetCurrentState().StateId.Should().Be("S2");
}
```

#### T-10: 終端状態からの遷移不可

```csharp
[Fact]
public void Transition_完了状態_遷移不可()
{
    var service = new StateTransitionService();
    // S5（完了）まで進める
    service.Transition("支払い処理");
    service.Transition("入金確認OK");
    service.Transition("出荷完了");
    service.Transition("受取確認");

    var result = service.Transition("支払い処理");

    result.Success.Should().BeFalse();
    result.ErrorMessage.Should().NotBeNullOrEmpty();
}
```

#### T-20: 正常完了パス

```csharp
[Fact]
public void BatchTest_正常完了パス_最終状態が完了()
{
    var service = new StateTransitionService();

    service.Transition("支払い処理");
    service.Transition("入金確認OK");
    service.Transition("出荷完了");
    var last = service.Transition("受取確認");

    last.Success.Should().BeTrue();
    service.GetCurrentState().StateName.Should().Be("完了");
}
```

#### T-21〜T-23: キャンセルパス

```csharp
[Theory]
[InlineData(new[] { "支払い処理", "入金確認NG" })]
[InlineData(new[] { "キャンセル" })]
[InlineData(new[] { "支払い処理", "入金確認OK", "キャンセル" })]
public void Transition_各キャンセルパス_最終状態がキャンセル(string[] events)
{
    var service = new StateTransitionService();

    foreach (var e in events)
        service.Transition(e);

    service.GetCurrentState().StateName.Should().Be("キャンセル");
}
```

#### RunBatchTest

```csharp
[Fact]
public void RunBatchTest_4パス全合格する()
{
    var service = new StateTransitionService();
    var results = service.RunBatchTest();
    results.Should().HaveCount(4);
    results.Should().AllSatisfy(r => r.Passed.Should().BeTrue());
}
```

---

## 2. 統合テスト

### 2.1 StateTransitionControllerTests

| No | テストケース名 | HTTPメソッド | パス | Body | 期待ステータス | 確認項目 |
|----|-------------|------------|------|------|--------------|---------|
| T-30 | GetState_初期_S1 | GET | /api/demo/testing/state | - | 200 OK | stateId=S1 |
| T-31 | Transition_T1_成功 | POST | /api/demo/testing/state/transition | {event:"支払い処理"} | 200 OK | success=true, toState=S2 |
| T-32 | Transition_無効_失敗 | POST | /api/demo/testing/state/transition | {event:"受取確認"} | 200 OK | success=false |
| T-33 | Reset_S1に戻る | DELETE | /api/demo/testing/state/reset | - | 200 OK | stateId=S1 |
| T-34 | BatchTest_全パス合格 | POST | /api/demo/testing/state/batch | - | 200 OK | 4件全passed=true |

---

## 3. E2Eテスト

| No | テストケース名 | 概要 |
|----|-------------|------|
| T-40 | 画面表示_正常系 | 状態遷移テストデモ画面が表示される |
| T-41 | 正常完了パス操作 | 全イベントを順番に実行して「完了」状態になる |
| T-42 | 無効遷移_エラーメッセージ表示 | 無効なイベント実行でエラーが表示される |
| T-43 | リセット後_S1に戻る | リセットボタン押下でS1（注文受付）に戻る |
| T-44 | 全遷移自動実行_全合格 | 全遷移テスト自動実行ボタン→4パス全て✅ |

---

## 4. テストカバレッジ

| 項目 | 目標値 |
|------|--------|
| 行カバレッジ | 80%以上 |
| メソッドカバレッジ | 90%以上 |
| 遷移カバレッジ | 100%（T1〜T7全遷移） |
| 状態カバレッジ | 100%（S1〜S6全状態） |
| パスカバレッジ | 100%（4パス全て） |

---

## 5. 参考

- [外部設計書](external-design.md)
- [内部設計書](internal-design.md)
