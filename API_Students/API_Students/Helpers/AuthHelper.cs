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
            // Create a unix epoch time
            var epochTime = DateTime.UnixEpoch;
            epochTime = epochTime.AddSeconds(utcExpiryDate).ToUniversalTime();
            return epochTime;
        }
    }
}
