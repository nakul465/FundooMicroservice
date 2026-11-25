using System.Collections.Generic;
using System.Threading.Tasks;
using Fundoo.Notes.Application.Features.Labels.Services;
using Fundoo.Notes.Domain.Entity;

namespace Fundoo.Notes.Infrastructure.Services.Labels
{
    public class LabelBL : ILabelBL
    {
        private readonly ILabelRL _labelRL;

        public LabelBL(ILabelRL labelRL)
        {
            _labelRL = labelRL;
        }

        public async Task<LabelEntity> CreateLabelBL(LabelEntity label)
        {
            return await _labelRL.CreateLabelRL(label);
        }

        public async Task<List<LabelEntity>> GetLabelsByUserBL(int userId)
        {
            return await _labelRL.GetLabelsByUserRL(userId);
        }

        public async Task<LabelEntity?> GetLabelByIdBL(int labelId, int userId)
        {
            return await _labelRL.GetLabelByIdRL(labelId, userId);
        }

        public async Task<LabelEntity?> UpdateLabelBL(LabelEntity label)
        {
            return await _labelRL.UpdateLabelRL(label);
        }

        public async Task<bool> DeleteLabelBL(int labelId, int userId)
        {
            return await _labelRL.DeleteLabelRL(labelId, userId);
        }

        public async Task<bool> AttachLabelToNoteBL(int noteId, int labelId, int userId)
        {
            return await _labelRL.AttachLabelToNoteRL(noteId, labelId, userId);
        }

        public async Task<bool> DetachLabelFromNoteBL(int noteId, int labelId, int userId)
        {
            return await _labelRL.DetachLabelFromNoteRL(noteId, labelId, userId);
        }

        public async Task<Dictionary<int, List<LabelEntity>>> GetLabelsByNoteIdsBL(IEnumerable<int> noteIds)
        {
            return await _labelRL.GetLabelsByNoteIdsRL(noteIds);
        }
    }
}
