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
    public class SearchNotesByContentHandler : IRequestHandler<SearchNotesByContentQuery, List<NoteEntity>>
    {
        private readonly INotesBL _notesBL;

        public SearchNotesByContentHandler(INotesBL notesBL)
        {
            _notesBL = notesBL;
        }

        public async Task<List<NoteEntity>> Handle(SearchNotesByContentQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0)
                throw new UnauthorizedAccessException("Invalid user ID.");

            var phrase = request.Model.Content?.Trim();
            if (string.IsNullOrWhiteSpace(phrase))
                throw new ArgumentException("Content search phrase cannot be empty or whitespace.");

            return await _notesBL.SearchNotesByContentBL(phrase, request.UserId);
        }
    }
}
