using System.Threading.Tasks;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Entity;
using Fundoo.User.Domain.Models;

namespace Fundoo.User.Infrastructure.Services.User
{
    public class UserBL : IUserBL
    {
        private readonly IUserRL _userRL;

        public UserBL(IUserRL userRL)
        {
            _userRL = userRL;
        }

        public async Task<UserEntity> RegisterUserBL(UserEntity entity)
        {
            return await _userRL.RegisterUserRL(entity);
        }

        public async Task<UserEntity?> LoginUserBL(LoginModel loginUser)
        {
            return await _userRL.LoginUser(loginUser);
        }

        public async Task<UserEntity?> GetUserByIdBL(int userId)
        {
            return await _userRL.GetUserByIdAsync(userId);
        }

        public async Task<UserEntity?> GetUserByEmailBL(string email)
        {
            return await _userRL.GetUserByEmailAsync(email);
        }

        public async Task<UserEntity> UpdateUserBL(UserEntity user)
        {
            return await _userRL.UpdateUserAsync(user);
        }

        public async Task<UserEntity> FindOrCreateSsoUserBL(SsoLoginRequestModel ssoRequest)
        {
            return await _userRL.FindOrCreateSsoUserAsync(ssoRequest);
        }
    }
}
