using System.Collections.Generic;
using Fundoo.Notes.Domain.Entity;
using MediatR;

namespace Fundoo.Notes.Application.Features.Labels.Query
{
    public class GetLabelsQuery : IRequest<List<LabelEntity>>
    {
        public int UserId { get; }

        public GetLabelsQuery(int userId)
        {
            UserId = userId;
        }
    }
}
