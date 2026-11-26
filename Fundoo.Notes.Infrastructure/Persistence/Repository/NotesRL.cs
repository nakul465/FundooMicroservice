using Fundoo.Notes.Application.Features.Notes.Services;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Fundoo.Notes.Infrastructure.Persistence.Repository
{
    public class NotesRL : INotesRL
    {
        private readonly string? _connectionString;

        public NotesRL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("FundooNotesConnection");
        }

        private NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public async Task<NoteEntity> AddNoteRL(NoteEntity note)
        {
            const string query = @"
                INSERT INTO ""Notes"" (""Title"", ""Description"", ""Colour"", ""IsArchived"", ""IsDeleted"", ""UserId"")
                VALUES (@Title, @Description, @Colour, @IsArchived, @IsDeleted, @UserId)
                RETURNING ""NoteId"";";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Title", note.Title ?? string.Empty);
            command.Parameters.AddWithValue("@Description", (object?)note.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Colour", (object?)note.Colour ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsArchived", note.IsArchived);
            command.Parameters.AddWithValue("@IsDeleted", note.IsDeleted);
            command.Parameters.AddWithValue("@UserId", note.UserId);

            var generatedId = await command.ExecuteScalarAsync();
            if (generatedId != null && int.TryParse(generatedId.ToString(), out int id))
            {
                note.NoteId = id;
            }

            return note;
        }

        public async Task<NoteEntity?> FindNoteByIdRL(NotesIdModel notesId, int userId)
        {
            const string query = @"
                SELECT ""NoteId"", ""Title"", ""Description"", ""Colour"", ""IsArchived"", ""IsDeleted"", ""UserId""
                FROM ""Notes""
                WHERE ""UserId"" = @UserId AND ""NoteId"" = @NoteId AND ""IsDeleted"" = FALSE
                LIMIT 1;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@NoteId", notesId.NoteId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var updatedNote = MapReaderToNote(reader);
                await reader.CloseAsync();
                await PopulateLabelsForNotesAsync(new List<NoteEntity> { updatedNote });
                return updatedNote;
            }

            return null;
        }

        public async Task<List<NoteEntity>> ViewAllNotesRL(int userId)
        {
            const string query = @"
                SELECT ""NoteId"", ""Title"", ""Description"", ""Colour"", ""IsArchived"", ""IsDeleted"", ""UserId""
                FROM ""Notes""
                WHERE ""UserId"" = @UserId AND ""IsArchived"" = FALSE AND ""IsDeleted"" = FALSE;";

            var notes = new List<NoteEntity>();

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notes.Add(MapReaderToNote(reader));
            }

            await reader.CloseAsync();
            await PopulateLabelsForNotesAsync(notes);

            return notes;
        }

        public async Task<NoteEntity?> DeleteNoteRL(NotesIdModel notesId, int userId)
        {
            const string query = @"
                DELETE FROM ""Notes""
                WHERE ""UserId"" = @UserId AND ""NoteId"" = @NoteId
                RETURNING ""NoteId"", ""Title"", ""Description"", ""Colour"", ""IsArchived"", ""IsDeleted"", ""UserId"";";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@NoteId", notesId.NoteId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var updatedNote = MapReaderToNote(reader);
                await reader.CloseAsync();
                await PopulateLabelsForNotesAsync(new List<NoteEntity> { updatedNote });
                return updatedNote;
            }

            return null;
        }

        public async Task<int> ArchUnarchNoteRL(NotesIdModel notesId, int userId)
        {
            const string query = @"
                UPDATE ""Notes""
                SET ""IsArchived"" = NOT ""IsArchived""
                WHERE ""UserId"" = @UserId AND ""NoteId"" = @NoteId AND ""IsDeleted"" = FALSE;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@NoteId", notesId.NoteId);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> TrashUnTrashRL(NotesIdModel notesId, int userId)
        {
            const string query = @"
                UPDATE ""Notes""
                SET ""IsDeleted"" = NOT ""IsDeleted""
                WHERE ""UserId"" = @UserId AND ""NoteId"" = @NoteId;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@NoteId", notesId.NoteId);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<NoteEntity?> EditNoteRL(NoteEntity note, int userId)
        {
            const string query = @"
                UPDATE ""Notes""
                SET ""Title"" = @Title,
                    ""Description"" = @Description,
                    ""Colour"" = @Colour
                WHERE ""UserId"" = @UserId AND ""NoteId"" = @NoteId AND ""IsDeleted"" = FALSE
                RETURNING ""NoteId"", ""Title"", ""Description"", ""Colour"", ""IsArchived"", ""IsDeleted"", ""UserId"";";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Title", note.Title ?? string.Empty);
            command.Parameters.AddWithValue("@Description", (object?)note.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Colour", (object?)note.Colour ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@NoteId", note.NoteId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var updatedNote = MapReaderToNote(reader);
                await reader.CloseAsync();
                await PopulateLabelsForNotesAsync(new List<NoteEntity> { updatedNote });
                return updatedNote;
            }

            return null;
        }

        private async Task PopulateLabelsForNotesAsync(List<NoteEntity> notes)
        {
            if (notes == null || notes.Count == 0) return;

            var noteIds = notes.Select(n => n.NoteId).Distinct().ToArray();
            const string query = @"
                SELECT nl.""NoteId"", l.""LabelId"", l.""Name"", l.""UserId""
                FROM ""NoteLabels"" nl
                INNER JOIN ""Labels"" l ON nl.""LabelId"" = l.""LabelId""
                WHERE nl.""NoteId"" = ANY(@NoteIds)
                ORDER BY l.""Name"" ASC;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@NoteIds", noteIds);

            var labelMap = notes.ToDictionary(n => n.NoteId, _ => new List<LabelEntity>());

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var noteId = reader.GetInt32(reader.GetOrdinal("NoteId"));
                var label = new LabelEntity
                {
                    LabelId = reader.GetInt32(reader.GetOrdinal("LabelId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId"))
                };

                if (labelMap.TryGetValue(noteId, out var list))
                {
                    list.Add(label);
                }
            }

            foreach (var note in notes)
            {
                note.Labels = labelMap.TryGetValue(note.NoteId, out var list) ? list : new List<LabelEntity>();
            }
        }

        public async Task<PagedNotesResponseModel> BrowseNotesPagedRL(BrowseNotesModel model, int userId)
        {
            var offset = (model.PageNumber - 1) * model.PageSize;

            const string countQuery = @"
                SELECT COUNT(*)
                FROM ""Notes""
                WHERE ""UserId"" = @UserId AND ""IsDeleted"" = FALSE;";

            const string dataQuery = @"
                SELECT ""NoteId"", ""Title"", ""Description"", ""Colour"", ""IsArchived"", ""IsDeleted"", ""UserId""
                FROM ""Notes""
                WHERE ""UserId"" = @UserId AND ""IsDeleted"" = FALSE
                ORDER BY ""NoteId"" DESC
                LIMIT @PageSize OFFSET @Offset;";

            var notes = new List<NoteEntity>();
            int totalCount = 0;

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using (var countCmd = new NpgsqlCommand(countQuery, connection))
            {
                countCmd.Parameters.AddWithValue("@UserId", userId);
                var scalar = await countCmd.ExecuteScalarAsync();
                totalCount = Convert.ToInt32(scalar);
            }

            await using (var dataCmd = new NpgsqlCommand(dataQuery, connection))
            {
                dataCmd.Parameters.AddWithValue("@UserId", userId);
                dataCmd.Parameters.AddWithValue("@PageSize", model.PageSize);
                dataCmd.Parameters.AddWithValue("@Offset", offset);

                await using var reader = await dataCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    notes.Add(MapReaderToNote(reader));
                }
            }

            await PopulateLabelsForNotesAsync(notes);

            int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / model.PageSize);

            return new PagedNotesResponseModel
            {
                Items = notes,
                PageNumber = model.PageNumber,
                PageSize = model.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<List<NoteEntity>> SearchNotesByTitleRL(string title, int userId)
        {
            const string query = @"
                SELECT ""NoteId"", ""Title"", ""Description"", ""Colour"", ""IsArchived"", ""IsDeleted"", ""UserId""
                FROM ""Notes""
                WHERE ""UserId"" = @UserId AND ""IsDeleted"" = FALSE AND ""Title"" ILIKE '%' || @Title || '%'
                ORDER BY ""NoteId"" DESC;";

            var notes = new List<NoteEntity>();

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Title", title.Trim());

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notes.Add(MapReaderToNote(reader));
            }

            await reader.CloseAsync();
            await PopulateLabelsForNotesAsync(notes);

            return notes;
        }

        public async Task<List<NoteEntity>> SearchNotesByContentRL(string content, int userId)
        {
            const string query = @"
                SELECT ""NoteId"", ""Title"", ""Description"", ""Colour"", ""IsArchived"", ""IsDeleted"", ""UserId""
                FROM ""Notes""
                WHERE ""UserId"" = @UserId AND ""IsDeleted"" = FALSE AND ""Description"" ILIKE '%' || @Content || '%'
                ORDER BY ""NoteId"" DESC;";

            var notes = new List<NoteEntity>();

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Content", content.Trim());

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notes.Add(MapReaderToNote(reader));
            }

            await reader.CloseAsync();
            await PopulateLabelsForNotesAsync(notes);

            return notes;
        }

        public async Task<bool> VerifyLabelBelongsToUserRL(int labelId, int userId)
        {
            const string query = @"
                SELECT 1 FROM ""Labels""
                WHERE ""LabelId"" = @LabelId AND ""UserId"" = @UserId LIMIT 1;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@LabelId", labelId);
            command.Parameters.AddWithValue("@UserId", userId);

            var result = await command.ExecuteScalarAsync();
            return result != null;
        }

        public async Task<bool> VerifyLabelNameBelongsToUserRL(string labelName, int userId)
        {
            const string query = @"
                SELECT 1 FROM ""Labels""
                WHERE LOWER(""Name"") = LOWER(@LabelName) AND ""UserId"" = @UserId LIMIT 1;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@LabelName", labelName.Trim());
            command.Parameters.AddWithValue("@UserId", userId);

            var result = await command.ExecuteScalarAsync();
            return result != null;
        }

        public async Task<List<NoteEntity>> BrowseNotesByLabelRL(int? labelId, string? labelName, int userId)
        {
            if (labelId.HasValue)
            {
                if (!await VerifyLabelBelongsToUserRL(labelId.Value, userId))
                    throw new KeyNotFoundException("Label not found.");
            }
            else if (!string.IsNullOrWhiteSpace(labelName))
            {
                if (!await VerifyLabelNameBelongsToUserRL(labelName, userId))
                    throw new KeyNotFoundException("Label not found.");
            }
            else
            {
                throw new ArgumentException("Either LabelId or LabelName must be provided.");
            }

            string query = @"
                SELECT DISTINCT n.""NoteId"", n.""Title"", n.""Description"", n.""Colour"", n.""IsArchived"", n.""IsDeleted"", n.""UserId""
                FROM ""Notes"" n
                INNER JOIN ""NoteLabels"" nl ON n.""NoteId"" = nl.""NoteId""
                INNER JOIN ""Labels"" l ON nl.""LabelId"" = l.""LabelId""
                WHERE n.""UserId"" = @UserId AND n.""IsDeleted"" = FALSE";

            if (labelId.HasValue)
            {
                query += @" AND nl.""LabelId"" = @LabelId";
            }
            else
            {
                query += @" AND LOWER(l.""Name"") = LOWER(@LabelName)";
            }

            query += @" ORDER BY n.""NoteId"" DESC;";

            var notes = new List<NoteEntity>();

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            if (labelId.HasValue)
            {
                command.Parameters.AddWithValue("@LabelId", labelId.Value);
            }
            else
            {
                command.Parameters.AddWithValue("@LabelName", labelName!.Trim());
            }

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notes.Add(MapReaderToNote(reader));
            }

            await reader.CloseAsync();
            await PopulateLabelsForNotesAsync(notes);

            return notes;
        }

        public async Task<List<NoteEntity>> SearchLabelledNotesRL(int? labelId, string? labelName, string keyword, int userId)
        {
            if (labelId.HasValue)
            {
                if (!await VerifyLabelBelongsToUserRL(labelId.Value, userId))
                    throw new KeyNotFoundException("Label not found.");
            }
            else if (!string.IsNullOrWhiteSpace(labelName))
            {
                if (!await VerifyLabelNameBelongsToUserRL(labelName, userId))
                    throw new KeyNotFoundException("Label not found.");
            }
            else
            {
                throw new ArgumentException("Either LabelId or LabelName must be provided.");
            }

            string query = @"
                SELECT DISTINCT n.""NoteId"", n.""Title"", n.""Description"", n.""Colour"", n.""IsArchived"", n.""IsDeleted"", n.""UserId""
                FROM ""Notes"" n
                INNER JOIN ""NoteLabels"" nl ON n.""NoteId"" = nl.""NoteId""
                INNER JOIN ""Labels"" l ON nl.""LabelId"" = l.""LabelId""
                WHERE n.""UserId"" = @UserId AND n.""IsDeleted"" = FALSE
                  AND (n.""Title"" ILIKE '%' || @Keyword || '%' OR n.""Description"" ILIKE '%' || @Keyword || '%')";

            if (labelId.HasValue)
            {
                query += @" AND nl.""LabelId"" = @LabelId";
            }
            else
            {
                query += @" AND LOWER(l.""Name"") = LOWER(@LabelName)";
            }

            query += @" ORDER BY n.""NoteId"" DESC;";

            var notes = new List<NoteEntity>();

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Keyword", keyword.Trim());
            if (labelId.HasValue)
            {
                command.Parameters.AddWithValue("@LabelId", labelId.Value);
            }
            else
            {
                command.Parameters.AddWithValue("@LabelName", labelName!.Trim());
            }

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notes.Add(MapReaderToNote(reader));
            }

            await reader.CloseAsync();
            await PopulateLabelsForNotesAsync(notes);

            return notes;
        }

        private static NoteEntity MapReaderToNote(NpgsqlDataReader reader)
        {
            return new NoteEntity
            {
                NoteId = reader.GetInt32(reader.GetOrdinal("NoteId")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                Colour = reader.IsDBNull(reader.GetOrdinal("Colour")) ? string.Empty : reader.GetString(reader.GetOrdinal("Colour")),
                IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId"))
            };
        }
    }
}
