using Fundoo.User.Application.Command;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Models;
using MediatR;

namespace Fundoo.User.Application.Handler
{
    public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponseModel>
    {
        private readonly IUserRL _userRL;
        private readonly IJwtService _jwtService;

        public ResetPasswordHandler(IUserRL userRL,IJwtService jwtService)
        {
            _userRL = userRL;
            _jwtService = jwtService;
        }

        public async Task<ResetPasswordResponseModel> Handle(ResetPasswordCommand command,CancellationToken cancellationToken)
        {
            var request = command.Request;

            var tokenEmail =_jwtService.ValidatePasswordResetToken(request.Token);

            if (tokenEmail == null ||!string.Equals(tokenEmail,request.Email,StringComparison.OrdinalIgnoreCase))
            {
                return new ResetPasswordResponseModel
                {
                    Success = false,
                    Message = "Invalid or expired password reset token."
                };
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return new ResetPasswordResponseModel
                {
                    Success = false,
                    Message = "The passwords do not match."
                };
            }

            return await _userRL.ResetPasswordAsyncRL(request);
        }
    }
}

