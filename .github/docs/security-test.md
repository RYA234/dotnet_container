# セキュリティ 単体テスト仕様書

## 文書情報
- **作成日**: 2025-12-12
- **最終更新**: 2025-12-12
- **バージョン**: 1.0
- **ステータス**: 実装中
- **関連設計書**: [セキュリティ設計](security.md)

---

## 1. テスト対象

### 1.1 テスト対象コンポーネント

| コンポーネント | 説明 |
|-------------|------|
| 認証サービス | パスワード認証、トークンリフレッシュ |
| 認可ポリシー | ロールベース認可、クレームベース認可 |
| SQLインジェクション対策 | パラメータ化クエリ、ホワイトリスト検証 |
| XSS対策 | HTMLサニタイゼーション |
| 秘密情報管理 | シークレット取得、ログマスキング |
| CSRF対策 | トークン検証 |

---

## 2. テスト計画

### 2.1 テスト方針

1. **認証の正確性**: 有効な認証情報で成功、無効な認証情報で失敗すること
2. **認可の適切性**: ロールや権限に応じて正しく認可判定が行われること
3. **SQLインジェクション耐性**: 攻撃文字列が安全に処理され、データベースが破壊されないこと
4. **XSS耐性**: 悪意のあるスクリプトが除去され、安全なHTMLのみ保持されること
5. **秘密情報の保護**: シークレットが安全に取得され、ログでマスキングされること

---

### 2.2 テストレベル

| テストレベル | 対象 | 目的 |
|------------|------|------|
| 単体テスト | 認証サービス、認可ポリシー、サニタイザー | 各コンポーネントの独立した動作確認 |
| 統合テスト | 認証+認可、SQL実行+パラメータ化 | セキュリティ機能の統合動作確認 |

---

### 2.3 テストカバレッジ目標

| カテゴリ | 目標カバレッジ | 備考 |
|---------|--------------|------|
| 認証・認可 | 90% | サインイン、トークンリフレッシュ、ポリシー |
| SQLインジェクション対策 | 95% | パラメータ化クエリ、ホワイトリスト検証 |
| XSS対策 | 90% | HTMLサニタイゼーション |
| 秘密情報管理 | 85% | シークレット取得、ログマスキング |
| CSRF対策 | 80% | トークン検証 |

---

## 3. 認証機能のテストケース

### 3.1 パスワード認証

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-SEC-001 | 有効な認証情報でサインイン成功 | なし | email="test@example.com", password="ValidPassword123!" | 認証結果オブジェクト返却、AccessToken/RefreshToken設定、Information ログ出力 | 高 |
| TC-SEC-002 | 無効な認証情報で認証失敗 | なし | email="test@example.com", password="InvalidPassword" | UnauthorizedException、Warning ログ出力 | 高 |
| TC-SEC-003 | AccessTokenがnullの場合認証失敗 | なし | 認証成功するがAccessToken=null | UnauthorizedException | 中 |
| TC-SEC-004 | メールアドレス未確認ユーザーの検証 | メール未確認ユーザー存在 | 未確認ユーザーの認証情報 | EmailConfirmed=false の認証結果返却 | 中 |

---

### 3.2 トークンリフレッシュ

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-SEC-005 | 有効なリフレッシュトークンで新トークン取得 | なし | refreshToken="valid-refresh-token" | 新しいAccessToken/RefreshToken返却 | 高 |
| TC-SEC-006 | 無効なリフレッシュトークンで失敗 | なし | refreshToken="invalid-refresh-token" | UnauthorizedException、Warning ログ出力 | 高 |
| TC-SEC-007 | 期限切れリフレッシュトークンで失敗 | なし | refreshToken="expired-token" | UnauthorizedException、Warning ログ出力 | 中 |

---

## 4. 認可機能のテストケース

### 4.1 ロールベース認可

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-SEC-008 | 管理者ロールでAdminOnlyポリシー成功 | なし | Role="admin" | 認可成功（Succeeded=true） | 高 |
| TC-SEC-009 | 一般ユーザーでAdminOnlyポリシー失敗 | なし | Role="user" | 認可失敗（Succeeded=false） | 高 |
| TC-SEC-010 | 複数ロール保持ユーザーの認可 | なし | Roles=["user", "admin"] | 認可成功 | 中 |

---

### 4.2 クレームベース認可

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-SEC-011 | メール確認済みでEmailConfirmedポリシー成功 | なし | email_confirmed="true" | 認可成功 | 高 |
| TC-SEC-012 | メール未確認でEmailConfirmedポリシー失敗 | なし | email_confirmed="false" | 認可失敗 | 中 |

---

## 5. SQLインジェクション対策のテストケース

### 5.1 パラメータ化クエリ

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-SEC-013 | DROP TABLE攻撃文字列の安全な処理 | テストデータ存在 | email="'; DROP TABLE Users; --" | NotFoundException、データベース破壊なし | 高 |
| TC-SEC-014 | OR '1'='1' 攻撃の安全な処理 | テストデータ存在 | email="admin' OR '1'='1" | NotFoundException、全ユーザー返却なし | 高 |
| TC-SEC-015 | 特殊文字の自動エスケープ | テストデータ存在 | email="test'; DROP TABLE Users; --@example.com" | NotFoundException、テストデータ残存 | 高 |
| TC-SEC-016 | UNION SELECT攻撃の防御 | テストデータ存在 | email="' UNION SELECT * FROM Users --" | NotFoundException、データ漏洩なし | 中 |

---

### 5.2 ホワイトリスト検証

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-SEC-017 | ホワイトリスト内カラム名の受け入れ | なし | sortBy="id", sortOrder="ASC" | ユーザー一覧返却 | 高 |
| TC-SEC-018 | ホワイトリスト外カラム名の拒否 | なし | sortBy="password", sortOrder="ASC" | ValidationException | 高 |
| TC-SEC-019 | SQLインジェクション攻撃カラム名の拒否 | なし | sortBy="DROP TABLE Users", sortOrder="ASC" | ValidationException | 高 |
| TC-SEC-020 | 無効なソート順の拒否 | なし | sortBy="id", sortOrder="INVALID" | ValidationException | 中 |
| TC-SEC-021 | 複数カラム指定攻撃の拒否 | なし | sortBy="id; DROP TABLE Users; --" | ValidationException | 中 |

---

## 6. XSS対策のテストケース

### 6.1 HTMLサニタイゼーション

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-SEC-022 | scriptタグの削除 | なし | `<script>alert('XSS')</script>` | 空文字列 | 高 |
| TC-SEC-023 | onerror属性の削除 | なし | `<img src=x onerror=alert('XSS')>` | 空文字列 | 高 |
| TC-SEC-024 | javascript:スキームの削除 | なし | `<a href='javascript:alert("XSS")'>Click</a>` | 空文字列 | 高 |
| TC-SEC-025 | 安全なタグの保持 | なし | `<p>Hello World</p>` | `<p>Hello World</p>` | 高 |
| TC-SEC-026 | 危険な属性の削除 | なし | `<p onclick='alert("XSS")'>Click me</p>` | `<p>Click me</p>` | 高 |
| TC-SEC-027 | 空文字列の処理 | なし | `""` | `""` | 低 |
| TC-SEC-028 | nullの処理 | なし | `null` | `""` | 低 |
| TC-SEC-029 | style属性内のjavascriptの削除 | なし | `<div style='background:url(javascript:alert("XSS"))'>Text</div>` | `<div>Text</div>` | 中 |
| TC-SEC-030 | 安全なリンクの保持 | なし | `<a href='https://example.com'>Link</a>` | `<a href="https://example.com">Link</a>` | 中 |

---

## 7. 秘密情報管理のテストケース

### 7.1 シークレット取得

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-SEC-031 | 存在するシークレットの取得成功 | シークレット存在 | secretName="prod/database/connectionstring" | JSON文字列返却 | 高 |
| TC-SEC-032 | 存在しないシークレットで例外 | シークレット不在 | secretName="nonexistent/secret" | InfrastructureException、Error ログ出力 | 高 |
| TC-SEC-033 | 接続文字列の正しい取得 | シークレット存在 | 有効なJSON形式のシークレット | ConnectionString返却 | 高 |
| TC-SEC-034 | アクセス権限不足での失敗 | アクセス権限なし | 任意のsecretName | InfrastructureException、Error ログ出力 | 中 |

---

### 7.2 ログマスキング

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-SEC-035 | パスワードのマスキング | なし | "password=abc123" | "password=***" | 高 |
| TC-SEC-036 | 大文字小文字を区別せずマスキング | なし | "Password=SecurePass123!" | "Password=***" | 高 |
| TC-SEC-037 | APIキーのマスキング | なし | "apikey=sk_test_123456" | "apikey=***" | 高 |
| TC-SEC-038 | トークンのマスキング | なし | "token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" | "token=***" | 高 |
| TC-SEC-039 | 複数シークレットのマスキング | なし | "password=abc123 and apikey=sk_test_456" | 両方マスキング、元の値含まず | 高 |
| TC-SEC-040 | 非シークレットテキストの保持 | なし | "User email is test@example.com" | 変更なし | 中 |

---

## 8. CSRF対策のテストケース

### 8.1 トークン検証

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-SEC-041 | トークンなしでリクエスト拒否 | なし | CSRFトークンなしのPOSTリクエスト | StatusCode=400 | 高 |
| TC-SEC-042 | 有効なトークンでリクエスト成功 | なし | 有効なCSRFトークン含むPOSTリクエスト | StatusCode=201 | 高 |
| TC-SEC-043 | 無効なトークンでリクエスト拒否 | なし | 無効なCSRFトークン | StatusCode=400 | 中 |
| TC-SEC-044 | 期限切れトークンでリクエスト拒否 | なし | 期限切れCSRFトークン | StatusCode=400 | 中 |

---

## 9. テスト実装ガイドライン

### 9.1 テストツール推奨

| 言語 | 推奨ツール |
|------|----------|
| C# / .NET | xUnit, FluentAssertions, Moq |
| Java | JUnit 5, AssertJ, Mockito |
| TypeScript / Node.js | Jest, Supertest |
| Python | pytest, unittest.mock |
| Go | testing package, testify |

---

### 9.2 Mockの使用方針

1. **認証サービスのMock**: 外部認証サービス（Supabase、Auth0など）は必ずMock化
2. **シークレット管理のMock**: AWS Secrets Manager、Azure Key Vaultなど外部サービスはMock化
3. **データベースのMock**: SQL実行時のデータベース接続はMock化または専用テストDB使用
4. **ログのMock**: ログ出力を検証するためにLogger をMock化

---

### 9.3 テストデータ管理

1. **攻撃パターンデータ**: SQLインジェクション、XSS攻撃文字列を明確に定義
2. **テストユーザーデータ**: 各ロール・権限のユーザーデータを事前準備
3. **クリーンアップ**: テスト実行後は状態をクリーンアップ

---

## 10. テスト実行計画

### 10.1 実行順序

1. 認証機能のテスト（TC-SEC-001 〜 TC-SEC-007）
2. 認可機能のテスト（TC-SEC-008 〜 TC-SEC-012）
3. SQLインジェクション対策のテスト（TC-SEC-013 〜 TC-SEC-021）
4. XSS対策のテスト（TC-SEC-022 〜 TC-SEC-030）
5. 秘密情報管理のテスト（TC-SEC-031 〜 TC-SEC-040）
6. CSRF対策のテスト（TC-SEC-041 〜 TC-SEC-044）

---

### 10.2 テスト環境

| 項目 | 要件 |
|------|------|
| 認証サービス | Mockサービスまたはテスト用認証環境 |
| データベース | テスト用データベース（インメモリまたは専用DB） |
| シークレット管理 | Mockサービスまたはテスト用シークレット |
| ログ出力 | テスト用ログシンク（ファイルまたはメモリ） |

---

## 11. 参考

- [セキュリティ設計](security.md)
- [OWASP Testing Guide](https://owasp.org/www-project-web-security-testing-guide/)
- テストフレームワーク公式ドキュメント
- Mock ライブラリ公式ドキュメント
