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

        // 1. Create Note
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

            return StatusCode(StatusCodes.Status201Created, new ResponseModel<NoteEntity>
            {
                Success = true,
                Message = "Note created successfully",
                Data = entity
            });
        }

        // 2. View Note by ID
        [HttpGet("{noteId:int}")]
        public async Task<IActionResult> GetNoteById([FromRoute] int noteId)
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

            var note = await _sender.Send(new GetNoteByIdQuery(new NotesIdModel { NoteId = noteId }, userId));
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

        // 3. View All Notes
        [HttpGet("all")]
        public async Task<IActionResult> ViewAllNotes()
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new ResponseModel<List<NoteEntity>>
                {
                    Success = false,
                    Message = "Unauthorized: Could not determine valid User ID from token.",
                    Data = null
                });
            }

            var notes = await _sender.Send(new GetAllNotesQuery(userId));

            return Ok(new ResponseModel<List<NoteEntity>>
            {
                Success = true,
                Message = "Notes Found",
                Data = notes
            });
        }

        // 4. Update Note
        [HttpPut("{noteId:int}")]
        public async Task<IActionResult> UpdateNote([FromRoute] int noteId, [FromBody] EditNoteModel editNoteModel)
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

            editNoteModel.NoteId = noteId;
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

        // 5. Delete Note
        [HttpDelete("{noteId:int}")]
        public async Task<IActionResult> DeleteNote([FromRoute] int noteId)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized();
            }

            var deletedNote = await _sender.Send(new DeleteNoteCommand(new NotesIdModel { NoteId = noteId }, userId));

            if (deletedNote == null)
            {
                return NotFound(new ResponseModel<NoteEntity>
                {
                    Success = false,
                    Message = "Note not Found",
                    Data = null
                });
            }

            return NoContent();
        }

        // 6. Archive / Unarchive Note
        [HttpPatch("{noteId:int}/archive")]
        public async Task<IActionResult> ArchUnarchNote([FromRoute] int noteId)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized();
            }

            var notesId = new NotesIdModel { NoteId = noteId };
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

        // 7. Trash / Untrash Note
        [HttpPatch("{noteId:int}/trash")]
        public async Task<IActionResult> TrashUnTrash([FromRoute] int noteId)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized();
            }

            var notesId = new NotesIdModel { NoteId = noteId };
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

        // 8. Expectation 1 & 7: Browsing a Large Notes Collection (with Pagination)
        [HttpGet("browse")]
        public async Task<IActionResult> BrowseNotesPaged([FromQuery] BrowseNotesModel model)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new ResponseModel<PagedNotesResponseModel>
                {
                    Success = false,
                    Message = "Unauthorized: Could not determine valid User ID from token.",
                    Data = null
                });
            }

            try
            {
                var result = await _sender.Send(new BrowseNotesPagedQuery(model, userId));

                string message;
                if (result.TotalCount == 0)
                {
                    message = "No matching notes found.";
                }
                else if (model.PageNumber > result.TotalPages)
                {
                    message = $"Page is out of range. No notes found on page {model.PageNumber}.";
                }
                else
                {
                    message = "Notes retrieved successfully";
                }

                return Ok(new ResponseModel<PagedNotesResponseModel>
                {
                    Success = true,
                    Message = message,
                    Data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseModel<PagedNotesResponseModel>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        // Expectation 2 & 6: Finding a Note by Its Title
        [HttpGet("search/title")]
        public async Task<IActionResult> SearchByTitle([FromQuery] SearchNoteByTitleModel model)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new ResponseModel<List<NoteEntity>>
                {
                    Success = false,
                    Message = "Unauthorized: Could not determine valid User ID from token.",
                    Data = null
                });
            }

            try
            {
                var notes = await _sender.Send(new SearchNotesByTitleQuery(model, userId));

                return Ok(new ResponseModel<List<NoteEntity>>
                {
                    Success = true,
                    Message = notes.Count == 0 ? "No matching notes found." : "Notes found matching title search.",
                    Data = notes
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseModel<List<NoteEntity>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        // Expectation 3 & 6: Finding Information Within a Note (Content / Description)
        [HttpGet("search/content")]
        public async Task<IActionResult> SearchByContent([FromQuery] SearchNoteByContentModel model)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new ResponseModel<List<NoteEntity>>
                {
                    Success = false,
                    Message = "Unauthorized: Could not determine valid User ID from token.",
                    Data = null
                });
            }

            try
            {
                var notes = await _sender.Send(new SearchNotesByContentQuery(model, userId));

                return Ok(new ResponseModel<List<NoteEntity>>
                {
                    Success = true,
                    Message = notes.Count == 0 ? "No matching notes found." : "Notes found matching content search.",
                    Data = notes
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseModel<List<NoteEntity>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        // Expectation 4, 6 & 7: Browsing Notes by Label
        [HttpGet("by-label")]
        public async Task<IActionResult> BrowseByLabel([FromQuery] BrowseNotesByLabelModel model)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new ResponseModel<List<NoteEntity>>
                {
                    Success = false,
                    Message = "Unauthorized: Could not determine valid User ID from token.",
                    Data = null
                });
            }

            try
            {
                var notes = await _sender.Send(new BrowseNotesByLabelQuery(model, userId));

                return Ok(new ResponseModel<List<NoteEntity>>
                {
                    Success = true,
                    Message = notes.Count == 0 ? "No matching notes found." : "Labelled notes retrieved successfully.",
                    Data = notes
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseModel<List<NoteEntity>>
                {
                    Success = false,
                    Message = "Label not found.",
                    Data = null
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseModel<List<NoteEntity>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        // Expectation 5 & 6: Finding a Note Within a Labelled Collection (Label + Keyword)
        [HttpGet("by-label/search")]
        public async Task<IActionResult> SearchLabelledNotes([FromQuery] SearchLabelledNotesModel model)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new ResponseModel<List<NoteEntity>>
                {
                    Success = false,
                    Message = "Unauthorized: Could not determine valid User ID from token.",
                    Data = null
                });
            }

            try
            {
                var notes = await _sender.Send(new SearchLabelledNotesQuery(model, userId));

                return Ok(new ResponseModel<List<NoteEntity>>
                {
                    Success = true,
                    Message = notes.Count == 0 ? "No matching notes found." : "Matching notes found in labelled collection.",
                    Data = notes
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseModel<List<NoteEntity>>
                {
                    Success = false,
                    Message = "Label not found.",
                    Data = null
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseModel<List<NoteEntity>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }
    }
}
