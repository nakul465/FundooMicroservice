using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;

namespace Fundoo.Notes.Application.Features.Notes.Services
{
    public interface INotesBL
    {
        Task<NoteEntity> AddNoteBL(NoteEntity note);
        Task<NoteEntity?> FindNoteByIdBL(NotesIdModel notesId, int userId);
        Task<List<NoteEntity>> ViewAllNotesBL(int userId);
        Task<NoteEntity?> EditNoteBL(NoteEntity note, int userId);
        Task<NoteEntity?> DeleteNoteBL(NotesIdModel notesId, int userId);
        Task<int> ArchUnarchNoteBL(NotesIdModel notesId, int userId);
        Task<int> TrashUnTrashBL(NotesIdModel notesId, int userId);
        Task<PagedNotesResponseModel> BrowseNotesPagedBL(BrowseNotesModel model, int userId);
        Task<List<NoteEntity>> SearchNotesByTitleBL(string title, int userId);
        Task<List<NoteEntity>> SearchNotesByContentBL(string content, int userId);
        Task<List<NoteEntity>> BrowseNotesByLabelBL(int? labelId, string? labelName, int userId);
        Task<List<NoteEntity>> SearchLabelledNotesBL(int? labelId, string? labelName, string keyword, int userId);
        Task<bool> VerifyLabelBelongsToUserBL(int labelId, int userId);
        Task<bool> VerifyLabelNameBelongsToUserBL(string labelName, int userId);
    }
}
