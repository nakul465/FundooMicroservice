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
                return MapReaderToNote(reader);
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
                return MapReaderToNote(reader);
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
                return MapReaderToNote(reader);
            }

            return null;
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
