using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using BlazorApp.Features.Demo.Data;
using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

public class NPlusOneService : INPlusOneService
{
    private readonly DemoDbContext _context;
    private readonly ILogger<NPlusOneService> _logger;

    public NPlusOneService(DemoDbContext context, ILogger<NPlusOneService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NPlusOneResponse> GetUsersBad()
    {
        var sw = Stopwatch.StartNew();

        // N+1問題あり: ユーザーを取得後、ループ内で部署情報を取得
        var users = await _context.Users.ToListAsync(); // 1回のクエリ

        var result = new List<UserWithDepartment>();
        foreach (var user in users)
        {
            // 各ユーザーごとに部署情報を取得（N回のクエリ）
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == user.DepartmentId);

            result.Add(new UserWithDepartment
            {
                Id = user.Id,
                Name = user.Name,
                Department = department != null ? new DepartmentInfo
                {
                    Id = department.Id,
                    Name = department.Name
                } : new DepartmentInfo()
            });
        }

        sw.Stop();

        // InMemoryではクエリカウントが難しいため、ループ数から推測
        var sqlCount = 1 + users.Count; // 1回(Users取得) + N回(Department取得)

        return new NPlusOneResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            SqlCount = sqlCount,
            DataSize = System.Text.Json.JsonSerializer.Serialize(result).Length,
            RowCount = result.Count,
            Data = result,
            Message = $"N+1問題あり: ループ内で部署情報を{users.Count}回個別に取得しています（合計{sqlCount}クエリ）"
        };
    }

    public async Task<NPlusOneResponse> GetUsersGood()
    {
        var sw = Stopwatch.StartNew();

        // 最適化済み: Includeを使って1回のクエリでJOIN取得
        var users = await _context.Users
            .Include(u => u.Department)
            .ToListAsync();

        var result = users.Select(user => new UserWithDepartment
        {
            Id = user.Id,
            Name = user.Name,
            Department = user.Department != null ? new DepartmentInfo
            {
                Id = user.Department.Id,
                Name = user.Department.Name
            } : new DepartmentInfo()
        }).ToList();

        sw.Stop();

        return new NPlusOneResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            SqlCount = 1, // Includeで1回のクエリ
            DataSize = System.Text.Json.JsonSerializer.Serialize(result).Length,
            RowCount = result.Count,
            Data = result,
            Message = $"最適化済み: 1回のJOINクエリで全データを取得しています"
        };
    }
}
