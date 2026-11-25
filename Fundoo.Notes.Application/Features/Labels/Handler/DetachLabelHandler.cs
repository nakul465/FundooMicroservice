using System.Threading;
using System.Threading.Tasks;
using Fundoo.Notes.Application.Features.Labels.Command;
using Fundoo.Notes.Application.Features.Labels.Services;
using MediatR;

namespace Fundoo.Notes.Application.Features.Labels.Handler
{
    public class DetachLabelHandler : IRequestHandler<DetachLabelCommand, bool>
    {
        private readonly ILabelBL _labelBL;

        public DetachLabelHandler(ILabelBL labelBL)
        {
            _labelBL = labelBL;
        }

        public async Task<bool> Handle(DetachLabelCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0 || request.NoteId <= 0 || request.LabelId <= 0)
                return false;

            return await _labelBL.DetachLabelFromNoteBL(request.NoteId, request.LabelId, request.UserId);
        }
    }
}
