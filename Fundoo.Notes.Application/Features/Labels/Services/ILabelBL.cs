using System.Collections.Generic;
using System.Threading.Tasks;
using Fundoo.Notes.Domain.Entity;

namespace Fundoo.Notes.Application.Features.Labels.Services
{
    public interface ILabelBL
    {
        Task<LabelEntity> CreateLabelBL(LabelEntity label);
        Task<List<LabelEntity>> GetLabelsByUserBL(int userId);
        Task<LabelEntity?> GetLabelByIdBL(int labelId, int userId);
        Task<LabelEntity?> UpdateLabelBL(LabelEntity label);
        Task<bool> DeleteLabelBL(int labelId, int userId);
        Task<bool> AttachLabelToNoteBL(int noteId, int labelId, int userId);
        Task<bool> DetachLabelFromNoteBL(int noteId, int labelId, int userId);
        Task<Dictionary<int, List<LabelEntity>>> GetLabelsByNoteIdsBL(IEnumerable<int> noteIds);
    }
}
