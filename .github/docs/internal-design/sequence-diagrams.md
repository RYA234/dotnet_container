# シーケンス図

## N+1問題（Bad版）

```mermaid
sequenceDiagram
    participant Client as ブラウザ
    participant Controller as DemoController
    participant Service as NPlusOneService
    participant DB as SQLite

    Client->>Controller: GET /api/demo/n-plus-one/bad
    Controller->>Service: GetUsersBad()

    Service->>Service: Stopwatch.Start()
    Service->>Service: _sqlQueryCount = 0
    Service->>DB: SELECT * FROM Users
    DB-->>Service: 100 rows
    Service->>Service: _sqlQueryCount = 1

    loop 各ユーザー(100回)
        Service->>DB: SELECT * FROM Departments WHERE Id = ?
        DB-->>Service: 1 row
        Service->>Service: _sqlQueryCount++
        Service->>Service: UserWithDepartment生成
    end

    Service->>Service: Stopwatch.Stop()
    Service->>Service: NPlusOneResponse生成
    Service-->>Controller: NPlusOneResponse (sqlCount=101, executionTimeMs=45)
    Controller-->>Client: JSON Response
```

**問題点**:
- **101回のクエリ**: 1回（Users取得）+ 100回（Departments取得）
- **実行時間**: 約45ms（環境により変動）
- **ネットワーク遅延**: 各クエリごとに発生

---

## N+1問題（Good版）

```mermaid
sequenceDiagram
    participant Client as ブラウザ
    participant Controller as DemoController
    participant Service as NPlusOneService
    participant DB as SQLite

    Client->>Controller: GET /api/demo/n-plus-one/good
    Controller->>Service: GetUsersGood()

    Service->>Service: Stopwatch.Start()
    Service->>Service: _sqlQueryCount = 0
    Service->>DB: SELECT u.*, d.* FROM Users u INNER JOIN Departments d ON u.DepartmentId = d.Id
    DB-->>Service: 100 rows (with department data)
    Service->>Service: _sqlQueryCount = 1

    loop 各行
        Service->>Service: UserWithDepartment生成
    end

    Service->>Service: Stopwatch.Stop()
    Service->>Service: NPlusOneResponse生成
    Service-->>Controller: NPlusOneResponse (sqlCount=1, executionTimeMs=12)
    Controller-->>Client: JSON Response
```

**改善点**:
- **1回のクエリ**: JOINで一括取得
- **実行時間**: 約12ms（Bad版の1/4）
- **ネットワーク遅延**: 1回のみ

---

## Supabase接続テスト

```mermaid
sequenceDiagram
    participant Client as ブラウザ
    participant Controller as SupabaseController
    participant Service as SupabaseService
    participant Secrets as AWS Secrets Manager
    participant Supabase as Supabase API

    Client->>Controller: GET /supabase/test
    Controller->>Service: TestConnection()

    Service->>Secrets: GetSecretValueAsync("ecs/dotnet-container/supabase")
    Secrets-->>Service: {"url": "...", "anon_key": "..."}
    Service->>Service: JSON解析

    Service->>Supabase: GET {url}/auth/v1/health
    Note over Service,Supabase: Authorization: Bearer {anon_key}

    alt 接続成功
        Supabase-->>Service: 200 OK
        Service-->>Controller: {"success": true, "message": "Supabase connection successful"}
        Controller-->>Client: 200 OK
    else 接続失敗
        Supabase-->>Service: 500 Error
        Service-->>Controller: {"success": false, "message": "Connection failed: ..."}
        Controller-->>Client: 500 Internal Server Error
    end
```

**ポイント**:
- **秘密情報取得**: AWS Secrets Manager から取得
- **ヘルスチェック**: Supabase の `/auth/v1/health` を使用
- **エラーハンドリング**: 接続失敗時は500エラー

---

## エラー発生時のフロー

```mermaid
sequenceDiagram
    participant Client as ブラウザ
    participant Controller as DemoController
    participant Service as NPlusOneService
    participant DB as SQLite
    participant Logger as ILogger

    Client->>Controller: GET /api/demo/n-plus-one/bad

    Controller->>Service: GetUsersBad()
    Service->>DB: SELECT * FROM Users

    alt DB接続エラー
        DB-->>Service: SqlException
        Service-->>Controller: SqlException
        Controller->>Logger: LogError(ex, "Error in N+1 bad endpoint")
        Controller-->>Client: 500 {"error": "Database connection failed"}
    end
```

**エラー処理方針**:
- **Controller層**: try-catchでラップ、500エラー返却
- **Service層**: 例外をそのままスロー
- **ログ出力**: `ILogger.LogError()` で記録

---

## 初期化フロー（アプリ起動時）

```mermaid
sequenceDiagram
    participant Main as Program.cs
    participant DI as DI Container
    participant DB as demo.db

    Main->>Main: var builder = WebApplication.CreateBuilder(args)
    Main->>DI: builder.Services.AddScoped<INPlusOneService, NPlusOneService>()
    Main->>Main: var app = builder.Build()

    alt demo.db が存在しない
        Main->>DB: CREATE TABLE Users
        Main->>DB: CREATE TABLE Departments
        Main->>DB: INSERT INTO Departments (5件)
        Main->>DB: INSERT INTO Users (100件)
    end

    Main->>Main: app.Run()
```

**初期化処理**:
- **DI登録**: `AddScoped<INPlusOneService, NPlusOneService>()`
- **DBセットアップ**: demo.db が存在しない場合はテーブル作成とデータ投入

---

## 参考

- [クラス設計](class-design.md)
- [エラー処理設計](error-handling.md)
- [外部IF設計](../external-design/external-interface.md)
