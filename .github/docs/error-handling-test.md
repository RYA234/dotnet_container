# エラーハンドリング 単体テスト仕様書

## 文書情報
- **作成日**: 2025-12-12
- **最終更新**: 2025-12-12
- **バージョン**: 1.0
- **ステータス**: 実装中
- **関連設計書**: [エラーハンドリング設計](error-handling.md)

---

## 1. テスト対象

### 1.1 テスト対象コンポーネント

| コンポーネント | 説明 |
|-------------|------|
| カスタム例外クラス | ApplicationException, NotFoundException, ValidationException, BusinessRuleException, InfrastructureException |
| エラーハンドリングミドルウェア | グローバル例外ハンドラー |
| Controller エラーハンドリング | Controller レベルの例外処理 |
| リトライロジック | 失敗時の自動リトライ機能 |
| Circuit Breaker | サーキットブレーカーパターン実装 |

---

## 2. テスト計画

### 2.1 テスト方針

1. **例外クラスの正確性**: カスタム例外クラスが正しいプロパティと値を保持すること
2. **エラーレスポンスの一貫性**: すべてのエラーレスポンスが標準形式に従うこと
3. **ログ出力の適切性**: エラー発生時に適切なログレベルでログが出力されること
4. **リトライロジックの正確性**: 指定回数リトライし、最終的に失敗または成功すること
5. **Circuit Breaker の動作**: 連続失敗時に開き、一定時間後にリセットされること

---

### 2.2 テストレベル

| テストレベル | 対象 | 目的 |
|------------|------|------|
| 単体テスト | 例外クラス、リトライロジック、Circuit Breaker | 各コンポーネントの独立した動作確認 |
| 統合テスト | Controller + Service + Middleware | エラーハンドリングの統合動作確認 |

---

### 2.3 テストカバレッジ目標

| カテゴリ | 目標カバレッジ | 備考 |
|---------|--------------|------|
| カスタム例外クラス | 100% | コンストラクタ、プロパティのテスト |
| Controller エラーハンドリング | 90% | 主要な例外パターンをカバー |
| グローバル例外ハンドラー | 90% | ApplicationException、予期しない例外 |
| リトライロジック | 85% | 成功/失敗/部分成功パターン |
| Circuit Breaker | 85% | 開閉状態、リセット |

---

## 3. カスタム例外クラスのテストケース

### 3.1 NotFoundException

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-001 | コンストラクタでプロパティが正しく設定される | なし | resourceType="User", resourceId="123" | ResourceType="User", ResourceId="123", ErrorCode="NOT_FOUND", StatusCode=404, Message="User with id '123' not found" | 高 |
| TC-EH-002 | Detailsに正しい値が設定される | なし | resourceType="User", resourceId="123" | Details["resourceType"]="User", Details["resourceId"]="123" | 高 |
| TC-EH-003 | 異なるリソースタイプでも正しく動作 | なし | resourceType="Order", resourceId="456" | Message="Order with id '456' not found" | 中 |
| TC-EH-004 | 空文字列のリソースIDでも動作 | なし | resourceType="User", resourceId="" | Message="User with id '' not found" | 低 |

---

### 3.2 ValidationException

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-005 | バリデーションエラーリストが正しく設定される | なし | errors=[{Field="Email", Message="Invalid"}, {Field="Name", Message="Required"}] | Errors.Count=2, ErrorCode="VALIDATION_ERROR", StatusCode=400 | 高 |
| TC-EH-006 | 空のエラーリストでも例外が生成される | なし | errors=[] | Errors.Count=0, ErrorCode="VALIDATION_ERROR" | 中 |
| TC-EH-007 | 単一のエラーでも正しく動作 | なし | errors=[{Field="Email", Message="Invalid"}] | Errors.Count=1 | 中 |
| TC-EH-008 | Detailsにエラーリストが含まれる | なし | errors=[{Field="Email", Message="Invalid"}] | Details["errors"] に配列が含まれる | 高 |

---

### 3.3 BusinessRuleException

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-009 | ルール名とメッセージが正しく設定される | なし | ruleName="UNIQUE_EMAIL", message="Email already exists" | RuleName="UNIQUE_EMAIL", Message="Email already exists", ErrorCode="BUSINESS_RULE_VIOLATION", StatusCode=400 | 高 |
| TC-EH-010 | Detailsにルール名が含まれる | なし | ruleName="UNIQUE_EMAIL", message="Email already exists" | Details["ruleName"]="UNIQUE_EMAIL" | 高 |
| TC-EH-011 | 異なるビジネスルールでも正しく動作 | なし | ruleName="MIN_AGE", message="User must be at least 18" | RuleName="MIN_AGE", Message contains "18" | 中 |

---

### 3.4 InfrastructureException

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-012 | サービス名とメッセージが正しく設定される | なし | service="ExternalAPI", message="API call failed", innerException | Service="ExternalAPI", Message="API call failed", ErrorCode="INFRASTRUCTURE_ERROR", StatusCode=500 | 高 |
| TC-EH-013 | Detailsにサービス名が含まれる | なし | service="Database", message="Connection failed", innerException | Details["service"]="Database" | 高 |
| TC-EH-014 | InnerExceptionが保持される | なし | service="ExternalAPI", message="API call failed", innerException=HttpException | InnerException が設定されている | 中 |

---

## 4. Controller エラーハンドリングのテストケース

### 4.1 NotFoundException ハンドリング

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-015 | ユーザーが存在しない場合404を返す | なし | userId=999（存在しない） | StatusCode=404, ErrorCode="NOT_FOUND", Error="User with id '999' not found", Warning ログ出力 | 高 |
| TC-EH-016 | ユーザーが存在する場合200を返す | ユーザーID=1が存在 | userId=1 | StatusCode=200, User オブジェクト返却 | 高 |
| TC-EH-017 | エラーレスポンスにタイムスタンプが含まれる | なし | userId=999 | ErrorResponse.Timestamp が現在時刻（UTC） | 中 |
| TC-EH-018 | エラーレスポンスにDetailsが含まれる | なし | userId=999 | Details["resourceType"]="User", Details["resourceId"]="999" | 中 |

---

### 4.2 ValidationException ハンドリング

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-019 | バリデーションエラー時に400を返す | なし | Name="", Email="invalid-email" | StatusCode=400, ErrorCode="VALIDATION_ERROR", Details["errors"] に配列 | 高 |
| TC-EH-020 | バリデーション成功時に201を返す | なし | Name="John Doe", Email="john@example.com", Password="Valid123!" | StatusCode=201, Location ヘッダー設定 | 高 |
| TC-EH-021 | ModelStateエラーが正しくマッピングされる | なし | Name="", Email="invalid" | Details["errors"] に Name と Email のエラー含む | 中 |
| TC-EH-022 | 複数のバリデーションエラーをすべて返す | なし | Name="", Email="", Password="" | Details["errors"] に3つのエラー含む | 中 |

---

### 4.3 BusinessRuleException ハンドリング

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-023 | メールアドレス重複時に400を返す | Email="existing@example.com"が既に存在 | Email="existing@example.com" | StatusCode=400, ErrorCode="BUSINESS_RULE_VIOLATION", Details["ruleName"]="UNIQUE_EMAIL", Warning ログ出力 | 高 |
| TC-EH-024 | ビジネスルール違反のメッセージが含まれる | Email="existing@example.com"が既に存在 | Email="existing@example.com" | Error="Email already exists" | 中 |

---

### 4.4 予期しない例外ハンドリング

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-025 | 予期しない例外発生時に500を返す | Service層で予期しない例外発生 | 任意のリクエスト | StatusCode=500, ErrorCode="INTERNAL_ERROR", Error="Internal server error", Error ログ出力 | 高 |
| TC-EH-026 | 例外詳細をログに出力、ユーザーには非表示 | Service層で例外発生 | 任意のリクエスト | ログに例外スタックトレース含む、レスポンスには含まない | 高 |

---

## 5. グローバル例外ハンドラーのテストケース

### 5.1 ApplicationException ハンドリング

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-027 | NotFoundException発生時に404を返す | Middleware経由でNotFoundException発生 | NotFoundException("User", "123") | StatusCode=404, Content-Type="application/json", ErrorCode="NOT_FOUND" | 高 |
| TC-EH-028 | ValidationException発生時に400を返す | Middleware経由でValidationException発生 | ValidationException(errors) | StatusCode=400, ErrorCode="VALIDATION_ERROR" | 高 |
| TC-EH-029 | エラーログが出力される | ApplicationException発生 | 任意のApplicationException | Error ログに例外情報が含まれる | 中 |

---

### 5.2 予期しない例外ハンドリング

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-030 | 予期しない例外発生時に500を返す | Middleware経由でInvalidOperationException発生 | InvalidOperationException | StatusCode=500, ErrorCode="INTERNAL_ERROR", Error="Internal server error", Error ログ出力 | 高 |
| TC-EH-031 | レスポンスがJSON形式 | Middleware経由で例外発生 | 任意の例外 | Content-Type="application/json", 有効なJSON | 高 |

---

## 6. リトライロジックのテストケース

### 6.1 リトライ成功パターン

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-032 | 3回リトライ後も失敗する場合、例外をスロー | リトライ対象の処理が常に失敗 | HttpRequestException | 4回実行（1回目+3回リトライ）、最終的に例外スロー、Warning ログ3回出力 | 高 |
| TC-EH-033 | 2回目の実行で成功する場合、成功を返す | 1回目失敗、2回目成功 | 1回目HttpRequestException、2回目成功 | 2回実行、結果を返す、Warning ログ1回出力 | 高 |
| TC-EH-034 | 1回目で成功する場合、リトライなし | リトライ対象の処理が成功 | 正常なレスポンス | 1回のみ実行、リトライなし、Warning ログなし | 中 |
| TC-EH-035 | リトライ間隔が指数バックオフ | リトライ3回実行 | HttpRequestException | 1回目→2秒待機、2回目→4秒待機、3回目→8秒待機 | 中 |

---

### 6.2 リトライ失敗パターン

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-036 | リトライ対象外の例外は即座にスロー | ArgumentNullException発生 | ArgumentNullException | 1回のみ実行、即座に例外スロー、リトライなし | 高 |
| TC-EH-037 | タイムアウト例外は再スロー | TimeoutException発生 | TimeoutException | リトライせず即座にスロー | 中 |

---

## 7. Circuit Breaker のテストケース

### 7.1 Circuit Breaker 開閉

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-038 | 3回連続失敗後、Circuit Breakerが開く | Circuit Breaker設定: 3回失敗で開く | 3回連続でHttpRequestException | 4回目の実行でBrokenCircuitException、Error ログ出力 | 高 |
| TC-EH-039 | Circuit Breaker開放中は即座に失敗 | Circuit Breakerが開いている状態 | 任意のリクエスト | BrokenCircuitException、実際の処理は実行されない | 高 |
| TC-EH-040 | durationOfBreak後、Circuit Breakerがリセットされる | Circuit Breaker開放後100ms待機 | 2回失敗→100ms待機→成功リクエスト | リセット後は正常に実行される、Information ログ出力 | 高 |
| TC-EH-041 | 成功後、失敗カウントがリセットされる | 2回失敗→1回成功 | 2回失敗、1回成功、再度3回失敗 | 最初の2回失敗はカウントされず、再度3回失敗で開く | 中 |

---

### 7.2 Circuit Breaker 部分開放

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-EH-042 | Half-Open状態で成功すると閉じる | Circuit Breakerが開いた後、durationOfBreak経過 | 成功リクエスト | Circuit Breakerが閉じる、Information ログ出力 | 中 |
| TC-EH-043 | Half-Open状態で失敗すると再度開く | Circuit Breakerが開いた後、durationOfBreak経過 | 失敗リクエスト | Circuit Breakerが再度開く、Error ログ出力 | 中 |

---

## 8. テスト実装ガイドライン

### 8.1 テストツール推奨

| 言語 | 推奨ツール |
|------|----------|
| C# / .NET | xUnit, FluentAssertions, Moq |
| Java | JUnit 5, AssertJ, Mockito |
| TypeScript / Node.js | Jest, Supertest |
| Python | pytest, unittest.mock |

---

### 8.2 Mockの使用方針

1. **外部依存のMock**: データベース、外部API、ファイルシステムは必ずMock化
2. **時間のMock**: 現在時刻、タイムアウトなど時間依存の処理はMock化
3. **ログのMock**: ログ出力を検証するためにLogger をMock化

---

### 8.3 テストデータ管理

1. **固定テストデータ**: 各テストケースで使用するテストデータを明確に定義
2. **テストデータの独立性**: テスト間でデータを共有しない
3. **クリーンアップ**: テスト実行後は状態をクリーンアップ

---

## 9. テスト実行計画

### 9.1 実行順序

1. カスタム例外クラスのテスト（TC-EH-001 〜 TC-EH-014）
2. Controller エラーハンドリングのテスト（TC-EH-015 〜 TC-EH-026）
3. グローバル例外ハンドラーのテスト（TC-EH-027 〜 TC-EH-031）
4. リトライロジックのテスト（TC-EH-032 〜 TC-EH-037）
5. Circuit Breaker のテスト（TC-EH-038 〜 TC-EH-043）

---

### 9.2 テスト環境

| 項目 | 要件 |
|------|------|
| データベース | テスト用データベース（インメモリまたは専用DB） |
| 外部API | Mock サーバーまたはスタブ |
| ログ出力 | テスト用ログシンク（ファイルまたはメモリ） |

---

## 10. 参考

- [エラーハンドリング設計](error-handling.md)
- テストフレームワーク公式ドキュメント
- Mock ライブラリ公式ドキュメント
