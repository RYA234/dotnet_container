# ADR-008: 例外ハンドリングをMiddlewareで一括管理

## ステータス
採用済み

## 日付
2026-03-09

## コンテキスト

例外発生時のHTTPレスポンス生成を、Controllerごとにtrycatchで実装するか、Middlewareで一括管理するかを選択する必要があった。

Controller個別実装の場合、以下の問題が生じる。

- 全Controllerに同じ `try-catch` を書く必要があり、コードが重複する
- エラーレスポンスの形式がController間でズレるリスクがある
- 新しいエラー種別を追加するたびに全Controllerを修正する必要がある

## 決定

`ExceptionHandlingMiddleware` を実装し、`Program.cs` に登録して全リクエストの例外を一括ハンドリングする。

```
リクエスト → Middleware → Controller → Service
                ↑
           例外をここでキャッチしてJSONレスポンスに変換
```

ログレベルの方針も Middleware に集約する。

| 例外の種類 | ログレベル | 理由 |
|---|---|---|
| ValidationException / NotFoundException / BusinessRuleException | Warning | ユーザー起因のため |
| InfrastructureException / その他 | Error | システム障害のため |

## 理由

- **DRY原則**: エラー処理を一箇所に集めることで重複を排除できる
- **一貫性**: 全APIエンドポイントで同じフォーマットのエラーレスポンスが保証される
- **拡張性**: 新しい例外クラスの追加時は Middleware のみ修正すればよい

## 却下した代替案

**Controller個別にtry-catchを実装する**
- 全Controllerで同じコードが重複する
- エラーレスポンス形式の統一が人的管理に依存してしまう

**ASP.NET Core 標準の `UseExceptionHandler` を使う**
- カスタムの `ErrorCode`・`Details` を含むJSONレスポンスを柔軟に生成しにくい
- 例外の種類ごとにログレベルを変える制御が難しい

## 結果

- Controller は業務ロジックの結果を返すことに専念できる（try-catch 不要）
- エラーレスポンスの形式が全エンドポイントで統一される
- 開発環境のみ `StackTrace` を付与し、本番環境では内部情報を隠蔽するセキュリティ制御も一箇所で管理できる

## 関連

- [ADR-007: カスタム例外クラスの導入](007-custom-exception-classes.md)
