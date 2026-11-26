using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Domain.Models
{
    public class SearchLabelledNotesModel
    {
        public int? LabelId { get; set; }
        public string? LabelName { get; set; }

        [Required(ErrorMessage = "Keyword is required.")]
        public string Keyword { get; set; } = string.Empty;
    }
}
