using System.Diagnostics;
using System.Data;
using Microsoft.Data.Sqlite;
using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

public class NPlusOneService : INPlusOneService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NPlusOneService> _logger;
    private int _sqlQueryCount;

    public NPlusOneService(IConfiguration configuration, ILogger<NPlusOneService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private SqliteConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("NPlusOneDemo");
        return new SqliteConnection(connectionString);
    }

    private async Task EnsureDatabaseInitializedAsync(SqliteConnection connection)
    {
        var createTables = connection.CreateCommand();
        createTables.CommandText = @"
            CREATE TABLE IF NOT EXISTS Departments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                CreatedAt TEXT DEFAULT (datetime('now'))
            );
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                DepartmentId INTEGER NOT NULL,
                Email TEXT NOT NULL,
                CreatedAt TEXT DEFAULT (datetime('now')),
                FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
            );
            CREATE INDEX IF NOT EXISTS IX_Users_DepartmentId ON Users(DepartmentId);";
        await createTables.ExecuteNonQueryAsync();

        var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM Departments";
        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        if (count > 0) return;

        var seedDepts = connection.CreateCommand();
        seedDepts.CommandText = @"
            INSERT INTO Departments (Name) VALUES
                ('開発部'), ('営業部'), ('人事部'), ('総務部'), ('マーケティング部');";
        await seedDepts.ExecuteNonQueryAsync();

        var lastNames = new[] { "田中", "鈴木", "佐藤", "高橋", "伊藤", "渡辺", "山本", "中村", "小林", "加藤",
                                "吉田", "山田", "佐々木", "山口", "松本", "井上", "木村", "林", "斎藤", "清水" };
        var firstNames = new[] { "太郎", "花子", "次郎", "美咲", "健一", "恵子", "大輔", "裕子", "隆", "由美" };
        var values = new System.Text.StringBuilder();
        for (int i = 1; i <= 100; i++)
        {
            var name = lastNames[(i - 1) % lastNames.Length] + firstNames[(i - 1) % firstNames.Length];
            var deptId = ((i - 1) % 5) + 1;
            var email = $"user{i:D3}@example.com";
            if (i > 1) values.Append(',');
            values.Append($"('{name}', {deptId}, '{email}')");
        }
        var seedUsers = connection.CreateCommand();
        seedUsers.CommandText = $"INSERT INTO Users (Name, DepartmentId, Email) VALUES {values};";
        await seedUsers.ExecuteNonQueryAsync();
    }

    public async Task<NPlusOneResponse> GetUsersBad()
    {
        var sw = Stopwatch.StartNew();
        _sqlQueryCount = 0;
        var result = new List<UserWithDepartment>();

        using (var connection = GetConnection())
        {
            await connection.OpenAsync();
            await EnsureDatabaseInitializedAsync(connection);

            // N+1問題あり: ユーザーを取得後、ループ内で部署情報を取得
            var usersCommand = connection.CreateCommand();
            usersCommand.CommandText = "SELECT Id, Name, DepartmentId, Email FROM Users";
            _sqlQueryCount++; // 1回目のクエリ

            using (var reader = await usersCommand.ExecuteReaderAsync())
            {
                var users = new List<(int Id, string Name, int DepartmentId, string Email)>();
                while (await reader.ReadAsync())
                {
                    users.Add((
                        Convert.ToInt32(reader["Id"]),
                        reader["Name"].ToString() ?? "",
                        Convert.ToInt32(reader["DepartmentId"]),
                        reader["Email"].ToString() ?? ""
                    ));
                }
                reader.Close();

                // 各ユーザーごとに部署情報を取得（N回のクエリ）
                foreach (var user in users)
                {
                    var deptCommand = connection.CreateCommand();
                    deptCommand.CommandText = "SELECT Id, Name FROM Departments WHERE Id = @DeptId";
                    deptCommand.Parameters.AddWithValue("@DeptId", user.DepartmentId);
                    _sqlQueryCount++; // N回のクエリ

                    using (var deptReader = await deptCommand.ExecuteReaderAsync())
                    {
                        DepartmentInfo? department = null;
                        if (await deptReader.ReadAsync())
                        {
                            department = new DepartmentInfo
                            {
                                Id = Convert.ToInt32(deptReader["Id"]),
                                Name = deptReader["Name"].ToString() ?? ""
                            };
                        }

                        result.Add(new UserWithDepartment
                        {
                            Id = user.Id,
                            Name = user.Name,
                            Department = department ?? new DepartmentInfo()
                        });
                    }
                }
            }
        }

        sw.Stop();

        return new NPlusOneResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            SqlCount = _sqlQueryCount,
            DataSize = System.Text.Json.JsonSerializer.Serialize(result).Length,
            RowCount = result.Count,
            Data = result,
            Message = $"N+1問題あり: ループ内で部署情報を{result.Count}回個別に取得しています（合計{_sqlQueryCount}クエリ）"
        };
    }

    public async Task<NPlusOneResponse> GetUsersGood()
    {
        var sw = Stopwatch.StartNew();
        _sqlQueryCount = 0;
        var result = new List<UserWithDepartment>();

        using (var connection = GetConnection())
        {
            await connection.OpenAsync();
            await EnsureDatabaseInitializedAsync(connection);

            // 最適化済み: JOINを使って1回のクエリで全データ取得
            var sql = @"
                SELECT u.Id, u.Name, u.Email, d.Id AS DeptId, d.Name AS DeptName
                FROM Users u
                INNER JOIN Departments d ON u.DepartmentId = d.Id";

            var command = connection.CreateCommand();
            command.CommandText = sql;
            _sqlQueryCount++; // 1回のクエリ

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new UserWithDepartment
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Name = reader["Name"].ToString() ?? "",
                        Department = new DepartmentInfo
                        {
                            Id = Convert.ToInt32(reader["DeptId"]),
                            Name = reader["DeptName"].ToString() ?? ""
                        }
                    });
                }
            }
        }

        sw.Stop();

        return new NPlusOneResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            SqlCount = _sqlQueryCount,
            DataSize = System.Text.Json.JsonSerializer.Serialize(result).Length,
            RowCount = result.Count,
            Data = result,
            Message = $"最適化済み: 1回のJOINクエリで全データを取得しています"
        };
    }
}
