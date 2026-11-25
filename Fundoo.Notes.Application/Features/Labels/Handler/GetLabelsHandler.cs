using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fundoo.Notes.Application.Features.Labels.Query;
using Fundoo.Notes.Application.Features.Labels.Services;
using Fundoo.Notes.Domain.Entity;
using MediatR;

namespace Fundoo.Notes.Application.Features.Labels.Handler
{
    public class GetLabelsHandler : IRequestHandler<GetLabelsQuery, List<LabelEntity>>
    {
        private readonly ILabelBL _labelBL;

        public GetLabelsHandler(ILabelBL labelBL)
        {
            _labelBL = labelBL;
        }

        public async Task<List<LabelEntity>> Handle(GetLabelsQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0)
                return new List<LabelEntity>();

            return await _labelBL.GetLabelsByUserBL(request.UserId);
        }
    }
}
