# DB接続管理設計

## 文書情報
- **作成日**: 2025-12-12
- **最終更新**: 2025-12-12
- **バージョン**: 1.0
- **ステータス**: 実装中

---

## 1. 概要

### 1.1 目的

異なるRDBMS（PostgreSQL、SQLite、SQL Server等）を設定で簡単に切り替え可能な、共通のDB接続管理機能を提供する。

### 1.2 設計方針

| 方針 | 説明 |
|------|------|
| **抽象化** | RDBMS固有の実装を隠蔽し、共通インターフェースで操作可能にする |
| **設定駆動** | 設定ファイルまたは環境変数でRDBMSを切り替え可能にする |
| **接続プーリング** | 各RDBMSの接続プールを適切に管理する |
| **トランザクション管理** | 統一されたトランザクション管理APIを提供する |
| **エラーハンドリング** | RDBMS固有のエラーを共通例外に変換する |

---

## 2. アーキテクチャ

### 2.1 クラス構成図

```
┌─────────────────────────────────────────────────────────┐
│                    Application Layer                     │
│  (Service, Repository がインターフェースを使用)            │
└─────────────────────────────────────────────────────────┘
                            │
                            │ 依存
                            ▼
┌─────────────────────────────────────────────────────────┐
│                  IDbConnectionFactory                    │
│  + CreateConnection(): IDbConnection                     │
│  + BeginTransaction(): IDbTransaction                    │
└─────────────────────────────────────────────────────────┘
                            △
                            │ 実装
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ PostgreSqlConn  │ │  SqliteConn     │ │ SqlServerConn   │
│ ectionFactory   │ │  ectionFactory  │ │ ectionFactory   │
└─────────────────┘ └─────────────────┘ └─────────────────┘
        │                   │                   │
        │                   │                   │
        ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│    Npgsql       │ │  Microsoft      │ │  Microsoft      │
│  (PostgreSQL)   │ │  .Data.Sqlite   │ │  .Data.SqlCli.. │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

---

## 3. インターフェース設計

### 3.1 DbConnectionFactory（接続ファクトリ）

すべてのRDBMS実装が実装する共通インターフェース。

#### プロパティ

| プロパティ | 説明 |
|----------|------|
| DatabaseType | 使用中のRDBMS種別（"PostgreSQL", "SQLite", "SqlServer"） |

#### メソッド

| メソッド | 説明 |
|---------|------|
| CreateConnection() | 新しいDB接続を作成して返す（未オープン状態） |
| CreateOpenConnection() | 新しいDB接続を作成し、オープン状態で返す |
| BeginTransaction() | 新しいトランザクションを開始 |
| BeginTransaction(IsolationLevel) | 指定した分離レベルでトランザクションを開始 |
| ExecuteInTransaction(operation) | トランザクション内で処理を実行し、自動的にコミット/ロールバック |
| TestConnection() | 接続テスト（接続可能かどうかを確認） |

---

### 3.2 DbConnectionWrapper（接続ラッパー）

DB接続のラッパーインターフェース（必要に応じて拡張機能を提供）。

#### メソッド

| メソッド | 説明 |
|---------|------|
| QueryAsync(sql, param) | SQL実行してオブジェクトのリストを返す |
| QuerySingleAsync(sql, param) | SQL実行して単一オブジェクトを返す |
| ExecuteAsync(sql, param) | SQL実行して影響を受けた行数を返す |
| ExecuteScalarAsync(sql, param) | SQL実行してスカラー値を返す |

**備考**: Dapper など既存のマイクロORMを使う場合、このインターフェースは不要。

---

## 4. 実装クラス設計

### 4.1 PostgreSqlConnectionFactory

PostgreSQL用の接続ファクトリ。

#### 接続文字列形式

```
Host=localhost;Port=5432;Database=mydb;Username=myuser;Password=mypassword;Pooling=true;MinPoolSize=1;MaxPoolSize=20;
```

#### 実装ポイント
- 接続プールを利用
- PostgreSQL固有のエラーを共通例外に変換
- トランザクション分離レベルのマッピング

---

### 4.2 SqliteConnectionFactory

SQLite用の接続ファクトリ。

#### 接続文字列形式

```
Data Source=app.db;Cache=Shared;
```

#### 実装ポイント
- SQLiteはファイルベースのため、接続プーリングは限定的
- デフォルトでプーリング有効
- トランザクション分離レベルはSerializableのみサポート
- Foreign Key制約を有効化する場合はPRAGMAで設定

---

### 4.3 SqlServerConnectionFactory

SQL Server用の接続ファクトリ。

#### 接続文字列形式

```
Server=localhost;Database=mydb;User Id=myuser;Password=mypassword;TrustServerCertificate=True;
```

#### 実装ポイント
- 接続プールを利用
- SQL Server固有のエラーを共通例外に変換
- Azure SQL Database のサポート（接続文字列の違いのみ）

---

## 5. 設定管理

### 5.1 設定ファイル構造

設定ファイルに以下の構造でDB接続設定を定義する。

| 設定キー | 説明 | 例 |
|---------|------|-----|
| Database.Provider | 使用するRDBMS種別 | "PostgreSQL", "SQLite", "SqlServer" |
| Database.ConnectionStrings.PostgreSQL | PostgreSQL接続文字列 | "Host=localhost;Port=5432;Database=mydb;..." |
| Database.ConnectionStrings.SQLite | SQLite接続文字列 | "Data Source=app.db;Cache=Shared;" |
| Database.ConnectionStrings.SqlServer | SQL Server接続文字列 | "Server=localhost;Database=mydb;..." |
| Database.ConnectionPool.MinPoolSize | 最小接続プールサイズ | 1 |
| Database.ConnectionPool.MaxPoolSize | 最大接続プールサイズ | 20 |
| Database.ConnectionPool.ConnectionTimeout | 接続タイムアウト（秒） | 30 |

---

### 5.2 環境変数による上書き

環境変数で接続情報を上書き可能にする（本番環境でのセキュリティ対策）。

| 環境変数名 | 説明 | 例 |
|-----------|------|-----|
| DATABASE_PROVIDER | 使用するRDBMS種別 | `PostgreSQL` |
| DATABASE_CONNECTION_STRING | 接続文字列（Providerに対応） | `Host=prod-db;...` |
| DATABASE_MIN_POOL_SIZE | 最小接続プールサイズ | `5` |
| DATABASE_MAX_POOL_SIZE | 最大接続プールサイズ | `50` |

---

## 6. ファクトリ登録（DI設定）

### 6.1 依存性注入の登録

アプリケーション起動時に、設定に応じて適切なファクトリを登録する。

#### 登録の流れ

1. 設定ファイルから `Database.Provider` を読み込む
2. Provider の値に応じて適切なファクトリクラスをインスタンス化
3. DIコンテナにファクトリを登録

#### Provider と実装クラスのマッピング

| Provider値 | 実装クラス |
|-----------|----------|
| "PostgreSQL" | PostgreSqlConnectionFactory |
| "SQLite" | SqliteConnectionFactory |
| "SqlServer" | SqlServerConnectionFactory |

---

### 6.2 サービスライフタイム

| サービス | ライフタイム | 理由 |
|---------|------------|------|
| DbConnectionFactory | Singleton | 接続プール管理のため、アプリケーション全体で1つのインスタンスを共有 |
| DbConnection | Scoped または Transient | リクエストごとに新しい接続を作成し、処理完了後にDispose |

---

## 7. トランザクション管理

### 7.1 基本パターン

#### パターン1: 手動トランザクション管理

1. 接続をオープン
2. トランザクションを開始
3. 複数のSQL操作を実行
4. 成功時はコミット、失敗時はロールバック
5. 接続とトランザクションをDispose

#### パターン2: ファクトリのヘルパーメソッド使用

1. ファクトリの `ExecuteInTransaction` メソッドを呼び出す
2. トランザクション内で実行する処理を渡す
3. 自動的にコミット/ロールバックが実行される

---

### 7.2 分離レベル

各RDBMSでサポートする分離レベルをマッピングする。

| 分離レベル | PostgreSQL | SQLite | SQL Server |
|-----------|-----------|--------|-----------|
| ReadUncommitted | ✅ | ❌ | ✅ |
| ReadCommitted | ✅ (デフォルト) | ❌ | ✅ (デフォルト) |
| RepeatableRead | ✅ | ❌ | ✅ |
| Serializable | ✅ | ✅ (デフォルト) | ✅ |
| Snapshot | ❌ | ❌ | ✅ |

**備考**: SQLiteはSerializableのみサポート。他の分離レベルを指定した場合はエラーにせず、Serializableで動作させる。

---

## 8. エラーハンドリング

### 8.1 RDBMS固有例外の変換

各RDBMSの固有例外を共通例外に変換する。

| RDBMS | 固有例外 | 共通例外への変換 |
|-------|---------|---------------|
| PostgreSQL | NpgsqlException | DatabaseException |
| SQLite | SqliteException | DatabaseException |
| SQL Server | SqlException | DatabaseException |

---

### 8.2 エラーコードマッピング

RDBMS固有のエラーコードを共通のエラーコードに変換する。

#### PostgreSQL

| PostgreSQLエラーコード | 説明 | 共通エラーコード |
|---------------------|------|---------------|
| 23505 | Unique violation | DUPLICATE_KEY |
| 23503 | Foreign key violation | FOREIGN_KEY_VIOLATION |
| 08000-08006 | Connection failure | CONNECTION_FAILURE |
| 57014 | Query timeout | QUERY_TIMEOUT |

#### SQLite

| SQLiteエラーコード | 説明 | 共通エラーコード |
|------------------|------|---------------|
| SQLITE_CONSTRAINT | Constraint violation | DUPLICATE_KEY or FOREIGN_KEY_VIOLATION |
| SQLITE_BUSY | Database locked | DATABASE_LOCKED |
| SQLITE_CANTOPEN | Cannot open database | CONNECTION_FAILURE |

#### SQL Server

| SQLエラーコード | 説明 | 共通エラーコード |
|---------------|------|---------------|
| 2601, 2627 | Unique violation | DUPLICATE_KEY |
| 547 | Foreign key violation | FOREIGN_KEY_VIOLATION |
| -2 | Timeout | QUERY_TIMEOUT |
| 4060, 18456 | Login failure | CONNECTION_FAILURE |

---

### 8.3 カスタム例外階層

| 例外クラス | 説明 |
|----------|------|
| DatabaseException | 基底例外クラス |
| └─ ConnectionFailureException | 接続失敗 |
| └─ DuplicateKeyException | 一意制約違反 |
| └─ ForeignKeyViolationException | 外部キー制約違反 |
| └─ QueryTimeoutException | クエリタイムアウト |
| └─ DatabaseLockedException | データベースロック（SQLite専用） |

**備考**: 詳細は [エラーハンドリング設計](error-handling.md) を参照。

---

## 9. ログ出力

### 9.1 ログ出力ポイント

| イベント | ログレベル | 出力内容 |
|---------|----------|---------|
| 接続作成 | Debug | DatabaseType, ConnectionString (マスキング) |
| 接続オープン | Debug | DatabaseType, 接続時間（ms） |
| トランザクション開始 | Debug | DatabaseType, IsolationLevel |
| トランザクションコミット | Information | DatabaseType, 実行時間（ms） |
| トランザクションロールバック | Warning | DatabaseType, 例外メッセージ |
| SQL実行 | Debug | SQL文, パラメータ（秘密情報マスキング） |
| SQL実行完了 | Information | SQL文（最初の50文字）, 実行時間（ms） |
| 遅いクエリ検出 | Warning | SQL文, 実行時間（ms）, 閾値 |
| エラー発生 | Error | DatabaseType, SQL文, エラーメッセージ, スタックトレース |

**備考**: 詳細は [ログ設計](logging.md) を参照。

---

### 9.2 秘密情報のマスキング

接続文字列やSQLパラメータに含まれる秘密情報をマスキングする。

| マスキング対象 | パターン例 | マスキング後 |
|-------------|----------|-----------|
| パスワード | `Password=abc123` | `Password=***` |
| APIキー | `ApiKey=sk_test_123` | `ApiKey=***` |
| トークン | `Token=eyJhbGci...` | `Token=***` |

---

## 10. パフォーマンス最適化

### 10.1 接続プーリング

各RDBMSの接続プールを適切に設定する。

| RDBMS | プーリング設定方法 | 推奨設定 |
|-------|---------------|---------|
| PostgreSQL | 接続文字列で指定 | `Pooling=true;MinPoolSize=1;MaxPoolSize=20;` |
| SQLite | デフォルトで有効 | `Cache=Shared;` |
| SQL Server | デフォルトで有効 | `Pooling=true;Min Pool Size=1;Max Pool Size=100;` |

---

### 10.2 接続タイムアウト

| RDBMS | タイムアウト設定 | 推奨値 |
|-------|--------------|-------|
| PostgreSQL | `Timeout=30` | 30秒 |
| SQLite | `Default Timeout=30` | 30秒 |
| SQL Server | `Connect Timeout=30` | 30秒 |

---

### 10.3 コマンドタイムアウト

長時間実行されるクエリのタイムアウトを設定する。

| 処理種別 | 推奨タイムアウト |
|---------|--------------|
| 通常のSELECT | 30秒 |
| 複雑な集計クエリ | 60秒 |
| バッチINSERT/UPDATE | 120秒 |

---

## 11. テスト戦略

### 11.1 単体テスト

| テスト対象 | テスト方法 |
|----------|----------|
| ファクトリの切り替え | 設定を変更してPostgreSQL/SQLite/SqlServerファクトリが正しく作成されることを確認 |
| 接続作成 | Mock接続を使って、CreateConnection() が正しく動作することを確認 |
| トランザクション管理 | トランザクション開始→コミット/ロールバックが正しく動作することを確認 |
| エラー変換 | RDBMS固有例外が共通例外に変換されることを確認 |

---

### 11.2 統合テスト

| テスト対象 | テスト方法 |
|----------|----------|
| 実DB接続テスト | PostgreSQL, SQLite, SQL Server に実際に接続できることを確認 |
| トランザクション動作 | 実DBでコミット/ロールバックが正しく動作することを確認 |
| 接続プール動作 | 複数接続を同時に作成し、プールが正しく機能することを確認 |
| エラーハンドリング | 実DBで制約違反を発生させ、エラー変換が正しく動作することを確認 |

---

### 11.3 テストデータベース

| 環境 | 使用DB |
|------|-------|
| ローカル開発 | SQLite (ファイルベース、簡単にセットアップ可能) |
| CI/CD | PostgreSQL (Docker コンテナで起動) |
| 統合テスト | PostgreSQL, SQLite, SQL Server すべてでテスト実行 |

---

## 12. マイグレーション戦略

### 12.1 スキーマ管理ツール

RDBMS間でのスキーマ互換性を保つため、マイグレーションツールを使用する。

| ツール | 対応RDBMS | 特徴 |
|-------|----------|------|
| **FluentMigrator** | PostgreSQL, SQLite, SQL Server | コードベースのマイグレーション |
| **DbUp** | PostgreSQL, SQLite, SQL Server | SQLスクリプトベースのマイグレーション |
| **EF Core Migrations** | PostgreSQL, SQLite, SQL Server | Entity Framework Coreのマイグレーション機能 |

---

### 12.2 RDBMS別マイグレーション

RDBMS固有の機能を使う場合は、条件分岐でマイグレーションを実行する。

#### RDBMS固有機能の例

| RDBMS | 固有機能 | 備考 |
|-------|---------|------|
| PostgreSQL | UUID拡張機能 | CREATE EXTENSION で有効化 |
| SQLite | PRAGMA設定 | Foreign Key制約など |
| SQL Server | スキーマ機能 | デフォルトスキーマの設定 |

---

## 13. 使用パターン

### 13.1 Repositoryパターン

#### 基本的な使用フロー

1. **コンストラクタでファクトリを受け取る**
   - DIコンテナからDbConnectionFactoryを注入

2. **単純なクエリ実行**
   - `CreateOpenConnection()` で接続を取得
   - パラメータ化クエリでSQLを実行
   - 接続を自動的にDispose

3. **トランザクションが必要な場合**
   - `ExecuteInTransaction()` メソッドを使用
   - 複数のSQL操作をトランザクション内で実行
   - 自動的にコミット/ロールバック

---

## 14. セキュリティ考慮事項

### 14.1 接続文字列の保護

| 環境 | 保護方法 |
|------|---------|
| 開発環境 | appsettings.Development.json (Gitignore) |
| 本番環境 | 環境変数 または AWS Secrets Manager / Azure Key Vault |

---

### 14.2 SQLインジェクション対策

| 対策 | 実装方法 |
|------|---------|
| パラメータ化クエリ | 必ずパラメータ化クエリを使用（文字列連結禁止） |
| 動的ソート | ホワイトリスト検証（[セキュリティ設計](security.md) 参照） |
| 入力検証 | SQLキーワードを含む入力値の検証 |

---

## 15. 参考資料

- [アーキテクチャ設計](architecture.md)
- [エラーハンドリング設計](error-handling.md)
- [セキュリティ設計](security.md)
- [ログ設計](logging.md)

---

## 16. 変更履歴

| バージョン | 日付 | 変更内容 |
|----------|------|---------|
| 1.0 | 2025-12-12 | 初版作成 |
