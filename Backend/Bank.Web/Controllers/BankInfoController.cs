using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class BankInfoController : ControllerBase
{
    [HttpGet]
    public Dictionary<string, string> Get()
    {
        var currentUser = HttpContext.User;
        var ret = new Dictionary<string, string> { ["name"] = "Bank.Web", ["version"] = "1" };
        foreach (var claim in currentUser.Claims)
        {
            ret.Add(claim.Type, claim.Value);
        }
        return ret;
    }
}