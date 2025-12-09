using System.Diagnostics;
using System.Data;
using Microsoft.Data.SqlClient;
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

    private SqlConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("DemoDatabase");
        return new SqlConnection(connectionString);
    }

    public async Task<NPlusOneResponse> GetUsersBad()
    {
        var sw = Stopwatch.StartNew();
        _sqlQueryCount = 0;
        var result = new List<UserWithDepartment>();

        using (var connection = GetConnection())
        {
            await connection.OpenAsync();

            // N+1問題あり: ユーザーを取得後、ループ内で部署情報を取得
            var usersCommand = new SqlCommand("SELECT Id, Name, DepartmentId, Email FROM Users", connection);
            _sqlQueryCount++; // 1回目のクエリ

            using (var reader = await usersCommand.ExecuteReaderAsync())
            {
                var users = new List<(int Id, string Name, int DepartmentId, string Email)>();
                while (await reader.ReadAsync())
                {
                    users.Add((
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetInt32(2),
                        reader.GetString(3)
                    ));
                }
                reader.Close();

                // 各ユーザーごとに部署情報を取得（N回のクエリ）
                foreach (var user in users)
                {
                    var deptCommand = new SqlCommand(
                        "SELECT Id, Name FROM Departments WHERE Id = @DeptId",
                        connection);
                    deptCommand.Parameters.AddWithValue("@DeptId", user.DepartmentId);
                    _sqlQueryCount++; // N回のクエリ

                    using (var deptReader = await deptCommand.ExecuteReaderAsync())
                    {
                        DepartmentInfo? department = null;
                        if (await deptReader.ReadAsync())
                        {
                            department = new DepartmentInfo
                            {
                                Id = deptReader.GetInt32(0),
                                Name = deptReader.GetString(1)
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

            // 最適化済み: JOINを使って1回のクエリで全データ取得
            var sql = @"
                SELECT u.Id, u.Name, u.Email, d.Id AS DeptId, d.Name AS DeptName
                FROM Users u
                INNER JOIN Departments d ON u.DepartmentId = d.Id";

            var command = new SqlCommand(sql, connection);
            _sqlQueryCount++; // 1回のクエリ

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new UserWithDepartment
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
