using API_Students.Models;
using API_Students.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using API_Students.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using API_Students.Infrastructure;

namespace API_Students.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        private readonly DB_Context _dbContext;

        public AuthenticationController(UserManager<IdentityUser> userManager, IConfiguration configuration, DB_Context dbContext)
        {
            _userManager = userManager;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequest registrationRequest)
        {
            if (ModelState.IsValid)
            {
                // Validate if the user exist
                var userExist = await _userManager.FindByEmailAsync(registrationRequest.Email);

                if (userExist != null)
                    return BadRequest(AuthHelper.CreateAuthResult("Email already exist"));


                //Create user
                var newUser = new IdentityUser()
                {
                    Email = registrationRequest.Email,
                    UserName = registrationRequest.Email
                };

                var createUser = await _userManager.CreateAsync(newUser,registrationRequest.Password);

                if (createUser.Succeeded)
                {
                    // Generate token
                    var jwtToken = await GenerateToken(newUser);
                    return Ok(jwtToken);
                }

                return BadRequest(AuthHelper.CreateAuthResult("Server error"));
            }

            return BadRequest();
        }

        /// <summary>
        /// Generate JWT Token
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task<AuthResult> GenerateToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration.GetSection("JwtConfig:Secret").Value);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(type:"Id", value:user.Id),
                    new Claim(type:JwtRegisteredClaimNames.Sub, value:user.Email),
                    new Claim(type:JwtRegisteredClaimNames.Email, value:user.Email),
                    new Claim(type:JwtRegisteredClaimNames.Jti, value:Guid.NewGuid().ToString()),
                    new Claim(type:JwtRegisteredClaimNames.Iat, value:DateTime.Now.ToUniversalTime().ToString())
                }),
                NotBefore = DateTime.Now,
                Expires = DateTime.UtcNow.Add(TimeSpan.Parse(_configuration.GetSection("JwtConfig:ExpiryTimeFrame").Value)),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), algorithm:SecurityAlgorithms.HmacSha256)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken()
            {
                JwtId = token.Id,
                Token = AuthHelper.RandomStringGeneration(23),
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                IsRevoked = false,
                IsUsed = false,
                UserId = user.Id
            };

            await _dbContext.refreshToken.AddAsync(refreshToken);
            await _dbContext.SaveChangesAsync();

            return AuthHelper.CreateAuthResult(jwtToken, refreshToken.Token);
        }
        
    }
}
