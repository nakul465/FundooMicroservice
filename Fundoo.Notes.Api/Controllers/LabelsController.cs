using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Fundoo.Notes.Api.Authentication;
using Fundoo.Notes.Application.Features.Labels.Command;
using Fundoo.Notes.Application.Features.Labels.Query;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Exceptions;
using Fundoo.Notes.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.Notes.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public class LabelsController : ControllerBase
    {
        private readonly ISender _sender;

        public LabelsController(ISender sender)
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

        [HttpGet]
        public async Task<IActionResult> GetAllLabels()
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Unauthorized: Could not determine valid User ID from token." });
            }

            var labels = await _sender.Send(new GetLabelsQuery(userId));
            return Ok(labels);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLabel([FromBody] CreateLabelModel model)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Unauthorized: Could not determine valid User ID from token." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var trimmed = model?.Name?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return BadRequest(new { message = "Label name cannot be empty or whitespace." });
            }

            try
            {
                var label = await _sender.Send(new CreateLabelCommand(model!, userId));
                return StatusCode(201, label);
            }
            catch (DuplicateLabelException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{labelId:int}")]
        public async Task<IActionResult> UpdateLabel([FromRoute] int labelId, [FromBody] UpdateLabelModel model)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Unauthorized: Could not determine valid User ID from token." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var trimmed = model?.Name?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return BadRequest(new { message = "Label name cannot be empty or whitespace." });
            }

            try
            {
                var label = await _sender.Send(new UpdateLabelCommand(labelId, model!, userId));
                if (label == null)
                {
                    return NotFound(new { message = "Label not found." });
                }

                return Ok(label);
            }
            catch (DuplicateLabelException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{labelId:int}")]
        public async Task<IActionResult> DeleteLabel([FromRoute] int labelId)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Unauthorized: Could not determine valid User ID from token." });
            }

            var success = await _sender.Send(new DeleteLabelCommand(labelId, userId));
            if (!success)
            {
                return NotFound(new { message = "Label not found." });
            }

            return NoContent();
        }

        // Attach label to note: PUT /api/v1/notes/{noteId}/labels/{labelId}
        [HttpPut("/api/v1/notes/{noteId:int}/labels/{labelId:int}")]
        public async Task<IActionResult> AttachLabel([FromRoute] int noteId, [FromRoute] int labelId)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Unauthorized: Could not determine valid User ID from token." });
            }

            var success = await _sender.Send(new AttachLabelCommand(noteId, labelId, userId));
            if (!success)
            {
                return NotFound(new { message = "Note or label not found." });
            }

            return NoContent();
        }

        // Detach label from note: DELETE /api/v1/notes/{noteId}/labels/{labelId}
        [HttpDelete("/api/v1/notes/{noteId:int}/labels/{labelId:int}")]
        public async Task<IActionResult> DetachLabel([FromRoute] int noteId, [FromRoute] int labelId)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Unauthorized: Could not determine valid User ID from token." });
            }

            var success = await _sender.Send(new DetachLabelCommand(noteId, labelId, userId));
            if (!success)
            {
                return NotFound(new { message = "Note or label not found." });
            }

            return NoContent();
        }
    }
}
