using System.Threading;
using System.Threading.Tasks;
using Fundoo.Notes.Application.Features.Labels.Command;
using Fundoo.Notes.Application.Features.Labels.Services;
using MediatR;

namespace Fundoo.Notes.Application.Features.Labels.Handler
{
    public class DeleteLabelHandler : IRequestHandler<DeleteLabelCommand, bool>
    {
        private readonly ILabelBL _labelBL;

        public DeleteLabelHandler(ILabelBL labelBL)
        {
            _labelBL = labelBL;
        }

        public async Task<bool> Handle(DeleteLabelCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0 || request.LabelId <= 0)
                return false;

            return await _labelBL.DeleteLabelBL(request.LabelId, request.UserId);
        }
    }
}
