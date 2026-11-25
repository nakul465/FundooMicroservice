using System;
using System.Threading;
using System.Threading.Tasks;
using Fundoo.Notes.Application.Features.Labels.Command;
using Fundoo.Notes.Application.Features.Labels.Services;
using Fundoo.Notes.Domain.Entity;
using MediatR;

namespace Fundoo.Notes.Application.Features.Labels.Handler
{
    public class UpdateLabelHandler : IRequestHandler<UpdateLabelCommand, LabelEntity?>
    {
        private readonly ILabelBL _labelBL;

        public UpdateLabelHandler(ILabelBL labelBL)
        {
            _labelBL = labelBL;
        }

        public async Task<LabelEntity?> Handle(UpdateLabelCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0)
                throw new ArgumentException("Invalid user ID.");

            var trimmedName = request.Model.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedName))
                throw new ArgumentException("Label name cannot be empty or whitespace.");

            if (trimmedName.Length > 100)
                throw new ArgumentException("Label name cannot exceed 100 characters.");

            var label = new LabelEntity
            {
                LabelId = request.LabelId,
                Name = trimmedName,
                UserId = request.UserId
            };

            return await _labelBL.UpdateLabelBL(label);
        }
    }
}
