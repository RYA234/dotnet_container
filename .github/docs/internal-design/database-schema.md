# データベース設計（物理）

## テーブル定義

### Users（ユーザー）

**用途**: エンジニア教育用デモ - N+1問題

**DDL**:
```sql
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    DepartmentId INTEGER NOT NULL,
    Email TEXT NOT NULL UNIQUE,
    FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
);
```

**カラム定義**:

| カラム名 | 型 | NULL | デフォルト | インデックス | 説明 |
|---------|-----|------|----------|------------|------|
| Id | INTEGER | NOT NULL | AUTOINCREMENT | PK | ユーザーID |
| Name | TEXT | NOT NULL | - | - | ユーザー名 |
| DepartmentId | INTEGER | NOT NULL | - | FK | 部署ID |
| Email | TEXT | NOT NULL | - | UNIQUE | メールアドレス |

**制約**:
- **PRIMARY KEY**: Id
- **FOREIGN KEY**: DepartmentId → Departments(Id)
- **UNIQUE**: Email

---

### Departments（部署）

**用途**: エンジニア教育用デモ - N+1問題

**DDL**:
```sql
CREATE TABLE Departments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL
);
```

**カラム定義**:

| カラム名 | 型 | NULL | デフォルト | インデックス | 説明 |
|---------|-----|------|----------|------------|------|
| Id | INTEGER | NOT NULL | AUTOINCREMENT | PK | 部署ID |
| Name | TEXT | NOT NULL | - | - | 部署名 |

**制約**:
- **PRIMARY KEY**: Id

---

## インデックス設計

| テーブル名 | インデックス名 | カラム | 種類 | 目的 |
|----------|-------------|--------|------|------|
| Users | PK_Users | Id | PRIMARY KEY | 主キー |
| Users | UQ_Users_Email | Email | UNIQUE | メール重複防止 |
| Users | FK_Users_DepartmentId | DepartmentId | FOREIGN KEY | 部署との関連 |
| Departments | PK_Departments | Id | PRIMARY KEY | 主キー |

**パフォーマンス考慮**:
- **DepartmentId**: FKインデックスにより `JOIN` が高速化
- **Email**: UNIQUEインデックスにより重複チェックが高速化

---

## 初期データ

### Departments（5件）

```sql
INSERT INTO Departments (Id, Name) VALUES
(1, 'Engineering'),
(2, 'Sales'),
(3, 'Marketing'),
(4, 'HR'),
(5, 'Finance');
```

### Users（100件）

**データ生成ロジック**:
```csharp
for (int i = 1; i <= 100; i++)
{
    int deptId = ((i - 1) % 5) + 1; // 1〜5を繰り返し
    string sql = "INSERT INTO Users (Name, DepartmentId, Email) VALUES " +
                 $"('User {i}', {deptId}, 'user{i}@example.com')";
    // 実行
}
```

**データ分布**:
- Engineering: 20件
- Sales: 20件
- Marketing: 20件
- HR: 20件
- Finance: 20件

---

## SQLiteの特性

### 型システム
- **動的型付け**: カラムに異なる型を格納可能（推奨しない）
- **アフィニティ**: TEXT, INTEGER, REAL, BLOB, NUMERIC

### AUTO INCREMENT
- **AUTOINCREMENT**: 削除された行のIDを再利用しない
- **PRIMARY KEY**: 省略すると削除された行のIDを再利用する可能性がある

### FOREIGN KEY制約
- **デフォルト無効**: `PRAGMA foreign_keys = ON;` で有効化
- **本デモでは無効**: 教育用のため削除時の制約チェックを省略

---

## データベースファイル

### demo.db
- **場所**: プロジェクトルート
- **サイズ**: 約50KB
- **バージョン管理**: .gitignore に追加（初期データはスクリプトで再生成）

### 初期化スクリプト
```csharp
public static void InitializeDatabase(string connectionString)
{
    using (var connection = new SqliteConnection(connectionString))
    {
        connection.Open();

        // テーブル作成
        var createDepartmentsTable = @"
            CREATE TABLE IF NOT EXISTS Departments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL
            )";
        new SqliteCommand(createDepartmentsTable, connection).ExecuteNonQuery();

        var createUsersTable = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                DepartmentId INTEGER NOT NULL,
                Email TEXT NOT NULL UNIQUE,
                FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
            )";
        new SqliteCommand(createUsersTable, connection).ExecuteNonQuery();

        // 初期データ投入
        // ...
    }
}
```

---

## パフォーマンス最適化

### N+1問題のクエリ比較

#### Bad版（101回のクエリ）
```sql
-- 1回目
SELECT Id, Name, DepartmentId, Email FROM Users;

-- 2回目以降（ループ内で100回実行）
SELECT Id, Name FROM Departments WHERE Id = 1;
SELECT Id, Name FROM Departments WHERE Id = 2;
SELECT Id, Name FROM Departments WHERE Id = 3;
-- ... 100回
```

#### Good版（1回のクエリ）
```sql
SELECT
    u.Id,
    u.Name,
    u.Email,
    d.Id AS DeptId,
    d.Name AS DeptName
FROM Users u
INNER JOIN Departments d ON u.DepartmentId = d.Id;
```

**性能差**:
- **Bad版**: 約45ms（環境により変動）
- **Good版**: 約12ms（Bad版の1/4）
- **クエリ回数**: 101回 vs 1回

---

## トランザクション管理

### 現在の実装
- **読み取り専用**: トランザクション不使用
- **理由**: デモデータは更新されないため

### 将来の基幹システム（在庫管理等）
```csharp
using (var transaction = connection.BeginTransaction())
{
    try
    {
        // 在庫更新
        // 売上記録
        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

---

## バックアップ・リカバリ

### バックアップ
- **不要**: デモデータは初期化スクリプトで再生成可能
- **本番（Supabase）**: 自動バックアップ（日次）

### リカバリ
```bash
# demo.db を削除
rm demo.db

# アプリ起動時に自動再生成
dotnet run
```

---

## 参考

- [論理DB設計](../external-design/database-logical.md)
- [クラス設計](class-design.md)
- [ADR-001: SQLiteを教育用デモに採用](../adr/001-use-sqlite-for-education.md)
