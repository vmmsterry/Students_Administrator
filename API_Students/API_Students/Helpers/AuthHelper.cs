using API_Students.Models;

namespace API_Students.Helpers
{
    internal class AuthHelper
    {
        internal static AuthResult CreateAuthResult(string message)
        {
            return new AuthResult()
            {
                Result = false,
                Errors = new List<string>()
                {
                    message
                }
            };
        }

        internal static AuthResult CreateAuthResult(string token, string refreshToken)
        {
            return new AuthResult()
            {
                Result = true,
                Token = token,
                RefreshToken = refreshToken
            };
        }

        internal static string RandomStringGeneration(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyz_";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
