using System;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Command
{
    public class DeleteNoteCommand : IRequest<NoteEntity?>
    {
        public NotesIdModel NotesId { get; }
        public int UserId { get; }

        public DeleteNoteCommand(NotesIdModel notesId, int userId)
        {
            NotesId = notesId;
            UserId = userId;
        }
    }
}
