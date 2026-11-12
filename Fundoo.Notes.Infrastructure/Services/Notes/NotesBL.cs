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
    }
}
