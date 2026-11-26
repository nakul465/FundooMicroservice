using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Domain.Models
{
    public class SearchNoteByContentModel
    {
        [Required(ErrorMessage = "Content search phrase is required.")]
        public string Content { get; set; } = string.Empty;
    }
}
