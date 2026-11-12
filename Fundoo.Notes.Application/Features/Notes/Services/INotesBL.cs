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
    }
}
