using System.Data;
using BlazorApp.Shared.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BlazorApp.Tests.Shared.Data;

/// <summary>
/// SqliteConnectionFactory の単体テスト。
/// TC-DB-002, TC-DB-006, TC-DB-007, TC-DB-010, TC-DB-011 をカバーする。
/// </summary>
public class SqliteConnectionFactoryTests
{
    private readonly Mock<ILogger<SqliteConnectionFactory>> _mockLogger;

    public SqliteConnectionFactoryTests()
    {
        _mockLogger = new Mock<ILogger<SqliteConnectionFactory>>();
    }

    // ─── TC-DB-002: DatabaseType が "SQLite" を返す ───────────────────────────

    /// <summary>
    /// TC-DB-002: SQLite Provider の場合、DatabaseType が "SQLite" を返す
    /// </summary>
    [Fact]
    public void DatabaseType_ReturnsSQLite()
    {
        // Arrange
        var factory = new SqliteConnectionFactory("Data Source=:memory:", _mockLogger.Object);

        // Act
        var databaseType = factory.DatabaseType;

        // Assert
        databaseType.Should().Be("SQLite");
    }

    // ─── TC-DB-006: CreateConnection が未オープン状態の接続を返す ───────────────

    /// <summary>
    /// TC-DB-006: CreateConnection が ConnectionState.Closed の接続を返す
    /// </summary>
    [Fact]
    public void CreateConnection_ReturnsClosed()
    {
        // Arrange
        var factory = new SqliteConnectionFactory("Data Source=:memory:", _mockLogger.Object);

        // Act
        var connection = factory.CreateConnection();

        // Assert
        connection.State.Should().Be(ConnectionState.Closed);
        connection.Dispose();
    }

    // ─── TC-DB-007: 読み取り専用で存在しないファイルでは CreateOpenConnectionAsync が例外をスロー ──

    /// <summary>
    /// TC-DB-007: 読み取り専用モードで存在しないDBファイルを指定した場合、CreateOpenConnectionAsync が例外をスロー
    /// </summary>
    [Fact]
    public async Task CreateOpenConnectionAsync_ReadOnlyNonExistentFile_Throws()
    {
        // Arrange
        // Mode=ReadOnly かつ存在しないファイルを指定すると SQLite は例外をスロー
        var factory = new SqliteConnectionFactory(
            "Data Source=/nonexistent/db.sqlite;Mode=ReadOnly;",
            _mockLogger.Object);

        // Act
        var act = async () => await factory.CreateOpenConnectionAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    // ─── TC-DB-010: DB 起動中の場合、TestConnectionAsync が true を返す ─────────

    /// <summary>
    /// TC-DB-010: SQLite インメモリ DB に接続できる場合、TestConnectionAsync が true を返す
    /// </summary>
    [Fact]
    public async Task TestConnectionAsync_ValidDb_ReturnsTrue()
    {
        // Arrange
        var factory = new SqliteConnectionFactory("Data Source=:memory:", _mockLogger.Object);

        // Act
        var result = await factory.TestConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    // ─── TC-DB-011: DB 停止中の場合、TestConnectionAsync が false を返す ─────────

    /// <summary>
    /// TC-DB-011: 無効なパスの場合、TestConnectionAsync が false を返す
    /// （SQLite はファイルが存在しない場合でもデフォルトで作成するため、
    ///   読み取り専用フラグで書き込み不可のパスを使用して失敗を再現する）
    /// </summary>
    [Fact]
    public async Task TestConnectionAsync_InvalidConnectionString_ReturnsFalse()
    {
        // Arrange
        // Mode=ReadOnly かつ存在しないファイルを指定すると SQLite は例外をスロー
        var factory = new SqliteConnectionFactory(
            "Data Source=/nonexistent_readonly_path/db.sqlite;Mode=ReadOnly;",
            _mockLogger.Object);

        // Act
        var result = await factory.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    // ─── CreateOpenConnectionAsync: 正常系 ──────────────────────────────────

    /// <summary>
    /// CreateOpenConnectionAsync が ConnectionState.Open の接続を返す
    /// </summary>
    [Fact]
    public async Task CreateOpenConnectionAsync_ValidDb_ReturnsOpenConnection()
    {
        // Arrange
        var factory = new SqliteConnectionFactory("Data Source=:memory:", _mockLogger.Object);

        // Act
        var connection = await factory.CreateOpenConnectionAsync();

        // Assert
        connection.State.Should().Be(ConnectionState.Open);
        connection.Dispose();
    }
}
