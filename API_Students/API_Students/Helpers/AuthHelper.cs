using API_Students.Interfaces;
using API_Students.Models;
using System.Security.Cryptography;

namespace API_Students.Helpers
{
    internal class AuthHelper: IAuthResult
    {
        public AuthResult GetErrorResult(string message)
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

        public AuthResult GetSuccessResult(string token, string refreshToken)
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
            var randomGenerator = RandomNumberGenerator.Create();
            byte[] data = new byte[length];
            randomGenerator.GetBytes(data);
            return BitConverter.ToString(data);
        }

        internal static DateTime UnixTimeStamToDateTime(long utcExpiryDate)
        {
            var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTimeVal = dateTimeVal.AddSeconds(utcExpiryDate).ToUniversalTime();
            return dateTimeVal;
        }
    }
}
