using System.Collections.Generic;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Query
{
    public class SearchNotesByTitleQuery : IRequest<List<NoteEntity>>
    {
        public SearchNoteByTitleModel Model { get; }
        public int UserId { get; }

        public SearchNotesByTitleQuery(SearchNoteByTitleModel model, int userId)
        {
            Model = model;
            UserId = userId;
        }
    }
}
