using System.Security.Claims;
using Fundoo.Notes.Api.Authentication;
using Fundoo.Notes.Application.Features.Notes.Command;
using Fundoo.Notes.Application.Features.Notes.Query;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.Notes.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public class NotesController : ControllerBase
    {
        private readonly ISender _sender;

        public NotesController(ISender sender)
        {
            _sender = sender;
        }

        private int GetUserId()
        {
            // 1. Direct from authenticated user's JWT claims (OAuth)
            var claimVal = User.FindFirst("user_id")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
            if (int.TryParse(claimVal, out int id) && id > 0)
            {
                return id;
            }

            // 2. From forwarded X-User-Id (for M2M service-to-service calls with client credentials)
            if (Request.Headers.TryGetValue("X-User-Id", out var headerVal) &&
                int.TryParse(headerVal, out int headerUserId) && headerUserId > 0)
            {
                return headerUserId;
            }

            // 3. Fallback to HttpContext.Items["UserId"] set by filter
            if (HttpContext.Items.TryGetValue("UserId", out var userIdObj) && userIdObj is int userId && userId > 0)
            {
                return userId;
            }

            return 0;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateNote([FromBody] NoteCreationModel note)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new ResponseModel<NoteEntity>
                {
                    Success = false,
                    Message = "Unauthorized: Could not determine valid User ID from token.",
                    Data = null
                });
            }

            var entity = await _sender.Send(new CreateNoteCommand(note, userId));

            return Ok(new ResponseModel<NoteEntity>
            {
                Success = true,
                Message = "Note created successfully",
                Data = entity
            });
        }

        [HttpPost("view-by-id")]
        public async Task<IActionResult> ViewNoteById([FromBody] NotesIdModel notesId)
        {
            var userId = GetUserId();
            var note = await _sender.Send(new GetNoteByIdQuery(notesId, userId));

            if (note == null)
            {
                return NotFound(new ResponseModel<NoteEntity>
                {
                    Success = false,
                    Message = "Note not Found",
                    Data = null
                });
            }

            return Ok(new ResponseModel<NoteEntity>
            {
                Success = true,
                Message = "Note Found",
                Data = note
            });
        }

        [HttpGet("all")]
        public async Task<IActionResult> ViewAllNotes()
        {
            var userId = GetUserId();
            var notes = await _sender.Send(new GetAllNotesQuery(userId));

            return Ok(new ResponseModel<List<NoteEntity>>
            {
                Success = true,
                Message = "Notes Found",
                Data = notes
            });
        }

        [HttpPost("edit")]
        public async Task<IActionResult> EditNote([FromBody] EditNoteModel editNoteModel)
        {
            var userId = GetUserId();
            var editedNote = await _sender.Send(new EditNoteCommand(editNoteModel, userId));

            if (editedNote == null)
            {
                return NotFound(new ResponseModel<NoteEntity>
                {
                    Success = false,
                    Message = "Note not found",
                    Data = null
                });
            }

            return Ok(new ResponseModel<NoteEntity>
            {
                Success = true,
                Message = "Note updated successfully",
                Data = editedNote
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteNote([FromBody] NotesIdModel notesId)
        {
            var userId = GetUserId();
            var deletedNote = await _sender.Send(new DeleteNoteCommand(notesId, userId));

            if (deletedNote == null)
            {
                return NotFound(new ResponseModel<NoteEntity>
                {
                    Success = false,
                    Message = "Note not Found",
                    Data = null
                });
            }

            return Ok(new ResponseModel<NoteEntity>
            {
                Success = true,
                Message = "Note Deleted",
                Data = deletedNote
            });
        }

        [HttpPost("archive")]
        public async Task<IActionResult> ArchUnarchNote([FromBody] NotesIdModel notesId)
        {
            var userId = GetUserId();
            var affectedRows = await _sender.Send(new ArchiveNoteCommand(notesId, userId));

            if (affectedRows == 0)
            {
                return NotFound(new ResponseModel<NotesIdModel>
                {
                    Success = false,
                    Message = "Note not found",
                    Data = notesId
                });
            }

            return Ok(new ResponseModel<NotesIdModel>
            {
                Success = true,
                Message = "Note Archived/Unarchived",
                Data = notesId
            });
        }

        [HttpPost("trash")]
        public async Task<IActionResult> TrashUnTrash([FromBody] NotesIdModel notesId)
        {
            var userId = GetUserId();
            var affectedRows = await _sender.Send(new TrashNoteCommand(notesId, userId));

            if (affectedRows == 0)
            {
                return NotFound(new ResponseModel<NotesIdModel>
                {
                    Success = false,
                    Message = "Note not found",
                    Data = notesId
                });
            }

            return Ok(new ResponseModel<NotesIdModel>
            {
                Success = true,
                Message = "Note Trashed/Untrashed",
                Data = notesId
            });
        }
    }
}
