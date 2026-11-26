using System.Collections.Generic;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Query
{
    public class SearchNotesByContentQuery : IRequest<List<NoteEntity>>
    {
        public SearchNoteByContentModel Model { get; }
        public int UserId { get; }

        public SearchNotesByContentQuery(SearchNoteByContentModel model, int userId)
        {
            Model = model;
            UserId = userId;
        }
    }
}
