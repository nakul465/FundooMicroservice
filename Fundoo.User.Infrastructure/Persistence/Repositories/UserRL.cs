using System.Threading.Tasks;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Entity;
using Fundoo.User.Domain.Models;
using Fundoo.User.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fundoo.User.Infrastructure.Persistence.Repositories
{
    public class UserRL : IUserRL
    {
        private readonly FundooContext _fundooContext;

        public UserRL(FundooContext fundooContext)
        {
            _fundooContext = fundooContext;
        }

        public async Task<UserEntity> RegisterUserRL(UserEntity userEntity)
        {
            await _fundooContext.Users.AddAsync(userEntity);
            await _fundooContext.SaveChangesAsync();
            return userEntity;
        }

        public async Task<UserEntity?> LoginUser(LoginModel loginuser)
        {
            string email = loginuser.Email;
            UserEntity? user = await _fundooContext.Users.FirstOrDefaultAsync(e => e.Email == email);
            return user;
        }

        public async Task<ForgotPasswordResponseModel> ForgotPasswordAsyncRL(ForgotPasswordRequestModel req)
        {
            bool containsEmail = await _fundooContext.Users.AnyAsync(e => e.Email == req.Email);

            if (containsEmail)
            {
                return new ForgotPasswordResponseModel
                {
                    Message = "Email exists",
                    Success = true
                };
            }

            return new ForgotPasswordResponseModel
            {
                Message = "Email doesn't exists",
                Success = false
            };
        }

        public async Task<ResetPasswordResponseModel> ResetPasswordAsyncRL(ResetPasswordRequestModel req)
        {
            var user = await _fundooContext.Users.FirstOrDefaultAsync(e => e.Email == req.Email);

            if (user == null)
            {
                return new ResetPasswordResponseModel
                {
                    Success = false,
                    Message = "user not found"
                };
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _fundooContext.SaveChangesAsync();

            return new ResetPasswordResponseModel
            {
                Success = true,
                Message = "Password Changed Successfully"
            };
        }

        public async Task<UserEntity?> GetUserByIdAsync(int userId)
        {
            return await _fundooContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<UserEntity?> GetUserByEmailAsync(string email)
        {
            return await _fundooContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<UserEntity> UpdateUserAsync(UserEntity user)
        {
            _fundooContext.Users.Update(user);
            await _fundooContext.SaveChangesAsync();
            return user;
        }

        public async Task<UserEntity> FindOrCreateSsoUserAsync(SsoLoginRequestModel ssoRequest)
        {
            string email = ssoRequest.Email ?? string.Empty;
            string firstName = ssoRequest.FirstName ?? string.Empty;
            string lastName = ssoRequest.LastName ?? string.Empty;
            string providerId = string.Empty;

            // If token provided without email, try decoding claims
            if (string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(ssoRequest.IdToken))
            {
                try
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    if (handler.CanReadToken(ssoRequest.IdToken))
                    {
                        var jwt = handler.ReadJwtToken(ssoRequest.IdToken);
                        email = jwt.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
                        var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "";
                        providerId = jwt.Subject ?? "";
                        if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(name))
                        {
                            var parts = name.Split(' ', 2);
                            firstName = parts[0];
                            if (parts.Length > 1) lastName = parts[1];
                        }
                    }
                }
                catch
                {
                    // Ignore token parsing exceptions; fall back
                }
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new System.ArgumentException("A valid email is required for SSO authentication.");
            }

            var existingUser = await _fundooContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                if (string.IsNullOrEmpty(existingUser.SsoProvider))
                {
                    existingUser.SsoProvider = ssoRequest.Provider;
                }
                if (!string.IsNullOrEmpty(providerId))
                {
                    existingUser.SsoProviderId = providerId;
                }
                await _fundooContext.SaveChangesAsync();
                return existingUser;
            }

            // Just-In-Time Provisioning
            var newUser = new UserEntity
            {
                Email = email,
                FirstName = string.IsNullOrWhiteSpace(firstName) ? "SSO" : firstName,
                LastName = string.IsNullOrWhiteSpace(lastName) ? "User" : lastName,
                Password = BCrypt.Net.BCrypt.HashPassword(System.Guid.NewGuid().ToString("N")),
                PhoneNumber = string.Empty,
                SsoProvider = ssoRequest.Provider,
                SsoProviderId = providerId,
                IsTwoFactorEnabled = false
            };

            await _fundooContext.Users.AddAsync(newUser);
            await _fundooContext.SaveChangesAsync();
            return newUser;
        }
    }
}
