using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.Services;

namespace BlazorApp.Features.Demo.DatabaseConnection;

[Route("Demo")]
public class DatabaseConnectionController : Controller
{
    private readonly IDatabaseConnectionDemoService _dbConnectionDemoService;
    private readonly ILogger<DatabaseConnectionController> _logger;

    public DatabaseConnectionController(IDatabaseConnectionDemoService dbConnectionDemoService, ILogger<DatabaseConnectionController> logger)
    {
        _dbConnectionDemoService = dbConnectionDemoService;
        _logger = logger;
    }

    [Route("DatabaseConnection")]
    public IActionResult DatabaseConnection()
    {
        return View("~/Features/Demo/DatabaseConnection/Views/DatabaseConnection.cshtml");
    }

    [HttpGet("/api/demo/db/test")]
    public async Task<IActionResult> DbConnectionTest()
    {
        var result = await _dbConnectionDemoService.TestConnectionAsync();
        return Ok(new
        {
            isConnected = result.IsConnected,
            databaseType = result.DatabaseType,
            connectionString = result.ConnectionString,
            elapsedMs = result.ElapsedMs,
            message = result.IsConnected ? "接続成功" : "接続失敗"
        });
    }

    [HttpGet("/api/demo/db/tables")]
    public async Task<IActionResult> DbGetTables()
    {
        var tables = await _dbConnectionDemoService.GetTablesAsync();
        var tableList = tables.ToList();
        return Ok(new
        {
            tables = tableList,
            count = tableList.Count,
            message = $"{tableList.Count}件のテーブルが見つかりました"
        });
    }

    [HttpGet("/api/demo/db/count")]
    public async Task<IActionResult> DbGetRowCount([FromQuery] string table = "Users")
    {
        var count = await _dbConnectionDemoService.GetRowCountAsync(table);
        return Ok(new
        {
            tableName = table,
            rowCount = count,
            message = $"{table} テーブルに {count} 件のデータがあります"
        });
    }
}
