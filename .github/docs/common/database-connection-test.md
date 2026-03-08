# DB接続管理 単体テスト仕様書

## 文書情報
- **作成日**: 2026-03-08
- **最終更新**: 2026-03-08
- **バージョン**: 1.0
- **ステータス**: ドラフト
- **関連設計書**: [DB接続管理設計](database-connection.md)

---

## 1. テスト対象

### 1.1 テスト対象コンポーネント

| コンポーネント | 説明 |
|-------------|------|
| IDbConnectionFactory | ファクトリのインターフェース実装 |
| ファクトリ切り替え | Provider設定に応じた適切なファクトリの生成 |
| 接続管理 | CreateConnection / CreateOpenConnection の動作 |
| トランザクション管理 | 開始・コミット・ロールバックの動作 |
| エラー変換 | RDBMS固有例外から共通例外への変換 |
| 秘密情報マスキング | ログ出力時のパスワード・トークンのマスキング |

---

## 2. テスト計画

### 2.1 テスト方針

1. **ファクトリの切り替えの正確性**: Provider設定に応じて正しいファクトリが生成されること
2. **接続管理の正確性**: 接続の作成・オープン・クローズが正しく動作すること
3. **トランザクションの一貫性**: コミット・ロールバックが正しく動作すること
4. **エラー変換の正確性**: RDBMS固有の例外が共通例外に正しく変換されること
5. **秘密情報の保護**: ログ出力時にパスワード等がマスキングされること

---

### 2.2 テストレベル

| テストレベル | 対象 | 目的 |
|------------|------|------|
| 単体テスト | ファクトリ切り替え・エラー変換・マスキングロジック | 各機能の独立した動作確認 |
| 統合テスト | 実DB接続・トランザクション・接続プール | 実際のDB環境での動作確認 |

---

### 2.3 テストカバレッジ目標

| カテゴリ | 目標カバレッジ | 備考 |
|---------|--------------|------|
| ファクトリ切り替え | 100% | 全Providerパターンをカバー |
| 接続管理 | 90% | 正常・異常パターンをカバー |
| トランザクション管理 | 90% | コミット・ロールバック・ネスト等をカバー |
| エラー変換 | 100% | 全RDBMSの主要エラーコードをカバー |
| 秘密情報マスキング | 100% | 全マスキング対象パターンをカバー |

---

## 3. ファクトリ切り替えのテストケース

### 3.1 Provider設定による切り替え

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-DB-001 | PostgreSQL Providerの場合、PostgreSqlConnectionFactoryが生成される | なし | Provider="PostgreSQL" | PostgreSqlConnectionFactoryのインスタンス | 高 |
| TC-DB-002 | SQLite Providerの場合、SqliteConnectionFactoryが生成される | なし | Provider="SQLite" | SqliteConnectionFactoryのインスタンス | 高 |
| TC-DB-003 | SqlServer Providerの場合、SqlServerConnectionFactoryが生成される | なし | Provider="SqlServer" | SqlServerConnectionFactoryのインスタンス | 高 |
| TC-DB-004 | 未知のProviderの場合、例外がスローされる | なし | Provider="Unknown" | ArgumentException または InvalidOperationException | 高 |
| TC-DB-005 | Provider未設定の場合、例外がスローされる | なし | Provider=null | InvalidOperationException | 高 |

---

## 4. 接続管理のテストケース

### 4.1 CreateConnection

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-DB-006 | CreateConnectionが未オープン状態の接続を返す | 有効な接続文字列 | なし | ConnectionState=Closed | 高 |
| TC-DB-007 | 接続文字列が未設定の場合、例外がスローされる | 接続文字列なし | なし | InvalidOperationException | 高 |

---

### 4.2 CreateOpenConnection

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-DB-008 | CreateOpenConnectionがオープン状態の接続を返す | 有効な接続文字列・DB起動済み | なし | ConnectionState=Open | 高 |
| TC-DB-009 | DB接続失敗の場合、InfrastructureExceptionがスローされる | DBが停止中 | なし | InfrastructureException, ErrorCode=CONNECTION_FAILURE | 高 |

---

### 4.3 TestConnection

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-DB-010 | DB起動中の場合、TestConnectionがtrueを返す | DBが起動中 | なし | true | 高 |
| TC-DB-011 | DB停止中の場合、TestConnectionがfalseを返す | DBが停止中 | なし | false | 高 |

---

## 5. トランザクション管理のテストケース

### 5.1 コミット

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-DB-012 | 正常処理の場合、トランザクションがコミットされる | DB起動済み | 有効なSQL操作 | データがDBに反映される | 高 |
| TC-DB-013 | ExecuteInTransactionが成功した場合、自動コミットされる | DB起動済み | 有効なSQL操作 | データがDBに反映される | 高 |

---

### 5.2 ロールバック

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-DB-014 | 例外発生の場合、トランザクションがロールバックされる | DB起動済み | 途中で例外が発生する操作 | データがDBに反映されない | 高 |
| TC-DB-015 | ExecuteInTransactionが失敗した場合、自動ロールバックされる | DB起動済み | 途中で例外が発生する操作 | データがDBに反映されない | 高 |

---

### 5.3 分離レベル

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-DB-016 | SQLiteでSerializable以外を指定した場合、Serializableで動作する | SQLite使用 | IsolationLevel=ReadCommitted | Serializableで動作、エラーなし | 中 |
| TC-DB-017 | PostgreSQLでReadCommittedを指定した場合、正常に動作する | PostgreSQL使用 | IsolationLevel=ReadCommitted | 指定した分離レベルで動作 | 中 |

---

## 6. エラー変換のテストケース

### 6.1 PostgreSQL固有エラーの変換

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-DB-018 | PostgreSQLの一意制約違反がDuplicateKeyExceptionに変換される | なし | NpgsqlException(23505) | DuplicateKeyException, ErrorCode=DUPLICATE_KEY | 高 |
| TC-DB-019 | PostgreSQLの接続失敗がConnectionFailureExceptionに変換される | なし | NpgsqlException(08000) | ConnectionFailureException, ErrorCode=CONNECTION_FAILURE | 高 |
| TC-DB-020 | PostgreSQLのタイムアウトがQueryTimeoutExceptionに変換される | なし | NpgsqlException(57014) | QueryTimeoutException, ErrorCode=QUERY_TIMEOUT | 高 |

---

### 6.2 SQLite固有エラーの変換

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-DB-021 | SQLiteの制約違反がDuplicateKeyExceptionに変換される | なし | SqliteException(SQLITE_CONSTRAINT) | DuplicateKeyException, ErrorCode=DUPLICATE_KEY | 高 |
| TC-DB-022 | SQLiteのロックがDatabaseLockedExceptionに変換される | なし | SqliteException(SQLITE_BUSY) | DatabaseLockedException, ErrorCode=DATABASE_LOCKED | 高 |
| TC-DB-023 | SQLiteのオープン失敗がConnectionFailureExceptionに変換される | なし | SqliteException(SQLITE_CANTOPEN) | ConnectionFailureException, ErrorCode=CONNECTION_FAILURE | 高 |

---

### 6.3 SQL Server固有エラーの変換

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-DB-024 | SQL Serverの一意制約違反がDuplicateKeyExceptionに変換される | なし | SqlException(2627) | DuplicateKeyException, ErrorCode=DUPLICATE_KEY | 高 |
| TC-DB-025 | SQL ServerのタイムアウトがQueryTimeoutExceptionに変換される | なし | SqlException(-2) | QueryTimeoutException, ErrorCode=QUERY_TIMEOUT | 高 |
| TC-DB-026 | SQL Serverのログイン失敗がConnectionFailureExceptionに変換される | なし | SqlException(18456) | ConnectionFailureException, ErrorCode=CONNECTION_FAILURE | 高 |

---

## 7. 秘密情報マスキングのテストケース

### 7.1 接続文字列のマスキング

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-DB-027 | パスワードがマスキングされる | なし | "Host=localhost;Password=abc123" | "Host=localhost;Password=***" | 高 |
| TC-DB-028 | APIキーがマスキングされる | なし | "ApiKey=sk_test_123" | "ApiKey=***" | 高 |
| TC-DB-029 | トークンがマスキングされる | なし | "Token=eyJhbGci..." | "Token=***" | 高 |
| TC-DB-030 | 秘密情報がない場合、マスキングされない | なし | "Host=localhost;Database=mydb" | そのまま出力 | 中 |

---

## 8. テスト実装ガイドライン

### 8.1 テストツール推奨

| 言語 | 推奨ツール |
|------|----------|
| C# / .NET | xUnit, FluentAssertions, Moq |

---

### 8.2 Mockの使用方針

1. **DB接続のMock**: 単体テストではDB接続をMock化してDBへの依存をなくす
2. **例外のMock**: RDBMS固有の例外をMockで生成してエラー変換をテスト
3. **統合テスト**: SQLiteのインメモリDBを使用して実際の接続・トランザクションを確認

---

### 8.3 テストデータ管理

1. **固定テストデータ**: 各テストケースで使用するデータを明確に定義
2. **テストデータの独立性**: テスト間でデータを共有しない
3. **インメモリDB**: 統合テストはSQLiteのインメモリDBを使用

---

## 9. テスト実行計画

### 9.1 実行順序

1. ファクトリ切り替えのテスト（TC-DB-001 〜 TC-DB-005）
2. 接続管理のテスト（TC-DB-006 〜 TC-DB-011）
3. トランザクション管理のテスト（TC-DB-012 〜 TC-DB-017）
4. エラー変換のテスト（TC-DB-018 〜 TC-DB-026）
5. 秘密情報マスキングのテスト（TC-DB-027 〜 TC-DB-030）

---

### 9.2 テスト環境

| 項目 | 要件 |
|------|------|
| 単体テスト | Moqでスタブ化 |
| 統合テスト | SQLiteインメモリDB |
| 外部依存 | Moqでスタブ化 |

---

## 10. 参考

- [DB接続管理設計](database-connection.md)
- [エラーハンドリング設計](error-handling.md)
- xUnit 公式ドキュメント
- FluentAssertions 公式ドキュメント
