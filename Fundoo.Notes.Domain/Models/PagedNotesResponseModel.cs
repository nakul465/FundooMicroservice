using System.Collections.Generic;
using Fundoo.Notes.Domain.Entity;

namespace Fundoo.Notes.Domain.Models
{
    public class PagedNotesResponseModel
    {
        public List<NoteEntity> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
