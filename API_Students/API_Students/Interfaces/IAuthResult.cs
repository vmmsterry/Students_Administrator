using API_Students.Models;

namespace API_Students.Interfaces
{
    public interface IAuthResult
    {
        AuthResult GetErrorResult(string message);
        AuthResult GetSuccessResult(string token, string refreshToken);
    }
}
