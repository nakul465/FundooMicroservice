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
    public class BrowseNotesByLabelHandler : IRequestHandler<BrowseNotesByLabelQuery, List<NoteEntity>>
    {
        private readonly INotesBL _notesBL;

        public BrowseNotesByLabelHandler(INotesBL notesBL)
        {
            _notesBL = notesBL;
        }

        public async Task<List<NoteEntity>> Handle(BrowseNotesByLabelQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0)
                throw new UnauthorizedAccessException("Invalid user ID.");

            if (!request.Model.LabelId.HasValue && string.IsNullOrWhiteSpace(request.Model.LabelName))
                throw new ArgumentException("Either LabelId or LabelName must be specified.");

            return await _notesBL.BrowseNotesByLabelBL(request.Model.LabelId, request.Model.LabelName, request.UserId);
        }
    }
}
