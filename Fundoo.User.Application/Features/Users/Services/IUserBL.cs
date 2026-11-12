
using Fundoo.User.Domain.Entity;
using Fundoo.User.Domain.Models;

namespace Fundoo.User.Application.Services
{
	public interface IUserBL
	{
        public Task<UserEntity> RegisterUserBL(UserEntity entity);
        public Task<UserEntity?> LoginUserBL(LoginModel loginUser);
        public Task<UserEntity?> GetUserByIdBL(int userId);
        public Task<UserEntity?> GetUserByEmailBL(string email);
        public Task<UserEntity> UpdateUserBL(UserEntity user);
        public Task<UserEntity> FindOrCreateSsoUserBL(SsoLoginRequestModel ssoRequest);
    }
}

