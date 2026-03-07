# バリデーション 単体テスト仕様書

## 文書情報
- **作成日**: 2026-03-07
- **最終更新**: 2026-03-07
- **バージョン**: 1.0
- **ステータス**: ドラフト
- **関連設計書**: [バリデーション設計](validation.md)

---

## 1. テスト対象

### 1.1 テスト対象コンポーネント

| コンポーネント | 説明 |
|-------------|------|
| Data Annotations | [Required], [MaxLength], [Range], [EmailAddress] などの属性バリデーション |
| ModelState チェック | Controller での ModelState.IsValid によるバリデーション |
| 業務ルールバリデーション | Service 層での DB 参照を伴うバリデーション（存在チェック・与信チェック等） |
| エラーレスポンス形式 | バリデーションエラー時の統一レスポンス形式 |

---

## 2. テスト計画

### 2.1 テスト方針

1. **Data Annotations の正確性**: 各属性が正しくバリデーションエラーを検出すること
2. **ModelState の一貫性**: ModelState.IsValid が false の場合に 400 を返すこと
3. **業務ルールの正確性**: Service 層のバリデーションが正しい例外をスローすること
4. **エラーレスポンスの統一**: バリデーションエラーが標準形式（type, errors, timestamp）で返ること

---

### 2.2 テストレベル

| テストレベル | 対象 | 目的 |
|------------|------|------|
| 単体テスト | Request DTO（Data Annotations）、Service バリデーションロジック | 各バリデーションルールの独立した動作確認 |
| 統合テスト | Controller + ModelState + Service | バリデーションの統合動作確認 |

---

### 2.3 テストカバレッジ目標

| カテゴリ | 目標カバレッジ | 備考 |
|---------|--------------|------|
| Data Annotations | 100% | 各属性の正常・異常パターンをカバー |
| ModelState チェック | 90% | 単一エラー・複数エラーパターンをカバー |
| 業務ルールバリデーション | 90% | 存在チェック・業務制約チェックをカバー |
| エラーレスポンス形式 | 100% | 全フィールドの存在と値を確認 |

---

## 3. Data Annotations のテストケース

### 3.1 [Required] 属性

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-VL-001 | 必須フィールドが null の場合、バリデーションエラー | なし | CustomerCode=null | IsValid=false, ErrorMessage="顧客コードは必須です" | 高 |
| TC-VL-002 | 必須フィールドが空文字の場合、バリデーションエラー | なし | CustomerCode="" | IsValid=false | 高 |
| TC-VL-003 | 必須フィールドに値がある場合、バリデーション成功 | なし | CustomerCode="C001" | IsValid=true | 高 |

---

### 3.2 [MaxLength] 属性

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-VL-004 | 最大長を超える場合、バリデーションエラー | なし | CustomerCode="12345678901"（11文字、上限10） | IsValid=false, ErrorMessage="顧客コードは10文字以内で入力してください" | 高 |
| TC-VL-005 | 最大長ちょうどの場合、バリデーション成功 | なし | CustomerCode="1234567890"（10文字） | IsValid=true | 高 |
| TC-VL-006 | 最大長未満の場合、バリデーション成功 | なし | CustomerCode="C001"（4文字） | IsValid=true | 中 |

---

### 3.3 [Range] 属性

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-VL-007 | 最小値未満の場合、バリデーションエラー | なし | Quantity=0（最小値1） | IsValid=false, ErrorMessage="数量は1以上で入力してください" | 高 |
| TC-VL-008 | 負の値の場合、バリデーションエラー | なし | Quantity=-1 | IsValid=false | 高 |
| TC-VL-009 | 最小値ちょうどの場合、バリデーション成功 | なし | Quantity=1 | IsValid=true | 高 |
| TC-VL-010 | 最小値より大きい場合、バリデーション成功 | なし | Quantity=100 | IsValid=true | 中 |

---

### 3.4 [EmailAddress] 属性

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-VL-011 | メール形式が不正の場合、バリデーションエラー | なし | Email="not-an-email" | IsValid=false | 高 |
| TC-VL-012 | @ がない場合、バリデーションエラー | なし | Email="userexample.com" | IsValid=false | 高 |
| TC-VL-013 | 正しいメール形式の場合、バリデーション成功 | なし | Email="user@example.com" | IsValid=true | 高 |

---

## 4. ModelState のテストケース

### 4.1 単一フィールドのエラー

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-VL-014 | 単一フィールドが不正の場合、400 を返す | なし | CustomerCode=null, OrderDate=有効, Quantity=1 | StatusCode=400, errors に CustomerCode のエラー1件 | 高 |
| TC-VL-015 | エラーレスポンスに type="ValidationError" が含まれる | なし | CustomerCode=null | Response.type="ValidationError" | 高 |
| TC-VL-016 | エラーレスポンスに timestamp が含まれる | なし | CustomerCode=null | Response.timestamp が現在時刻（UTC） | 中 |

---

### 4.2 複数フィールドのエラー

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-VL-017 | 複数フィールドが不正の場合、すべてのエラーを返す | なし | CustomerCode=null, Quantity=0 | StatusCode=400, errors に CustomerCode と Quantity の2件 | 高 |
| TC-VL-018 | errors 配列に field と message が含まれる | なし | CustomerCode=null | errors[0].field="CustomerCode", errors[0].message が含まれる | 高 |
| TC-VL-019 | すべて有効な場合、バリデーションを通過する | なし | CustomerCode="C001", OrderDate=有効, Quantity=1 | StatusCode=201 または 200 | 高 |

---

## 5. 業務ルールバリデーションのテストケース

### 5.1 存在チェック

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-VL-020 | 存在しない顧客コードの場合、ValidationException をスロー | 顧客コード "C999" が存在しない | CustomerCode="C999" | ValidationException, Message="顧客コード 'C999' は存在しません" | 高 |
| TC-VL-021 | 存在する顧客コードの場合、例外なし | 顧客コード "C001" が存在する | CustomerCode="C001" | 例外なし、処理継続 | 高 |

---

### 5.2 業務制約チェック

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-VL-022 | 与信限度額を超える場合、ValidationException をスロー | 顧客の CreditLimit=10000 | TotalAmount=20000 | ValidationException, Message="与信限度額を超えています" | 高 |
| TC-VL-023 | 与信限度額以下の場合、例外なし | 顧客の CreditLimit=10000 | TotalAmount=10000 | 例外なし、処理継続 | 高 |
| TC-VL-024 | 与信限度額ちょうどの場合、例外なし | 顧客の CreditLimit=10000 | TotalAmount=10000 | 例外なし（境界値） | 中 |

---

### 5.3 重複チェック

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-VL-025 | 重複するコードで登録する場合、ValidationException をスロー | 同じコードがすでに存在する | 既存コードと同じ値 | ValidationException, 重複エラーメッセージ含む | 高 |
| TC-VL-026 | 重複しないコードで登録する場合、例外なし | 同じコードが存在しない | 新規コード | 例外なし、処理継続 | 高 |

---

## 6. エラーレスポンス形式のテストケース

### 6.1 レスポンス構造

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-VL-027 | レスポンスに type フィールドが存在する | バリデーションエラー発生 | 不正なリクエスト | Response.type="ValidationError" | 高 |
| TC-VL-028 | レスポンスに errors 配列が存在する | バリデーションエラー発生 | 不正なリクエスト | Response.errors が配列 | 高 |
| TC-VL-029 | レスポンスに timestamp フィールドが存在する | バリデーションエラー発生 | 不正なリクエスト | Response.timestamp が ISO 8601 形式 | 高 |
| TC-VL-030 | errors 配列の各要素に field と message が存在する | バリデーションエラー発生 | CustomerCode=null | errors[0].field が文字列, errors[0].message が文字列 | 高 |
| TC-VL-031 | Content-Type が application/json | バリデーションエラー発生 | 不正なリクエスト | Content-Type="application/json" | 中 |

---

## 7. テスト実装ガイドライン

### 7.1 テストツール推奨

| 言語 | 推奨ツール |
|------|----------|
| C# / .NET | xUnit, FluentAssertions, Moq |

---

### 7.2 Mockの使用方針

1. **DB参照のMock**: 存在チェック・重複チェックなど DB 参照が必要な処理は必ず Mock 化
2. **ログのMock**: ログ出力を検証するために Logger を Mock 化

---

### 7.3 テストデータ管理

1. **固定テストデータ**: 各テストケースで使用するテストデータを明確に定義
2. **テストデータの独立性**: テスト間でデータを共有しない
3. **境界値テスト**: MaxLength・Range の境界値（ちょうど・+1・-1）を必ずテスト

---

## 8. テスト実行計画

### 8.1 実行順序

1. Data Annotations のテスト（TC-VL-001 〜 TC-VL-013）
2. ModelState のテスト（TC-VL-014 〜 TC-VL-019）
3. 業務ルールバリデーションのテスト（TC-VL-020 〜 TC-VL-026）
4. エラーレスポンス形式のテスト（TC-VL-027 〜 TC-VL-031）

---

### 8.2 テスト環境

| 項目 | 要件 |
|------|------|
| データベース | インメモリ DB または Mock |
| 外部依存 | Moq でスタブ化 |
| ログ出力 | テスト用ログシンク（メモリ） |

---

## 9. 参考

- [バリデーション設計](validation.md)
- [エラーハンドリング設計](error-handling.md)
- xUnit 公式ドキュメント
- FluentAssertions 公式ドキュメント
