using MediatR;

namespace Fundoo.Notes.Application.Features.Labels.Command
{
    public class DeleteLabelCommand : IRequest<bool>
    {
        public int LabelId { get; }
        public int UserId { get; }

        public DeleteLabelCommand(int labelId, int userId)
        {
            LabelId = labelId;
            UserId = userId;
        }
    }
}
