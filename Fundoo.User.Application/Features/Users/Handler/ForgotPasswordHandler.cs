using Fundoo.User.Application.Command;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Models;
using MediatR;

namespace Fundoo.User.Application.Handler
{
    public class ForgotPasswordHandler: IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponseModel>
    {
        private readonly IUserRL _userRL;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;

        public ForgotPasswordHandler(IUserRL userRL,IEmailService emailService,IJwtService jwtService)
        {
            _userRL = userRL;
            _emailService = emailService;
            _jwtService = jwtService;
        }

        public async Task<ForgotPasswordResponseModel> Handle(ForgotPasswordCommand command,CancellationToken cancellationToken)
        {
            var request = command.Request;

            var result = await _userRL.ForgotPasswordAsyncRL(request);

            if (!result.Success)
                return result;

            var token = _jwtService.GeneratePasswordResetToken(request.Email);

            var emailRequest = new EmailRequestModel
            {
                To = request.Email,
                Subject = "Password Reset",
                Body = $"Your password reset token is: {token}",
                IsHtml = false
            };

            var emailResponse = await _emailService.SendEmailAsync(emailRequest);

            if (!emailResponse.Success)
            {
                return new ForgotPasswordResponseModel
                {
                    Success = false,
                    Message = "Password reset email could not be sent."
                };
            }

            return new ForgotPasswordResponseModel
            {
                Success = true,
                Message = "Password reset email sent successfully."
            };
        }
    }
}

