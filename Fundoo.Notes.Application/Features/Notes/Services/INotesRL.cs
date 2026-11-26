using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;

namespace Fundoo.Notes.Application.Features.Notes.Services
{
    public interface INotesRL
    {
        Task<NoteEntity> AddNoteRL(NoteEntity note);
        Task<NoteEntity?> FindNoteByIdRL(NotesIdModel notesId, int userId);
        Task<List<NoteEntity>> ViewAllNotesRL(int userId);
        Task<NoteEntity?> DeleteNoteRL(NotesIdModel notesId, int userId);
        Task<int> ArchUnarchNoteRL(NotesIdModel notesId, int userId);
        Task<int> TrashUnTrashRL(NotesIdModel notesId, int userId);
        Task<NoteEntity?> EditNoteRL(NoteEntity note, int userId);
        Task<PagedNotesResponseModel> BrowseNotesPagedRL(BrowseNotesModel model, int userId);
        Task<List<NoteEntity>> SearchNotesByTitleRL(string title, int userId);
        Task<List<NoteEntity>> SearchNotesByContentRL(string content, int userId);
        Task<List<NoteEntity>> BrowseNotesByLabelRL(int? labelId, string? labelName, int userId);
        Task<List<NoteEntity>> SearchLabelledNotesRL(int? labelId, string? labelName, string keyword, int userId);
        Task<bool> VerifyLabelBelongsToUserRL(int labelId, int userId);
        Task<bool> VerifyLabelNameBelongsToUserRL(string labelName, int userId);
    }
}
