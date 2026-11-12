using System.Threading;
using System.Threading.Tasks;
using Fundoo.User.Application.Command;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Models;
using MediatR;

namespace Fundoo.User.Application.Handler
{
    public class SsoLoginHandler : IRequestHandler<SsoLoginCommand, SsoLoginResponseModel>
    {
        private readonly IUserBL _userBL;
        private readonly IJwtService _jwtService;

        public SsoLoginHandler(IUserBL userBL, IJwtService jwtService)
        {
            _userBL = userBL;
            _jwtService = jwtService;
        }

        public async Task<SsoLoginResponseModel> Handle(SsoLoginCommand command, CancellationToken cancellationToken)
        {
            var user = await _userBL.FindOrCreateSsoUserBL(command.Model);

            // If user has 2FA enabled, return 2FA session token challenge instead of full token
            if (user.IsTwoFactorEnabled)
            {
                var twoFactorType = user.TwoFactorType ?? "TOTP";
                var sessionToken = _jwtService.GenerateTwoFactorSessionToken(user.UserId, user.Email, twoFactorType);

                return new SsoLoginResponseModel
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Provider = user.SsoProvider ?? command.Model.Provider,
                    Token = string.Empty,
                    IsNewUser = false,
                    RequiresTwoFactor = true,
                    TwoFactorSessionToken = sessionToken,
                    TwoFactorType = twoFactorType
                };
            }

            var userResponse = new UserResponseModel
            {
                UserId = user.UserId,
                Email = user.Email,
                Fname = user.FirstName,
                Lname = user.LastName,
                ContactNo = user.PhoneNumber,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                SsoProvider = user.SsoProvider
            };

            var token = _jwtService.GenerateToken(userResponse);

            return new SsoLoginResponseModel
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Provider = user.SsoProvider ?? command.Model.Provider,
                Token = token,
                IsNewUser = string.IsNullOrEmpty(user.PhoneNumber)
            };
        }
    }
}
