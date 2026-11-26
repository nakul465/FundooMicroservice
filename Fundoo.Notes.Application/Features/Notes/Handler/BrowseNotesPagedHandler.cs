using System;
using System.Threading;
using System.Threading.Tasks;
using Fundoo.Notes.Application.Features.Notes.Query;
using Fundoo.Notes.Application.Features.Notes.Services;
using Fundoo.Notes.Domain.Models;
using MediatR;

namespace Fundoo.Notes.Application.Features.Notes.Handler
{
    public class BrowseNotesPagedHandler : IRequestHandler<BrowseNotesPagedQuery, PagedNotesResponseModel>
    {
        private readonly INotesBL _notesBL;

        public BrowseNotesPagedHandler(INotesBL notesBL)
        {
            _notesBL = notesBL;
        }

        public async Task<PagedNotesResponseModel> Handle(BrowseNotesPagedQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0)
                throw new UnauthorizedAccessException("Invalid user ID.");

            if (request.Model.PageNumber < 1)
                throw new ArgumentException("PageNumber must be greater than or equal to 1.");

            if (request.Model.PageSize < 1)
                throw new ArgumentException("PageSize must be greater than or equal to 1.");

            if (request.Model.PageSize > 100)
                throw new ArgumentException("PageSize cannot exceed 100. Maximum allowed items per page is 100.");

            return await _notesBL.BrowseNotesPagedBL(request.Model, request.UserId);
        }
    }
}
