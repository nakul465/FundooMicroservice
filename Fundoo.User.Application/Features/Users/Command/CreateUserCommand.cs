using System;
using Fundoo.User.Domain.Entity;
using Fundoo.User.Domain.Models;
using MediatR;

namespace Fundoo.User.Application.Command
{
	public class CreateUserCommand : IRequest<UserResponseModel>
    {
        public UserEntity User { get; }

        public CreateUserCommand(RegistrationModel registrationModel)
        {
            User = new UserEntity();
            User.FirstName = registrationModel.FirstName;
            User.LastName = registrationModel.LastName;
            User.Email = registrationModel.Email;
            User.Password = BCrypt.Net.BCrypt.HashPassword(registrationModel.Password);
            User.PhoneNumber = registrationModel.ContactNo;
        }
    }
}

