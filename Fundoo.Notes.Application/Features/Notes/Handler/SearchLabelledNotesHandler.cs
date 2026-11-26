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
    public class SearchLabelledNotesHandler : IRequestHandler<SearchLabelledNotesQuery, List<NoteEntity>>
    {
        private readonly INotesBL _notesBL;

        public SearchLabelledNotesHandler(INotesBL notesBL)
        {
            _notesBL = notesBL;
        }

        public async Task<List<NoteEntity>> Handle(SearchLabelledNotesQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0)
                throw new UnauthorizedAccessException("Invalid user ID.");

            var keyword = request.Model.Keyword?.Trim();
            if (string.IsNullOrWhiteSpace(keyword))
                throw new ArgumentException("Search keyword cannot be empty or whitespace.");

            if (!request.Model.LabelId.HasValue && string.IsNullOrWhiteSpace(request.Model.LabelName))
                throw new ArgumentException("Either LabelId or LabelName must be specified.");

            return await _notesBL.SearchLabelledNotesBL(request.Model.LabelId, request.Model.LabelName, keyword, request.UserId);
        }
    }
}
