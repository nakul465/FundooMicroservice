using System;
using System.Collections.Generic;
using Fundoo.Notes.Domain.Entity;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Query
{
    public class GetAllNotesQuery : IRequest<List<NoteEntity>>
    {
        public int UserId { get; }

        public GetAllNotesQuery(int userId)
        {
            UserId = userId;
        }
    }
}
