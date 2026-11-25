using MediatR;

namespace Fundoo.Notes.Application.Features.Labels.Command
{
    public class AttachLabelCommand : IRequest<bool>
    {
        public int NoteId { get; }
        public int LabelId { get; }
        public int UserId { get; }

        public AttachLabelCommand(int noteId, int labelId, int userId)
        {
            NoteId = noteId;
            LabelId = labelId;
            UserId = userId;
        }
    }
}
