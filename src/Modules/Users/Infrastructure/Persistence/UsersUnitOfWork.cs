using AlQalam.Users.Core.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace AlQalam.Users.Infrastructure.Persistence;

public sealed class UsersTransaction(IDbContextTransaction transaction) : IUsersTransaction
{
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await transaction.RollbackAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await transaction.DisposeAsync();
    }
}

public sealed class UsersUnitOfWork(UsersDbContext context) : IUsersUnitOfWork
{
    public async Task<IUsersTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new UsersTransaction(transaction);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
