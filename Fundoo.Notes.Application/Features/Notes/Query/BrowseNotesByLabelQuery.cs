using System.Collections.Generic;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Query
{
    public class BrowseNotesByLabelQuery : IRequest<List<NoteEntity>>
    {
        public BrowseNotesByLabelModel Model { get; }
        public int UserId { get; }

        public BrowseNotesByLabelQuery(BrowseNotesByLabelModel model, int userId)
        {
            Model = model;
            UserId = userId;
        }
    }
}
