// このファイルは設計書とコードの同期のためのサンプルコードです
// 実際のコードは Features/Demo/Services/NPlusOneService.cs にあります

namespace BlazorApp.Features.Demo.Services;

using Microsoft.Data.Sqlite;
using System.Diagnostics;

/// <summary>
/// N+1問題のデモ実装
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/n-plus-one-demo/internal-design.md</para>
/// <para><strong>責務:</strong> N+1問題のBad版とGood版を実装し、実行時間とクエリ回数を測定する</para>
/// <para><strong>依存関係:</strong></para>
/// <list type="bullet">
/// <item><description>IConfiguration: 接続文字列取得</description></item>
/// <item><description>ILogger&lt;NPlusOneService&gt;: ログ出力</description></item>
/// </list>
/// <para><strong>教育目的:</strong> ループ内でのSQL実行がパフォーマンスに与える影響を可視化</para>
/// </remarks>
public class NPlusOneService : INPlusOneService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NPlusOneService> _logger;
    private int _sqlQueryCount = 0;

    public NPlusOneService(IConfiguration configuration, ILogger<NPlusOneService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// N+1問題版（非効率な実装）
    /// </summary>
    /// <returns>実行結果（実行時間、クエリ回数、データ）</returns>
    /// <remarks>
    /// <para><strong>アルゴリズム:</strong></para>
    /// <list type="number">
    /// <item><description>Stopwatch.Start()</description></item>
    /// <item><description>Usersテーブルから全ユーザー取得（1回目のクエリ）</description></item>
    /// <item><description>各ユーザーごとにループ: Departmentsテーブルから取得（N回のクエリ）</description></item>
    /// <item><description>Stopwatch.Stop()</description></item>
    /// <item><description>NPlusOneResponseを生成して返却</description></item>
    /// </list>
    /// <para><strong>SQL実行回数:</strong> 101回（1回のUsers取得 + 100回のDepartments取得）</para>
    /// <para><strong>期待実行時間:</strong> 約45ms</para>
    /// <para><strong>SQL文（1回目）:</strong></para>
    /// <code>
    /// SELECT Id, Name, DepartmentId, Email FROM Users;
    /// </code>
    /// <para><strong>SQL文（2回目以降、ループ内で100回実行）:</strong></para>
    /// <code>
    /// SELECT Id, Name FROM Departments WHERE Id = @DeptId;
    /// </code>
    /// <para><strong>問題点:</strong> ループ内でSQLを実行することで、ネットワーク遅延が100回発生</para>
    /// </remarks>
    public async Task<NPlusOneResponse> GetUsersBad()
    {
        var sw = Stopwatch.StartNew();
        _sqlQueryCount = 0;
        var users = new List<UserWithDepartment>();

        using var connection = GetConnection();
        await connection.OpenAsync();

        // 1回目のクエリ: Usersテーブルから全ユーザー取得
        var usersCommand = new SqliteCommand("SELECT Id, Name, DepartmentId, Email FROM Users", connection);
        _sqlQueryCount++;

        using (var reader = await usersCommand.ExecuteReaderAsync())
        {
            var usersList = new List<(int Id, string Name, int DepartmentId, string Email)>();
            while (await reader.ReadAsync())
            {
                usersList.Add((reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3)));
            }
            reader.Close();

            // N回のクエリ: ループ内で部署情報を個別に取得（N+1問題）
            foreach (var user in usersList)
            {
                var deptCommand = new SqliteCommand("SELECT Id, Name FROM Departments WHERE Id = @DeptId", connection);
                deptCommand.Parameters.AddWithValue("@DeptId", user.DepartmentId);
                _sqlQueryCount++;

                using var deptReader = await deptCommand.ExecuteReaderAsync();
                if (await deptReader.ReadAsync())
                {
                    users.Add(new UserWithDepartment
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Department = new DepartmentInfo
                        {
                            Id = deptReader.GetInt32(0),
                            Name = deptReader.GetString(1)
                        }
                    });
                }
            }
        }

        sw.Stop();
        _logger.LogInformation("N+1 bad executed: {QueryCount} queries, {ExecutionTimeMs}ms", _sqlQueryCount, sw.ElapsedMilliseconds);

        return new NPlusOneResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            SqlCount = _sqlQueryCount,
            DataSize = users.Count * 100,
            RowCount = users.Count,
            Message = $"N+1問題あり: ループ内で部署情報を{users.Count}回個別に取得しています（合計{_sqlQueryCount}クエリ）",
            Data = users
        };
    }

    /// <summary>
    /// N+1問題版（最適化済み）
    /// </summary>
    /// <returns>実行結果（実行時間、クエリ回数、データ）</returns>
    /// <remarks>
    /// <para><strong>アルゴリズム:</strong> JOINで一括取得</para>
    /// <para><strong>SQL文:</strong></para>
    /// <code>
    /// SELECT
    ///     u.Id,
    ///     u.Name,
    ///     u.Email,
    ///     d.Id AS DeptId,
    ///     d.Name AS DeptName
    /// FROM Users u
    /// INNER JOIN Departments d ON u.DepartmentId = d.Id;
    /// </code>
    /// <para><strong>SQL実行回数:</strong> 1回</para>
    /// <para><strong>期待実行時間:</strong> 約12ms</para>
    /// <para><strong>改善点:</strong> JOINにより1回のクエリで全データを取得、ネットワーク遅延が1回のみ</para>
    /// </remarks>
    public async Task<NPlusOneResponse> GetUsersGood()
    {
        var sw = Stopwatch.StartNew();
        _sqlQueryCount = 0;
        var users = new List<UserWithDepartment>();

        using var connection = GetConnection();
        await connection.OpenAsync();

        // 1回のクエリ: UsersとDepartmentsをJOINして一括取得
        var sql = @"
            SELECT
                u.Id,
                u.Name,
                u.Email,
                d.Id AS DeptId,
                d.Name AS DeptName
            FROM Users u
            INNER JOIN Departments d ON u.DepartmentId = d.Id";

        var command = new SqliteCommand(sql, connection);
        _sqlQueryCount++;

        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                users.Add(new UserWithDepartment
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Department = new DepartmentInfo
                    {
                        Id = reader.GetInt32(3),
                        Name = reader.GetString(4)
                    }
                });
            }
        }

        sw.Stop();
        _logger.LogInformation("N+1 good executed: {QueryCount} queries, {ExecutionTimeMs}ms", _sqlQueryCount, sw.ElapsedMilliseconds);

        return new NPlusOneResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            SqlCount = _sqlQueryCount,
            DataSize = users.Count * 100,
            RowCount = users.Count,
            Message = $"最適化済み: 1回のJOINクエリで全データを取得しています",
            Data = users
        };
    }

    /// <summary>
    /// データベース接続を取得
    /// </summary>
    /// <returns>SqliteConnection</returns>
    /// <remarks>
    /// <para><strong>接続文字列:</strong> appsettings.json の ConnectionStrings:DemoDatabase を使用</para>
    /// <para><strong>データベース:</strong> demo.db (SQLite)</para>
    /// </remarks>
    private SqliteConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("DemoDatabase");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DemoDatabase' not found");
        }
        return new SqliteConnection(connectionString);
    }
}

/// <summary>
/// N+1問題デモサービスのインターフェース
/// </summary>
public interface INPlusOneService
{
    /// <summary>
    /// N+1問題版（非効率な実装）
    /// </summary>
    Task<NPlusOneResponse> GetUsersBad();

    /// <summary>
    /// N+1問題版（最適化済み）
    /// </summary>
    Task<NPlusOneResponse> GetUsersGood();
}

// DTO定義は省略（Models/ フォルダに配置）
