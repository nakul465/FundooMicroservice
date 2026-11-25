using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Fundoo.Notes.Application.Features.Labels.Services;
using Fundoo.Notes.Domain.Entity;
using Fundoo.Notes.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Fundoo.Notes.Infrastructure.Persistence.Repository
{
    public class LabelRL : ILabelRL
    {
        private readonly string? _connectionString;

        public LabelRL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration.GetConnectionString("FundooNotesConnection");
        }

        private NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public async Task<LabelEntity> CreateLabelRL(LabelEntity label)
        {
            label.Name = label.Name?.Trim() ?? string.Empty;

            // Application-level check for duplicate name (case-insensitive per user)
            if (await ExistsByNameRL(label.Name, label.UserId))
            {
                throw new DuplicateLabelException($"A label with name '{label.Name}' already exists.");
            }

            const string query = @"
                INSERT INTO ""Labels"" (""Name"", ""UserId"")
                VALUES (@Name, @UserId)
                RETURNING ""LabelId"";";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            try
            {
                await using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Name", label.Name);
                command.Parameters.AddWithValue("@UserId", label.UserId);

                var generatedId = await command.ExecuteScalarAsync();
                if (generatedId != null && int.TryParse(generatedId.ToString(), out int id))
                {
                    label.LabelId = id;
                }

                return label;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new DuplicateLabelException($"A label with name '{label.Name}' already exists.");
            }
        }

        public async Task<List<LabelEntity>> GetLabelsByUserRL(int userId)
        {
            const string query = @"
                SELECT ""LabelId"", ""Name"", ""UserId""
                FROM ""Labels""
                WHERE ""UserId"" = @UserId
                ORDER BY ""Name"" ASC;";

            var labels = new List<LabelEntity>();

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                labels.Add(MapReaderToLabel(reader));
            }

            return labels;
        }

        public async Task<LabelEntity?> GetLabelByIdRL(int labelId, int userId)
        {
            const string query = @"
                SELECT ""LabelId"", ""Name"", ""UserId""
                FROM ""Labels""
                WHERE ""LabelId"" = @LabelId AND ""UserId"" = @UserId
                LIMIT 1;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@LabelId", labelId);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapReaderToLabel(reader);
            }

            return null;
        }

        public async Task<LabelEntity?> UpdateLabelRL(LabelEntity label)
        {
            label.Name = label.Name?.Trim() ?? string.Empty;

            // Check if label exists and belongs to user
            var existing = await GetLabelByIdRL(label.LabelId, label.UserId);
            if (existing == null)
            {
                return null;
            }

            // Check for duplicate name excluding current label
            if (await ExistsByNameRL(label.Name, label.UserId, label.LabelId))
            {
                throw new DuplicateLabelException($"A label with name '{label.Name}' already exists.");
            }

            const string query = @"
                UPDATE ""Labels""
                SET ""Name"" = @Name
                WHERE ""LabelId"" = @LabelId AND ""UserId"" = @UserId
                RETURNING ""LabelId"", ""Name"", ""UserId"";";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            try
            {
                await using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Name", label.Name);
                command.Parameters.AddWithValue("@LabelId", label.LabelId);
                command.Parameters.AddWithValue("@UserId", label.UserId);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapReaderToLabel(reader);
                }

                return null;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new DuplicateLabelException($"A label with name '{label.Name}' already exists.");
            }
        }

        public async Task<bool> DeleteLabelRL(int labelId, int userId)
        {
            const string query = @"
                DELETE FROM ""Labels""
                WHERE ""LabelId"" = @LabelId AND ""UserId"" = @UserId;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@LabelId", labelId);
            command.Parameters.AddWithValue("@UserId", userId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> AttachLabelToNoteRL(int noteId, int labelId, int userId)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            // Verify note belongs to user and is not deleted
            const string checkNoteQuery = @"
                SELECT 1 FROM ""Notes"" 
                WHERE ""NoteId"" = @NoteId AND ""UserId"" = @UserId AND ""IsDeleted"" = FALSE LIMIT 1;";
            await using (var checkNoteCmd = new NpgsqlCommand(checkNoteQuery, connection))
            {
                checkNoteCmd.Parameters.AddWithValue("@NoteId", noteId);
                checkNoteCmd.Parameters.AddWithValue("@UserId", userId);
                var noteExists = await checkNoteCmd.ExecuteScalarAsync();
                if (noteExists == null)
                {
                    return false;
                }
            }

            // Verify label belongs to user
            const string checkLabelQuery = @"
                SELECT 1 FROM ""Labels"" 
                WHERE ""LabelId"" = @LabelId AND ""UserId"" = @UserId LIMIT 1;";
            await using (var checkLabelCmd = new NpgsqlCommand(checkLabelQuery, connection))
            {
                checkLabelCmd.Parameters.AddWithValue("@LabelId", labelId);
                checkLabelCmd.Parameters.AddWithValue("@UserId", userId);
                var labelExists = await checkLabelCmd.ExecuteScalarAsync();
                if (labelExists == null)
                {
                    return false;
                }
            }

            // Idempotent insertion
            const string insertQuery = @"
                INSERT INTO ""NoteLabels"" (""NoteId"", ""LabelId"")
                VALUES (@NoteId, @LabelId)
                ON CONFLICT (""NoteId"", ""LabelId"") DO NOTHING;";

            await using (var insertCmd = new NpgsqlCommand(insertQuery, connection))
            {
                insertCmd.Parameters.AddWithValue("@NoteId", noteId);
                insertCmd.Parameters.AddWithValue("@LabelId", labelId);
                await insertCmd.ExecuteNonQueryAsync();
            }

            return true;
        }

        public async Task<bool> DetachLabelFromNoteRL(int noteId, int labelId, int userId)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            // Verify note belongs to user and is not deleted
            const string checkNoteQuery = @"
                SELECT 1 FROM ""Notes"" 
                WHERE ""NoteId"" = @NoteId AND ""UserId"" = @UserId AND ""IsDeleted"" = FALSE LIMIT 1;";
            await using (var checkNoteCmd = new NpgsqlCommand(checkNoteQuery, connection))
            {
                checkNoteCmd.Parameters.AddWithValue("@NoteId", noteId);
                checkNoteCmd.Parameters.AddWithValue("@UserId", userId);
                var noteExists = await checkNoteCmd.ExecuteScalarAsync();
                if (noteExists == null)
                {
                    return false;
                }
            }

            // Verify label belongs to user
            const string checkLabelQuery = @"
                SELECT 1 FROM ""Labels"" 
                WHERE ""LabelId"" = @LabelId AND ""UserId"" = @UserId LIMIT 1;";
            await using (var checkLabelCmd = new NpgsqlCommand(checkLabelQuery, connection))
            {
                checkLabelCmd.Parameters.AddWithValue("@LabelId", labelId);
                checkLabelCmd.Parameters.AddWithValue("@UserId", userId);
                var labelExists = await checkLabelCmd.ExecuteScalarAsync();
                if (labelExists == null)
                {
                    return false;
                }
            }

            const string deleteQuery = @"
                DELETE FROM ""NoteLabels""
                WHERE ""NoteId"" = @NoteId AND ""LabelId"" = @LabelId;";

            await using (var deleteCmd = new NpgsqlCommand(deleteQuery, connection))
            {
                deleteCmd.Parameters.AddWithValue("@NoteId", noteId);
                deleteCmd.Parameters.AddWithValue("@LabelId", labelId);
                await deleteCmd.ExecuteNonQueryAsync();
            }

            return true;
        }

        public async Task<Dictionary<int, List<LabelEntity>>> GetLabelsByNoteIdsRL(IEnumerable<int> noteIds)
        {
            var result = new Dictionary<int, List<LabelEntity>>();
            var noteIdList = noteIds.Distinct().ToList();
            if (!noteIdList.Any())
            {
                return result;
            }

            foreach (var id in noteIdList)
            {
                result[id] = new List<LabelEntity>();
            }

            const string query = @"
                SELECT nl.""NoteId"", l.""LabelId"", l.""Name"", l.""UserId""
                FROM ""NoteLabels"" nl
                INNER JOIN ""Labels"" l ON nl.""LabelId"" = l.""LabelId""
                WHERE nl.""NoteId"" = ANY(@NoteIds)
                ORDER BY l.""Name"" ASC;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@NoteIds", noteIdList.ToArray());

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var noteId = Convert.ToInt32(reader["NoteId"]);
                var label = MapReaderToLabel(reader);
                if (result.TryGetValue(noteId, out var list))
                {
                    list.Add(label);
                }
            }

            return result;
        }

        private async Task<bool> ExistsByNameRL(string name, int userId, int? excludeLabelId = null)
        {
            string query = @"
                SELECT 1 FROM ""Labels""
                WHERE ""UserId"" = @UserId AND LOWER(""Name"") = LOWER(@Name)";

            if (excludeLabelId.HasValue)
            {
                query += @" AND ""LabelId"" != @ExcludeLabelId";
            }
            query += " LIMIT 1;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Name", name);
            if (excludeLabelId.HasValue)
            {
                command.Parameters.AddWithValue("@ExcludeLabelId", excludeLabelId.Value);
            }

            var exists = await command.ExecuteScalarAsync();
            return exists != null;
        }

        private static LabelEntity MapReaderToLabel(IDataRecord reader)
        {
            return new LabelEntity
            {
                LabelId = Convert.ToInt32(reader["LabelId"]),
                Name = reader["Name"]?.ToString() ?? string.Empty,
                UserId = Convert.ToInt32(reader["UserId"])
            };
        }
    }
}
