using System;
using System.Threading;
using System.Threading.Tasks;
using Fundoo.Notes.Application.Features.Notes.Query;
using Fundoo.Notes.Application.Features.Notes.Services;
using Fundoo.Notes.Domain.Entity;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Handler
{
    public class GetNoteByIdHandler : IRequestHandler<GetNoteByIdQuery, NoteEntity?>
    {
        private readonly INotesBL _notesBL;

        public GetNoteByIdHandler(INotesBL notesBL)
        {
            _notesBL = notesBL;
        }

        public async Task<NoteEntity?> Handle(GetNoteByIdQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0)
                throw new ArgumentException("Invalid user.");

            if (request.NotesId.NoteId <= 0)
                throw new ArgumentException("Invalid note ID.");

            return await _notesBL.FindNoteByIdBL(request.NotesId, request.UserId);
        }
    }
}
