using System;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Command
{
    public class CreateNoteCommand : IRequest<NoteEntity>
    {
        public NoteEntity Note { get; }

        public CreateNoteCommand(NoteCreationModel model, int userId)
        {
            Note = new NoteEntity
            {
                Title = model.Title,
                Description = model.Description,
                Colour = model.Colour,
                UserId = userId
            };
        }
    }
}
