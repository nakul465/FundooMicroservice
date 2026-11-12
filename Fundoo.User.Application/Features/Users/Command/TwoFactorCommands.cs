using Fundoo.User.Domain.Models;
using MediatR;

namespace Fundoo.User.Application.Command
{
    public class TwoFactorSetupCommand : IRequest<TwoFactorSetupResponseModel>
    {
        public int UserId { get; }

        public TwoFactorSetupCommand(int userId)
        {
            UserId = userId;
        }
    }

    public class TwoFactorEnableCommand : IRequest<ResponseModel<bool>>
    {
        public int UserId { get; }
        public TwoFactorEnableRequestModel Model { get; }

        public TwoFactorEnableCommand(int userId, TwoFactorEnableRequestModel model)
        {
            UserId = userId;
            Model = model;
        }
    }

    public class TwoFactorDisableCommand : IRequest<ResponseModel<bool>>
    {
        public int UserId { get; }
        public TwoFactorDisableRequestModel Model { get; }

        public TwoFactorDisableCommand(int userId, TwoFactorDisableRequestModel model)
        {
            UserId = userId;
            Model = model;
        }
    }

    public class TwoFactorVerifyLoginCommand : IRequest<UserResponseModel?>
    {
        public TwoFactorVerifyLoginRequestModel Model { get; }

        public TwoFactorVerifyLoginCommand(TwoFactorVerifyLoginRequestModel model)
        {
            Model = model;
        }
    }

    public class TwoFactorSendEmailOtpCommand : IRequest<ResponseModel<bool>>
    {
        public TwoFactorSendEmailOtpRequestModel Model { get; }

        public TwoFactorSendEmailOtpCommand(TwoFactorSendEmailOtpRequestModel model)
        {
            Model = model;
        }
    }
}
