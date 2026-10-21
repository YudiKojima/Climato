using Climato.Gateway.Domain.Exceptions;
using Climato.Gateway.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Climato.Gateway.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ClimatoDbContext context;

    public UserRepository(ClimatoDbContext context)
    {
        this.context = context;
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.AddAsync(user, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ix_user_email" })
        {
            throw new DomainException("E-mail já cadastrado.");
        }
    }

    public async Task<User?> GetAsync(string email, CancellationToken cancellationToken)
    {
        return await context.Users
            .Where(c => c.Email == email)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Users
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AllowCreateAsync(string email, CancellationToken cancellationToken)
    {
        var exists = await context.Users.AnyAsync(c => c.Email == email, cancellationToken);
        return !exists;
    }
}
