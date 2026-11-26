using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Domain.Models
{
    public class SearchNoteByTitleModel
    {
        [Required(ErrorMessage = "Title search keyword is required.")]
        public string Title { get; set; } = string.Empty;
    }
}
