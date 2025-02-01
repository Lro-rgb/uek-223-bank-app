using Bank.Core.Helper;
using Bank.Core.Models;
using Bank.DbAccess.Data;

namespace Bank.DbAccess.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public User? Authenticate(string? username, string? password)
    {
        var user = context.Users
            .FirstOrDefault(u => u.Username == username);

        if (user == null || !PasswordHelper.VerifyPassword(password, user))
        {
            return null;
        }
        
        return user;
    }
    
    public User SelectOne(int id)
    {
        var user = context.Users.FirstOrDefault(u => u.Id == id);

        if (user == null)
        {
            throw new Exception($"No User with id {id}");
        }

        return user;
    }

    public void Update(User user)
    {
        var existingUser = context.Users.FirstOrDefault(u => u.Id == user.Id);
        if (existingUser == null)
        {
            throw new Exception($"User with id {user.Id} not found");
        }

        existingUser.Username = user.Username;
        existingUser.PasswordHash = user.PasswordHash;
        existingUser.Role = user.Role;

        context.SaveChanges();
    }
    
    public User Insert(User user)
    {
        context.Users.Add(user);
        context.SaveChanges();

        return user;
    }

    public void Delete(int id)
    {
        var user = context.Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            throw new Exception($"No User with id {id}");
        }

        context.Users.Remove(user);
        context.SaveChanges();
    }
}