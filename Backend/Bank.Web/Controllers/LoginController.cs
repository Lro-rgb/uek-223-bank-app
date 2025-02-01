using Bank.DbAccess.Repositories;
using Bank.Web.Dto;
using Bank.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Bank.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LoginController(IUserRepository userRepository, ILoginService loginService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LoginDto login)
    {
        return await Task.Run(() =>
        {
            IActionResult response;

            var user = userRepository.Authenticate(login.Username, login.Password);
                
            if (user == null)
            {
                response = Unauthorized();
            }
            else
            {
                response = Ok(new { token = loginService.CreateJwt(user) });
            }
                
            return response;
        });
    }
}