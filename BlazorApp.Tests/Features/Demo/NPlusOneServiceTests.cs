using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using BlazorApp.Features.Demo.Services;
using Microsoft.Data.SqlClient;

namespace BlazorApp.Tests.Features.Demo;

public class NPlusOneServiceTests : IDisposable
{
    private readonly NPlusOneService _service;
    private readonly string _connectionString;
    private readonly IConfiguration _configuration;

    public NPlusOneServiceTests()
    {
        // Use test database
        _connectionString = "Server=(localdb)\\mssqllocaldb;Database=DemoDbTest;Trusted_Connection=True;MultipleActiveResultSets=true";

        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ConnectionStrings:DemoDatabase", _connectionString }
        });
        _configuration = configBuilder.Build();

        var logger = new Mock<ILogger<NPlusOneService>>();
        _service = new NPlusOneService(_configuration, logger.Object);

        // Setup test database
        SetupTestDatabase();
    }

    private void SetupTestDatabase()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // Create database if it doesn't exist
        var createDbCommand = connection.CreateCommand();
        createDbCommand.CommandText = "IF DB_ID('DemoDbTest') IS NULL CREATE DATABASE DemoDbTest";
        try
        {
            createDbCommand.ExecuteNonQuery();
        }
        catch
        {
            // Database might already exist
        }

        connection.ChangeDatabase("DemoDbTest");

        // Drop tables if they exist
        var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = @"
            IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
            IF OBJECT_ID('Departments', 'U') IS NOT NULL DROP TABLE Departments;
        ";
        dropCommand.ExecuteNonQuery();

        // Create tables
        var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            CREATE TABLE Departments (
                Id INT PRIMARY KEY IDENTITY(1,1),
                Name NVARCHAR(100) NOT NULL,
                CreatedAt DATETIME2 DEFAULT GETDATE()
            );

            CREATE TABLE Users (
                Id INT PRIMARY KEY IDENTITY(1,1),
                Name NVARCHAR(100) NOT NULL,
                DepartmentId INT NOT NULL,
                Email NVARCHAR(255) NOT NULL,
                CreatedAt DATETIME2 DEFAULT GETDATE(),
                FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
            );
        ";
        createCommand.ExecuteNonQuery();

        // Insert test data
        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = @"
            INSERT INTO Departments (Name) VALUES ('開発部'), ('営業部'), ('人事部');

            INSERT INTO Users (Name, DepartmentId, Email) VALUES
            ('山田太郎', 1, 'yamada@example.com'),
            ('佐藤花子', 2, 'sato@example.com'),
            ('鈴木一郎', 1, 'suzuki@example.com'),
            ('高橋美咲', 3, 'takahashi@example.com'),
            ('田中健太', 1, 'tanaka@example.com');
        ";
        insertCommand.ExecuteNonQuery();
    }

    [Fact]
    public async Task GetUsersBad_ShouldReturnAllUsers()
    {
        // Act
        var result = await _service.GetUsersBad();

        // Assert
        result.Should().NotBeNull();
        result.RowCount.Should().Be(5);
        result.Data.Should().HaveCount(5);
        result.Message.Should().Contain("N+1問題あり");
    }

    [Fact]
    public async Task GetUsersBad_ShouldIncludeDepartmentInfo()
    {
        // Act
        var result = await _service.GetUsersBad();

        // Assert
        result.Data.Should().AllSatisfy(user =>
        {
            user.Department.Should().NotBeNull();
            user.Department.Name.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetUsersBad_ShouldTrackSqlCount()
    {
        // Act
        var result = await _service.GetUsersBad();

        // Assert
        result.SqlCount.Should().Be(6); // 1 + 5 queries (N+1 problem)
    }

    [Fact]
    public async Task GetUsersGood_ShouldReturnAllUsers()
    {
        // Act
        var result = await _service.GetUsersGood();

        // Assert
        result.Should().NotBeNull();
        result.RowCount.Should().Be(5);
        result.Data.Should().HaveCount(5);
        result.Message.Should().Contain("最適化済み");
    }

    [Fact]
    public async Task GetUsersGood_ShouldIncludeDepartmentInfo()
    {
        // Act
        var result = await _service.GetUsersGood();

        // Assert
        result.Data.Should().AllSatisfy(user =>
        {
            user.Department.Should().NotBeNull();
            user.Department.Name.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetUsersGood_ShouldUseFewQueries()
    {
        // Act
        var result = await _service.GetUsersGood();

        // Assert
        result.SqlCount.Should().Be(1); // Only 1 JOIN query
    }

    [Fact]
    public async Task GetUsersGood_ShouldBeFasterThanBad()
    {
        // Act
        var badResult = await _service.GetUsersBad();
        var goodResult = await _service.GetUsersGood();

        // Assert
        goodResult.SqlCount.Should().BeLessThan(badResult.SqlCount);
    }

    [Fact]
    public async Task GetUsersBad_ShouldReturnSameDataAsGood()
    {
        // Act
        var badResult = await _service.GetUsersBad();
        var goodResult = await _service.GetUsersGood();

        // Assert
        badResult.RowCount.Should().Be(goodResult.RowCount);
        badResult.Data.Should().HaveCount(goodResult.Data.Count);

        // Verify all users are the same
        for (int i = 0; i < badResult.Data.Count; i++)
        {
            badResult.Data[i].Id.Should().Be(goodResult.Data[i].Id);
            badResult.Data[i].Name.Should().Be(goodResult.Data[i].Name);
            badResult.Data[i].Department.Id.Should().Be(goodResult.Data[i].Department.Id);
            badResult.Data[i].Department.Name.Should().Be(goodResult.Data[i].Department.Name);
        }
    }

    public void Dispose()
    {
        // Cleanup test database
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var dropCommand = connection.CreateCommand();
            dropCommand.CommandText = @"
                IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
                IF OBJECT_ID('Departments', 'U') IS NOT NULL DROP TABLE Departments;
            ";
            dropCommand.ExecuteNonQuery();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
