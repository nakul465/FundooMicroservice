using System;

namespace Fundoo.Notes.Domain.Exceptions
{
    public class DuplicateLabelException : Exception
    {
        public DuplicateLabelException(string message) : base(message)
        {
        }
    }
}
