# ADR-002: データアクセス方法の使い分け（デモ = Raw SQL / 基幹システム = EF Core）

## ステータス
更新済み（2026-03-07 改訂）

## 日付
2025-12-10（初版） / 2026-03-07（改訂）

## コンテキスト
N+1問題のデモを実装する際、データベースアクセスの方法を決定する必要がある。
通常、ASP.NET CoreではEntity Framework Core (EF Core)が推奨されるが、教育目的を考慮して検討。

### 要件
- N+1問題を明確にデモできる
- 実際に何回クエリが発行されているか可視化できる
- 新人がSQLの重要性を理解できる

### 制約条件
- 教育用デモのため、実装の複雑さは許容
- 将来的な保守性よりも学習効果を優先

## 決定

機能の目的に応じてデータアクセス方法を使い分ける。

| 機能 | データアクセス方法 | 理由 |
|-----|-----------------|------|
| **デモ機能**（SQLアンチパターン） | ADO.NET（Raw SQL） | SQL を直接書くことで発行クエリを可視化し、学習効果を最大化する |
| **基幹システム機能** | EF Core（DbContext + LINQ） | 保守性・型安全性・マイグレーション管理のため |

**共通**: テーブル定義は EF Core の Entity クラスで一元管理する。デモ機能でも同じ Entity クラス・DbContext を使用し、`FromSqlRaw` で Raw SQL を実行する。

## 理由

### メリット
1. **クエリが見える**: 実際に発行されるSQL文が明確
2. **カウント可能**: クエリ回数を正確にカウントできる
3. **学習効果**: SQLの重要性を直接体感できる
4. **パフォーマンス最適化**: JOINの効果が明確に分かる
5. **デバッグしやすい**: ログでSQL文を確認できる

### デメリット
1. **コード量増加**: ORMに比べてボイラープレートコードが多い
   - **影響**: 教育用デモのためコード量は許容範囲
2. **型安全性の低下**: コンパイル時のチェックが弱い
   - **影響**: テストでカバー
3. **SQLインジェクションリスク**: 文字列連結を誤ると危険
   - **対策**: パラメータ化クエリを徹底

### 代替案との比較

| 手法 | クエリ可視化 | 学習効果 | 保守性 | 採用理由 |
|------|----------|---------|--------|---------|
| ADO.NET（素のSQL） | ⭐⭐⭐ | ⭐⭐⭐ | ⭐ | ✅ 教育効果が高い |
| Entity Framework Core | ⭐ | ⭐ | ⭐⭐⭐ | ❌ クエリが隠蔽される |
| Dapper | ⭐⭐ | ⭐⭐ | ⭐⭐ | △ 良いが、ライブラリ依存 |

### EF Coreでの問題点
```csharp
// EF Coreでは、N+1問題がわかりにくい
var users = await _context.Users.ToListAsync(); // 1回
foreach (var user in users)
{
    var dept = await _context.Departments
        .FirstAsync(d => d.Id == user.DepartmentId); // 内部で何回発行されるか不明瞭
}
```

### ADO.NETでの透明性
```csharp
// 素のSQLでは、クエリ回数が明確
_sqlQueryCount++; // カウント可能
var command = new SqlCommand("SELECT * FROM Users", connection);
using (var reader = await command.ExecuteReaderAsync())
{
    // ...
}
```

## 結果

### ポジティブな影響
- N+1問題のデモが非常にわかりやすい
- 実際のクエリ回数を正確に表示できる
- SQLの学習が進む
- パフォーマンスチューニングの感覚が身につく

### ネガティブな影響
- コード量が増える（約3倍）
- マイグレーション管理が手動
- リファクタリングが難しい

### トレードオフ
デモ機能は教育効果を最大化するため透明性を優先。基幹システム機能は保守性を優先して EF Core を使用。Repository パターンで層を分離しているため、両者が混在しても呼び出し元（Service）への影響はない。

### 実装例
```csharp
// Bad版: N+1問題
public async Task<NPlusOneResponse> GetUsersBad()
{
    _sqlQueryCount = 0;
    using (var connection = GetConnection())
    {
        await connection.OpenAsync();

        // 1回目のクエリ
        var usersCommand = new SqlCommand("SELECT Id, Name, DepartmentId, Email FROM Users", connection);
        _sqlQueryCount++; // カウント

        using (var reader = await usersCommand.ExecuteReaderAsync())
        {
            var users = new List<(int Id, string Name, int DepartmentId, string Email)>();
            while (await reader.ReadAsync())
            {
                users.Add((reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3)));
            }
            reader.Close();

            // N回のクエリ
            foreach (var user in users)
            {
                var deptCommand = new SqlCommand("SELECT Id, Name FROM Departments WHERE Id = @DeptId", connection);
                deptCommand.Parameters.AddWithValue("@DeptId", user.DepartmentId);
                _sqlQueryCount++; // カウント
                // ...
            }
        }
    }
}
```

## 関連 ADR
- ADR-001: SQLiteを教育用デモに採用
- ADR-005: Repository パターンを導入し DB 接続をデータアクセス層に限定する
