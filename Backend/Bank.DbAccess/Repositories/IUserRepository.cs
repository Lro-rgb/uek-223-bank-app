using Bank.Core.Models;

namespace Bank.DbAccess.Repositories;

public interface IUserRepository
{
    User? Authenticate(string? username, string? password);
    User SelectOne(int id);
    void Update(User user);
    User Insert(User user);
    void Delete(int id);
}