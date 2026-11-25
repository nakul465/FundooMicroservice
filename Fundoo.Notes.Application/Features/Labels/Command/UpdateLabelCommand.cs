using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Labels.Command
{
    public class UpdateLabelCommand : IRequest<LabelEntity?>
    {
        public int LabelId { get; }
        public UpdateLabelModel Model { get; }
        public int UserId { get; }

        public UpdateLabelCommand(int labelId, UpdateLabelModel model, int userId)
        {
            LabelId = labelId;
            Model = model;
            UserId = userId;
        }
    }
}
