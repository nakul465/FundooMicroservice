using System;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Command
{
    public class ArchiveNoteCommand : IRequest<int>
    {
        public NotesIdModel NotesId { get; }
        public int UserId { get; }

        public ArchiveNoteCommand(NotesIdModel notesId, int userId)
        {
            NotesId = notesId;
            UserId = userId;
        }
    }
}
