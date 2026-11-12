using System;
using Fundoo.User.Domain.Models;
using MediatR;

namespace Fundoo.User.Application.Command
{
	public class LoginCommand: IRequest<UserResponseModel?>
    {
        public LoginModel _loginModel;

		public LoginCommand(LoginModel loginModel)
		{
			_loginModel = loginModel;
		}
	}
}

