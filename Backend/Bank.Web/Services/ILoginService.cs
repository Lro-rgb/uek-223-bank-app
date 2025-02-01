using Bank.Core.Models;

namespace Bank.Web.Services;

public interface ILoginService
{
    string CreateJwt(User? user);
}