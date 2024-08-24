using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace TodoApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthenticationController(IConfiguration config)
    {
        _config = config;
    }

    public record AuthenticationData(string? UserName, string? Password);
    public record UserData(int Id, string FirstName, string LastName, string UserName);

    [HttpPost("token")]
    [AllowAnonymous]
    public ActionResult<string> Authenticate([FromBody] AuthenticationData data)
    {
        var user = ValidateCredentials(data);
        
        if (user == null)
        {
            return Unauthorized();
        }

        string token = GenerateToken(user);
        return Ok(token);
        
    }

    private string GenerateToken(UserData user)
    {
        var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_config.GetValue<string>("Authentication:SecretKey")));

        var signingCredentails = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = new();

        claims.Add(new(JwtRegisteredClaimNames.Sub, user.Id.ToString()));
        claims.Add(new(JwtRegisteredClaimNames.UniqueName, user.UserName));
        claims.Add(new(JwtRegisteredClaimNames.GivenName, user.FirstName));
        claims.Add(new(JwtRegisteredClaimNames.FamilyName, user.LastName));

        var token = new JwtSecurityToken(
            _config.GetValue<string>("Authentication:Issuer"),
            _config.GetValue<string>("Authentication:Audience"),
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(15),
            signingCredentails
            );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private UserData? ValidateCredentials(AuthenticationData data)
    {
        //NOT PRODUCTION CODE - BEWARE OF GNOMES
        if (CompareValues(data.UserName, "admin") && CompareValues(data.Password, "admin"))
        {
            return new UserData(1, "admin", "admin", data.UserName!);
        }

        return null;
    }

    private bool CompareValues(string? actual, string? expected)
    {
        if (actual != null) { if (actual.Equals(expected)) return true; }
        return false;
    }
}
