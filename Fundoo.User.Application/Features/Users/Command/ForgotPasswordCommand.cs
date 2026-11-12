using System;
using Fundoo.User.Domain.Models;
using MediatR;

namespace Fundoo.User.Application.Command
{
	public class ForgotPasswordCommand : IRequest<ForgotPasswordResponseModel>
    {
        public ForgotPasswordRequestModel Request { get; }

        public ForgotPasswordCommand(ForgotPasswordRequestModel request)
        {
            Request = request;
        }
    }
}

