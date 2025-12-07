using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using BlazorApp.Features.Demo.Data;
using BlazorApp.Features.Demo.Entities;
using BlazorApp.Features.Demo.Services;

namespace BlazorApp.Tests.Features.Demo;

public class NPlusOneServiceTests : IDisposable
{
    private readonly DemoDbContext _context;
    private readonly NPlusOneService _service;

    public NPlusOneServiceTests()
    {
        var options = new DbContextOptionsBuilder<DemoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique database for each test
            .Options;

        _context = new DemoDbContext(options);

        // Seed test data
        SeedTestData();

        var logger = new Mock<ILogger<NPlusOneService>>();
        _service = new NPlusOneService(_context, logger.Object);
    }

    private void SeedTestData()
    {
        var departments = new List<Department>
        {
            new Department { Id = 1, Name = "開発部" },
            new Department { Id = 2, Name = "営業部" },
            new Department { Id = 3, Name = "人事部" }
        };

        var users = new List<User>
        {
            new User { Id = 1, Name = "山田太郎", DepartmentId = 1, Email = "yamada@example.com" },
            new User { Id = 2, Name = "佐藤花子", DepartmentId = 2, Email = "sato@example.com" },
            new User { Id = 3, Name = "鈴木一郎", DepartmentId = 1, Email = "suzuki@example.com" },
            new User { Id = 4, Name = "高橋美咲", DepartmentId = 3, Email = "takahashi@example.com" },
            new User { Id = 5, Name = "田中健太", DepartmentId = 1, Email = "tanaka@example.com" }
        };

        _context.Departments.AddRange(departments);
        _context.Users.AddRange(users);
        _context.SaveChanges();
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
        result.SqlCount.Should().BeGreaterThan(1); // Should have multiple SQL queries
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
        result.SqlCount.Should().BeLessThanOrEqualTo(2); // Should be optimized with fewer queries
    }

    [Fact]
    public async Task GetUsersGood_ShouldBeFasterThanBad()
    {
        // Arrange
        // Add more data to make the difference noticeable
        for (int i = 6; i <= 50; i++)
        {
            _context.Users.Add(new User
            {
                Id = i,
                Name = $"User{i}",
                DepartmentId = (i % 3) + 1,
                Email = $"user{i}@example.com"
            });
        }
        _context.SaveChanges();

        // Act
        var badResult = await _service.GetUsersBad();
        var goodResult = await _service.GetUsersGood();

        // Assert
        goodResult.ExecutionTimeMs.Should().BeLessThanOrEqualTo(badResult.ExecutionTimeMs);
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
        _context.Dispose();
    }
}
