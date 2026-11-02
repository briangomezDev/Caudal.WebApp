using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Caudal.Infraestructure.Testing;

public class UnitOfWorkTests
{
    // La cadena de conexión nunca se usa: estas pruebas no abren conexión a la base de datos.
    private static CaudalDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CaudalDbContext>()
            .UseNpgsql("Host=unused;Database=unused")
            .Options;

        return new CaudalDbContext(options);
    }

    // Bueno: el flujo esperado se completa correctamente.

    [Fact]
    public async Task SaveChangesAsync_WithoutChanges_ReturnsZero()
    {
        // Arrange
        using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(0, result);
    }

    // Malo: el uso incorrecto de la transacción se rechaza.

    [Fact]
    public void Constructor_WithNullContext_ThrowsException()
    {
        // Arrange
        CaudalDbContext context = null!;

        // Act
        var act = () => new UnitOfWork(context);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public async Task CommitTransactionAsync_WithoutTransaction_ThrowsException()
    {
        // Arrange
        using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);

        // Act
        var act = () => unitOfWork.CommitTransactionAsync();

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithoutTransaction_ThrowsException()
    {
        // Arrange
        using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);

        // Act
        var act = () => unitOfWork.RollbackTransactionAsync();

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCanceledToken_ThrowsException()
    {
        // Arrange
        using var context = CreateContext();
        context.Add(new User(
            new UserId(Guid.NewGuid()),
            "Brian Gomez",
            Email.Create("brian@caudal.com"),
            "hashed-password",
            new SaltCode("abc123"),
            new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc)));
        var unitOfWork = new UnitOfWork(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var act = () => unitOfWork.SaveChangesAsync(cancellation.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }

    // Fallo: el contexto ya no está disponible.

    [Fact]
    public async Task CommitTransactionAsync_WhenContextIsDisposed_ThrowsException()
    {
        // Arrange
        var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        context.Dispose();

        // Act
        var act = () => unitOfWork.CommitTransactionAsync();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(act);
    }

    [Fact]
    public async Task RollbackTransactionAsync_WhenContextIsDisposed_ThrowsException()
    {
        // Arrange
        var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        context.Dispose();

        // Act
        var act = () => unitOfWork.RollbackTransactionAsync();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(act);
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenContextIsDisposed_ThrowsException()
    {
        // Arrange
        var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        context.Dispose();

        // Act
        var act = () => unitOfWork.BeginTransactionAsync();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(act);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenContextIsDisposed_ThrowsException()
    {
        // Arrange
        var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        context.Dispose();

        // Act
        var act = () => unitOfWork.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(act);
    }
}
