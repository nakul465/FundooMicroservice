using MediatR;

namespace Fundoo.Notes.Application.Features.Labels.Command
{
    public class DetachLabelCommand : IRequest<bool>
    {
        public int NoteId { get; }
        public int LabelId { get; }
        public int UserId { get; }

        public DetachLabelCommand(int noteId, int labelId, int userId)
        {
            NoteId = noteId;
            LabelId = labelId;
            UserId = userId;
        }
    }
}
