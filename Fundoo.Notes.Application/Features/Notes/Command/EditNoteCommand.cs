using System;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Command
{
    public class EditNoteCommand : IRequest<NoteEntity?>
    {
        public NoteEntity Note { get; }
        public int UserId { get; }

        public EditNoteCommand(EditNoteModel model, int userId)
        {
            Note = new NoteEntity
            {
                NoteId = model.NoteId,
                Title = model.Title,
                Description = model.Description,
                Colour = model.Colour,
                UserId = userId
            };

            UserId = userId;
        }
    }
}
