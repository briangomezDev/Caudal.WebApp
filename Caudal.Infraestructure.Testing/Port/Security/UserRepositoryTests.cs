using System.Linq.Expressions;
using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Infraestructure.Port.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Caudal.Infraestructure.Testing.Port.Security;

public class UserRepositoryTests
{
    private static readonly DateTime UtcDate = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    private static User CreateUser(string email = "brian@caudal.com")
    {
        return new User(
            new UserId(Guid.NewGuid()),
            "Brian Gomez",
            Email.Create(email),
            "hashed-password",
            new SaltCode("abc123"),
            UtcDate);
    }

    private static UserRepository CreateRepository(params User[] users)
    {
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Users).Returns(users.ToList().BuildMockDbSet().Object);

        return new UserRepository(context.Object);
    }

    private static UserRepository CreateRepository(Mock<IAsyncQueryProvider> provider)
    {
        var users = new Mock<DbSet<User>>();
        var queryable = users.As<IQueryable<User>>();
        queryable.Setup(u => u.Provider).Returns(provider.Object);
        queryable.Setup(u => u.Expression).Returns(new List<User>().AsQueryable().Expression);
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Users).Returns(users.Object);

        return new UserRepository(context.Object);
    }

    private static UserRepository CreateRepositoryWithoutUsers()
    {
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Users).Returns((DbSet<User>?)null);

        return new UserRepository(context.Object);
    }

    // Bueno: el flujo esperado se completa correctamente.

    [Fact]
    public async Task AddUserAsync_WithValidUser_AddsUserToDbSet()
    {
        // Arrange
        var user = CreateUser();
        var users = new Mock<DbSet<User>>();
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Users).Returns(users.Object);
        var repository = new UserRepository(context.Object);

        // Act
        await repository.AddUserAsync(user, CancellationToken.None);

        // Assert
        users.Verify(u => u.AddAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExistsByEmailAsync_WhenUserExists_ReturnsTrue()
    {
        // Arrange
        var repository = CreateRepository(CreateUser());

        // Act
        var exists = await repository.ExistsByEmailAsync(Email.Create("brian@caudal.com"), CancellationToken.None);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var user = CreateUser();
        var repository = CreateRepository(CreateUser("other@caudal.com"), user);

        // Act
        var result = await repository.GetByEmailAsync(Email.Create("brian@caudal.com"), CancellationToken.None);

        // Assert
        Assert.Same(user, result);
    }

    [Fact]
    public async Task AddUserAsync_WithCancellationToken_PassesTokenToDbSet()
    {
        // Arrange
        var user = CreateUser();
        using var cancellation = new CancellationTokenSource();
        var users = new Mock<DbSet<User>>();
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Users).Returns(users.Object);
        var repository = new UserRepository(context.Object);

        // Act
        await repository.AddUserAsync(user, cancellation.Token);

        // Assert
        users.Verify(u => u.AddAsync(user, cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task ExistsByEmailAsync_WithCancellationToken_PassesTokenToQueryProvider()
    {
        // Arrange
        var provider = new Mock<IAsyncQueryProvider>();
        var repository = CreateRepository(provider);
        using var cancellation = new CancellationTokenSource();

        // Act
        await repository.ExistsByEmailAsync(Email.Create("brian@caudal.com"), cancellation.Token);

        // Assert
        provider.Verify(p => p.ExecuteAsync<Task<bool>>(It.IsAny<Expression>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_WithCancellationToken_PassesTokenToQueryProvider()
    {
        // Arrange
        var provider = new Mock<IAsyncQueryProvider>();
        var repository = CreateRepository(provider);
        using var cancellation = new CancellationTokenSource();

        // Act
        await repository.GetByEmailAsync(Email.Create("brian@caudal.com"), cancellation.Token);

        // Assert
        provider.Verify(p => p.ExecuteAsync<Task<User>>(It.IsAny<Expression>(), cancellation.Token), Times.Once);
    }

    // Malo: datos inexistentes o inválidos se rechazan o no devuelven resultados.

    [Fact]
    public async Task ExistsByEmailAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var repository = CreateRepository(CreateUser());

        // Act
        var exists = await repository.ExistsByEmailAsync(Email.Create("other@caudal.com"), CancellationToken.None);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var repository = CreateRepository(CreateUser());

        // Act
        var result = await repository.GetByEmailAsync(Email.Create("other@caudal.com"), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsException()
    {
        // Arrange
        IApplicationDbContext context = null!;

        // Act
        var act = () => new UserRepository(context);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public async Task AddUserAsync_WhenUsersIsNull_ThrowsException()
    {
        // Arrange
        var repository = CreateRepositoryWithoutUsers();

        // Act
        var act = () => repository.AddUserAsync(CreateUser(), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ExistsByEmailAsync_WhenUsersIsNull_ThrowsException()
    {
        // Arrange
        var repository = CreateRepositoryWithoutUsers();

        // Act
        var act = () => repository.ExistsByEmailAsync(Email.Create("brian@caudal.com"), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUsersIsNull_ThrowsException()
    {
        // Arrange
        var repository = CreateRepositoryWithoutUsers();

        // Act
        var act = () => repository.GetByEmailAsync(Email.Create("brian@caudal.com"), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    // Fallo: una dependencia externa falla de forma inesperada.

    [Fact]
    public async Task AddUserAsync_WhenDbSetFails_PropagatesException()
    {
        // Arrange
        var user = CreateUser();
        var users = new Mock<DbSet<User>>();
        users
            .Setup(u => u.AddAsync(user, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database timeout."));
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Users).Returns(users.Object);
        var repository = new UserRepository(context.Object);

        // Act
        var act = () => repository.AddUserAsync(user, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<TimeoutException>(act);
    }

    [Fact]
    public async Task ExistsByEmailAsync_WhenContextFails_PropagatesException()
    {
        // Arrange
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Users).Throws(new TimeoutException("Database timeout."));
        var repository = new UserRepository(context.Object);

        // Act
        var act = () => repository.ExistsByEmailAsync(Email.Create("brian@caudal.com"), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<TimeoutException>(act);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenContextFails_PropagatesException()
    {
        // Arrange
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Users).Throws(new TimeoutException("Database timeout."));
        var repository = new UserRepository(context.Object);

        // Act
        var act = () => repository.GetByEmailAsync(Email.Create("brian@caudal.com"), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<TimeoutException>(act);
    }
}
