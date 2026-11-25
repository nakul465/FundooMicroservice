using System.Collections.Generic;
using System.Threading.Tasks;
using Fundoo.Notes.Domain.Entity;

namespace Fundoo.Notes.Application.Features.Labels.Services
{
    public interface ILabelRL
    {
        Task<LabelEntity> CreateLabelRL(LabelEntity label);
        Task<List<LabelEntity>> GetLabelsByUserRL(int userId);
        Task<LabelEntity?> GetLabelByIdRL(int labelId, int userId);
        Task<LabelEntity?> UpdateLabelRL(LabelEntity label);
        Task<bool> DeleteLabelRL(int labelId, int userId);
        Task<bool> AttachLabelToNoteRL(int noteId, int labelId, int userId);
        Task<bool> DetachLabelFromNoteRL(int noteId, int labelId, int userId);
        Task<Dictionary<int, List<LabelEntity>>> GetLabelsByNoteIdsRL(IEnumerable<int> noteIds);
    }
}
