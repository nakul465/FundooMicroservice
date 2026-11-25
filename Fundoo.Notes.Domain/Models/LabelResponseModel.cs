namespace Fundoo.Notes.Domain.Models
{
    public class LabelResponseModel
    {
        public int LabelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
