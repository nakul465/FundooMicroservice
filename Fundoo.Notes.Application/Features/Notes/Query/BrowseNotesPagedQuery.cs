using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Query
{
    public class BrowseNotesPagedQuery : IRequest<PagedNotesResponseModel>
    {
        public BrowseNotesModel Model { get; }
        public int UserId { get; }

        public BrowseNotesPagedQuery(BrowseNotesModel model, int userId)
        {
            Model = model;
            UserId = userId;
        }
    }
}
