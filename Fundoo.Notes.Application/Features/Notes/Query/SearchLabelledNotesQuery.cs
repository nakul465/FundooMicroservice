using System.Collections.Generic;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Query
{
    public class SearchLabelledNotesQuery : IRequest<List<NoteEntity>>
    {
        public SearchLabelledNotesModel Model { get; }
        public int UserId { get; }

        public SearchLabelledNotesQuery(SearchLabelledNotesModel model, int userId)
        {
            Model = model;
            UserId = userId;
        }
    }
}
