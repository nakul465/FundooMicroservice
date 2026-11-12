using Fundoo.User.Domain.Entity;
using Fundoo.User.Domain.Models;

namespace Fundoo.User.Application.Services
{
	public interface IUserRL
	{
        public Task<UserEntity> RegisterUserRL(UserEntity userEntity);
        public Task<UserEntity?> LoginUser(LoginModel loginuser);
        public Task<ForgotPasswordResponseModel> ForgotPasswordAsyncRL(ForgotPasswordRequestModel req);
        public Task<ResetPasswordResponseModel> ResetPasswordAsyncRL(ResetPasswordRequestModel req);
        public Task<UserEntity?> GetUserByIdAsync(int userId);
        public Task<UserEntity?> GetUserByEmailAsync(string email);
        public Task<UserEntity> UpdateUserAsync(UserEntity user);
        public Task<UserEntity> FindOrCreateSsoUserAsync(SsoLoginRequestModel ssoRequest);
    }
}