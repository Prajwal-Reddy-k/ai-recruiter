using AIRecruiter.Domain.Entities;

namespace AIRecruiter.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
