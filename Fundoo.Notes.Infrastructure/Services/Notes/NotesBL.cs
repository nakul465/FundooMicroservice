using System.Collections.Generic;
using System.Threading.Tasks;
using Fundoo.Notes.Application.Features.Notes.Services;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;

namespace Fundoo.Notes.Infrastructure.Services.Notes
{
    public class NotesBL : INotesBL
    {
        private readonly INotesRL _notesRL;

        public NotesBL(INotesRL notesRL)
        {
            _notesRL = notesRL;
        }

        public async Task<NoteEntity> AddNoteBL(NoteEntity note)
        {
            return await _notesRL.AddNoteRL(note);
        }

        public async Task<NoteEntity?> FindNoteByIdBL(NotesIdModel notesId, int userId)
        {
            return await _notesRL.FindNoteByIdRL(notesId, userId);
        }

        public async Task<List<NoteEntity>> ViewAllNotesBL(int userId)
        {
            return await _notesRL.ViewAllNotesRL(userId);
        }

        public async Task<NoteEntity?> EditNoteBL(NoteEntity note, int userId)
        {
            return await _notesRL.EditNoteRL(note, userId);
        }

        public async Task<NoteEntity?> DeleteNoteBL(NotesIdModel notesId, int userId)
        {
            return await _notesRL.DeleteNoteRL(notesId, userId);
        }

        public async Task<int> ArchUnarchNoteBL(NotesIdModel notesId, int userId)
        {
            return await _notesRL.ArchUnarchNoteRL(notesId, userId);
        }

        public async Task<int> TrashUnTrashBL(NotesIdModel notesId, int userId)
        {
            return await _notesRL.TrashUnTrashRL(notesId, userId);
        }

        public async Task<PagedNotesResponseModel> BrowseNotesPagedBL(BrowseNotesModel model, int userId)
        {
            return await _notesRL.BrowseNotesPagedRL(model, userId);
        }

        public async Task<List<NoteEntity>> SearchNotesByTitleBL(string title, int userId)
        {
            return await _notesRL.SearchNotesByTitleRL(title, userId);
        }

        public async Task<List<NoteEntity>> SearchNotesByContentBL(string content, int userId)
        {
            return await _notesRL.SearchNotesByContentRL(content, userId);
        }

        public async Task<List<NoteEntity>> BrowseNotesByLabelBL(int? labelId, string? labelName, int userId)
        {
            return await _notesRL.BrowseNotesByLabelRL(labelId, labelName, userId);
        }

        public async Task<List<NoteEntity>> SearchLabelledNotesBL(int? labelId, string? labelName, string keyword, int userId)
        {
            return await _notesRL.SearchLabelledNotesRL(labelId, labelName, keyword, userId);
        }

        public async Task<bool> VerifyLabelBelongsToUserBL(int labelId, int userId)
        {
            return await _notesRL.VerifyLabelBelongsToUserRL(labelId, userId);
        }

        public async Task<bool> VerifyLabelNameBelongsToUserBL(string labelName, int userId)
        {
            return await _notesRL.VerifyLabelNameBelongsToUserRL(labelName, userId);
        }
    }
}
