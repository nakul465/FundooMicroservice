using System;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Command
{
    public class TrashNoteCommand : IRequest<int>
    {
        public NotesIdModel NotesId { get; }
        public int UserId { get; }

        public TrashNoteCommand(NotesIdModel notesId, int userId)
        {
            NotesId = notesId;
            UserId = userId;
        }
    }
}
