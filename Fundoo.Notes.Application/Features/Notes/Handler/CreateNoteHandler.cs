using System;
using System.Threading;
using System.Threading.Tasks;
using Fundoo.Notes.Application.Features.Notes.Command;
using Fundoo.Notes.Application.Features.Notes.Services;
using Fundoo.Notes.Domain.Entity;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Handler
{
    public class CreateNoteHandler : IRequestHandler<CreateNoteCommand, NoteEntity>
    {
        private readonly INotesBL _notesBL;

        public CreateNoteHandler(INotesBL notesBL)
        {
            _notesBL = notesBL;
        }

        public async Task<NoteEntity> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
        {
            if (request.Note.UserId <= 0)
                throw new ArgumentException("Invalid user.");

            if (string.IsNullOrWhiteSpace(request.Note.Title))
                throw new ArgumentException("Note title is required.");

            return await _notesBL.AddNoteBL(request.Note);
        }
    }
}
