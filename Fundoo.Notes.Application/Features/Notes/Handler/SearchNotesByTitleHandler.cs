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
    public class SearchNotesByTitleHandler : IRequestHandler<SearchNotesByTitleQuery, List<NoteEntity>>
    {
        private readonly INotesBL _notesBL;

        public SearchNotesByTitleHandler(INotesBL notesBL)
        {
            _notesBL = notesBL;
        }

        public async Task<List<NoteEntity>> Handle(SearchNotesByTitleQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0)
                throw new UnauthorizedAccessException("Invalid user ID.");

            var term = request.Model.Title?.Trim();
            if (string.IsNullOrWhiteSpace(term))
                throw new ArgumentException("Title search keyword cannot be empty or whitespace.");

            return await _notesBL.SearchNotesByTitleBL(term, request.UserId);
        }
    }
}
