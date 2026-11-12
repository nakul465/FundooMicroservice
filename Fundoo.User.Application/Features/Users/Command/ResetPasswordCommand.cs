using System;
using Fundoo.User.Domain.Models;
using MediatR;

namespace Fundoo.User.Application.Command
{
    public class ResetPasswordCommand : IRequest<ResetPasswordResponseModel>
    {
        public ResetPasswordRequestModel Request { get; }

        public ResetPasswordCommand(ResetPasswordRequestModel request)
        {
            Request = request;
        }
    }
}

