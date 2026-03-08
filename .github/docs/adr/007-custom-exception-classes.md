# ADR-007: カスタム例外クラスの導入

## ステータス
採用済み

## 日付
2026-03-09

## コンテキスト

ASP.NET Core の標準例外（`Exception`, `ArgumentException` 等）をそのまま使用すると、以下の問題が生じる。

- 例外の種類とHTTPステータスコードの対応を、キャッチする側（ControllerやMiddleware）が知る必要がある
- `ex.Message` の文字列比較でエラー種別を判断することになり、壊れやすい
- `ErrorCode`・`Details` など、APIレスポンスに必要な情報を例外が持てない

## 決定

`ApplicationException`（抽象基底クラス）を定義し、以下の4種類のカスタム例外を実装する。

| クラス | HTTPステータス | 用途 |
|---|---|---|
| `ValidationException` | 400 | 入力値のバリデーションエラー |
| `NotFoundException` | 404 | リソース未存在 |
| `BusinessRuleException` | 400 | ビジネスルール違反（在庫不足など） |
| `InfrastructureException` | 500 | DB・外部API障害 |

各クラスは `StatusCode`・`ErrorCode`・`Details` を保持する。

## 理由

型でキャッチできるため、Middleware での分岐がシンプルかつ安全になる。

```csharp
// 型で分岐 → 文字列比較不要、コンパイル時に検出できる
catch (NotFoundException ex)       // → 404
catch (ValidationException ex)    // → 400
catch (InfrastructureException ex) // → 500
catch (Exception ex)               // → 500（予期しない例外）
```

## 却下した代替案

**標準例外をそのまま使う**
- `catch (Exception ex)` で全部受けて `ex.Message` で分岐する方法
- 文字列比較になるため壊れやすく、新しいエラー種別の追加時に漏れが起きやすい

## 結果

- Service 層は業務ロジックに集中し、HTTPの関心事（ステータスコード）を持たなくて済む
- Middleware が一箇所でHTTPレスポンスに変換する責務を担える（ADR-008参照）
- テスト時に例外クラスの型・プロパティを直接検証できる
