using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Labels.Command
{
    public class CreateLabelCommand : IRequest<LabelEntity>
    {
        public CreateLabelModel Model { get; }
        public int UserId { get; }

        public CreateLabelCommand(CreateLabelModel model, int userId)
        {
            Model = model;
            UserId = userId;
        }
    }
}
