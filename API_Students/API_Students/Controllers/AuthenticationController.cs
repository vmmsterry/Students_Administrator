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
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace API_Students.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly TokenValidationParameters _tokenValidationParameters;

        private readonly DB_Context _dbContext;

        private readonly AuthHelper authHelper;

        public AuthenticationController(UserManager<IdentityUser> userManager, IConfiguration configuration, DB_Context dbContext, TokenValidationParameters tokenValidationParameters)
        {
            _userManager = userManager;
            _configuration = configuration;
            _dbContext = dbContext;
            _tokenValidationParameters = tokenValidationParameters;

            authHelper = new AuthHelper();
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
                    return BadRequest(authHelper.GetErrorResult("Email already exist"));


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

                return BadRequest(authHelper.GetErrorResult("Server error"));
            }

            return BadRequest();
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest userLogin)
        {
            if (ModelState.IsValid)
            {
                // User exist
                var existingUser = await _userManager.FindByEmailAsync(userLogin.Email);
                if(existingUser == null)
                    return BadRequest(authHelper.GetErrorResult("User doesn't exist"));

                // User is correct
                var isCorrect = await _userManager.CheckPasswordAsync(existingUser, userLogin.Password);
                if(!isCorrect)
                    return BadRequest(authHelper.GetErrorResult("Invalid credentials"));

                var jwtToken = await GenerateToken(existingUser);
                return Ok(jwtToken);
            }

            return BadRequest();
        }

        [HttpPost]
        [Route("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
        {
            if (ModelState.IsValid)
            {
                var result = await ValidateAndGenerateToken(tokenRequest);

                if(result == null)
                    return BadRequest(authHelper.GetErrorResult("Invalid parameters"));

                return Ok(result);
            }

            return BadRequest(authHelper.GetErrorResult("Invalid parameters"));
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

            await _dbContext.RefreshToken.AddAsync(refreshToken);
            await _dbContext.SaveChangesAsync();

            return authHelper.GetSuccessResult(jwtToken, refreshToken.Token);
        }
        

        private async Task<AuthResult> ValidateAndGenerateToken(TokenRequest tokenRequest)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            try
            {
                _tokenValidationParameters.ValidateLifetime = false; // for testing - needs to be true 

                var tokenInVerification = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParameters, out var validatedToken);
                if (validatedToken is JwtSecurityToken jwtSecurityToken) 
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
                    if (!result)
                        return authHelper.GetErrorResult("Invalid token");
                }
                
                var utcExpiryDate = long.Parse(tokenInVerification.Claims.First(x => x.Type.Equals(JwtRegisteredClaimNames.Exp)).Value);
                var expiryDate = AuthHelper.UnixTimeStamToDateTime(utcExpiryDate);
                if(expiryDate < DateTime.Now)
                    return authHelper.GetErrorResult("Invalid token");

                var storedToken = _dbContext.RefreshToken.First(x => x.Token == tokenRequest.RefreshToken);

                if(storedToken == null)
                    return authHelper.GetErrorResult("Invalid token");

                if (storedToken.IsUsed)
                    return authHelper.GetErrorResult("Invalid token");

                if (storedToken.IsRevoked)
                    return authHelper.GetErrorResult("Invalid token");

                var jti = tokenInVerification.Claims.First(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

                if (storedToken.JwtId != jti)
                    return authHelper.GetErrorResult("Invalid token");

                if (storedToken.ExpiryDate < DateTime.Now)
                    return authHelper.GetErrorResult("Expired token");

                storedToken.IsUsed = true;
                _dbContext.RefreshToken.Update(storedToken);
                _dbContext.SaveChanges();

                var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);
                return await GenerateToken(dbUser);
            }
            catch
            {
                return authHelper.GetErrorResult("Server error");
            }
        }
    }
}
