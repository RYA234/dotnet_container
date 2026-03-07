# バリデーション設計

## 文書情報
- **作成日**: 2026-03-07
- **バージョン**: 1.0
- **ステータス**: ドラフト

---

## 1. 基本方針

> **バックエンドのバリデーションは必須。フロントエンドのバリデーションはUX改善のオプション。**

フロントエンドのチェックはブラウザの開発ツールで簡単に回避できるため、セキュリティ・データ整合性の保証はバックエンドで必ず行う。

---

## 2. フロントエンド vs バックエンド 判断基準

| チェック内容 | フロント | バックエンド | 理由 |
|------------|---------|------------|------|
| 必須入力 | ○（即時フィードバック） | ○（必須） | UX改善 + セキュリティ |
| 文字数制限 | ○（即時フィードバック） | ○（必須） | UX改善 + DB制約保護 |
| 数値範囲 | ○（即時フィードバック） | ○（必須） | UX改善 + セキュリティ |
| メール形式 | ○（即時フィードバック） | ○（必須） | UX改善 + セキュリティ |
| 重複チェック（DB参照） | ✕ | ○（必須） | DB参照が必要なため |
| 業務ルール（複数テーブル） | ✕ | ○（必須） | DB参照が必要なため |
| 認証・認可チェック | ✕ | ○（必須） | フロントは改ざん可能なため |

---

## 3. バックエンドのバリデーション実装

### 3.1 Data Annotations（ASP.NET Core標準）

RequestDTOに属性を付与してバリデーションを宣言的に定義する。

```csharp
public class CreateOrderRequest
{
    [Required(ErrorMessage = "顧客コードは必須です")]
    [MaxLength(10, ErrorMessage = "顧客コードは10文字以内で入力してください")]
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "受注日は必須です")]
    public DateTime OrderDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "数量は1以上で入力してください")]
    public int Quantity { get; set; }
}
```

### 3.2 ModelStateによるチェック

ControllerでModelState.IsValidをチェックする。

```csharp
[HttpPost("api/orders")]
public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    var result = await _orderService.CreateOrder(request);
    return Ok(result);
}
```

### 3.3 業務ルールのバリデーション（Service層）

DB参照が必要なチェックはService層で行い、例外をスローしてControllerに伝える。

```csharp
public async Task<OrderResponse> CreateOrder(CreateOrderRequest request)
{
    // 顧客存在チェック
    var customer = await GetCustomerAsync(request.CustomerCode);
    if (customer == null)
    {
        throw new ValidationException($"顧客コード '{request.CustomerCode}' は存在しません");
    }

    // 与信チェック
    if (customer.CreditLimit < request.TotalAmount)
    {
        throw new ValidationException("与信限度額を超えています");
    }

    // ... 処理
}
```

---

## 4. エラーレスポンス形式

バリデーションエラーは統一した形式で返す。

```json
{
  "type": "ValidationError",
  "errors": [
    {
      "field": "CustomerCode",
      "message": "顧客コードは必須です"
    },
    {
      "field": "Quantity",
      "message": "数量は1以上で入力してください"
    }
  ],
  "timestamp": "2026-03-07T00:00:00Z"
}
```

---

## 5. 未決事項

- [ ] フロントエンドのバリデーションライブラリの選定（Blazorの標準機能で対応するか）
- [ ] 業務ルールのバリデーション例外クラスを共通化するか
- [ ] バリデーションエラーのログ出力要否

---

## 6. 参考

- [クラス図](class-diagram.md)
- [エラーハンドリング設計](error-handling.md)
