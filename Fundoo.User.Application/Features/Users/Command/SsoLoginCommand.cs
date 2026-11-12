using Fundoo.User.Domain.Models;
using MediatR;

namespace Fundoo.User.Application.Command
{
    public class SsoLoginCommand : IRequest<SsoLoginResponseModel>
    {
        public SsoLoginRequestModel Model { get; }

        public SsoLoginCommand(SsoLoginRequestModel model)
        {
            Model = model;
        }
    }
}
