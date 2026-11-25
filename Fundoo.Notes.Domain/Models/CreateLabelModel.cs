using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Domain.Models
{
    public class CreateLabelModel
    {
        [Required(ErrorMessage = "Label name is required.")]
        [StringLength(100, ErrorMessage = "Label name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}
