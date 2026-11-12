using System;
using Fundoo.Notes.Domain.Models;

namespace Fundoo.Notes.Application.Features.Notes.Command
{
    public class TrashUntrashCommand
    {
        public NotesIdModel NotesId { get; set; }
        public int UserId { get; set; }

        public TrashUntrashCommand(NotesIdModel notesId, int userId)
        {
            NotesId = notesId;
            UserId = userId;
        }
    }
}
