using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fundoo.Notes.Application.Features.Notes.Query;
using Fundoo.Notes.Application.Features.Notes.Services;
using Fundoo.Notes.Domain.Entity;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Handler
{
    public class GetAllNotesHandler : IRequestHandler<GetAllNotesQuery, List<NoteEntity>>
    {
        private readonly INotesBL _notesBL;

        public GetAllNotesHandler(INotesBL notesBL)
        {
            _notesBL = notesBL;
        }

        public async Task<List<NoteEntity>> Handle(
            GetAllNotesQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0)
                throw new ArgumentException("Invalid user.");

            return await _notesBL.ViewAllNotesBL(request.UserId);
        }
    }
}
