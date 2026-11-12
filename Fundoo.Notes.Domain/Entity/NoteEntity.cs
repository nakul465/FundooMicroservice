using System;
using System.ComponentModel.DataAnnotations;

namespace Fundoo.Notes.Domain.Entity
{
	public class NoteEntity
	{
        [Key]
        public int NoteId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Colour { get; set; } = string.Empty;

        public bool IsArchived { get; set; }

        public bool IsDeleted { get; set; }

        [Required]
        public int UserId { get; set; }
    }
}

