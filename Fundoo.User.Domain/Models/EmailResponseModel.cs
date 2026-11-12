using System;
namespace Fundoo.User.Domain.Models
{
	public class EmailResponseModel
	{
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

