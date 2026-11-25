using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Domain.Entity
{
    public class LabelEntity
    {
        [Key]
        public int LabelId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }
    }
}
