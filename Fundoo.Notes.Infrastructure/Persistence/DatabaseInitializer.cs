using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Fundoo.Notes.Infrastructure.Persistence
{
    public class DatabaseInitializer
    {
        private readonly string? _connectionString;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration.GetConnectionString("FundooNotesConnection");
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogWarning("Connection string for Notes database is missing. Skipping schema initialization.");
                return;
            }

            const string ddl = @"
                CREATE TABLE IF NOT EXISTS ""Notes"" (
                    ""NoteId"" SERIAL PRIMARY KEY,
                    ""Title"" VARCHAR(255) NOT NULL,
                    ""Description"" TEXT,
                    ""Colour"" VARCHAR(50),
                    ""IsArchived"" BOOLEAN NOT NULL DEFAULT FALSE,
                    ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                    ""UserId"" INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ""Labels"" (
                    ""LabelId"" SERIAL PRIMARY KEY,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""UserId"" INTEGER NOT NULL
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ""ix_labels_user_name_lower"" 
                ON ""Labels"" (""UserId"", LOWER(""Name""));

                CREATE TABLE IF NOT EXISTS ""NoteLabels"" (
                    ""NoteId"" INTEGER NOT NULL REFERENCES ""Notes""(""NoteId"") ON DELETE CASCADE,
                    ""LabelId"" INTEGER NOT NULL REFERENCES ""Labels""(""LabelId"") ON DELETE CASCADE,
                    PRIMARY KEY (""NoteId"", ""LabelId"")
                );

                CREATE INDEX IF NOT EXISTS ""ix_notelabels_noteid"" ON ""NoteLabels"" (""NoteId"");
                CREATE INDEX IF NOT EXISTS ""ix_notelabels_labelid"" ON ""NoteLabels"" (""LabelId"");
            ";

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand(ddl, connection);
                await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Notes database schema initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Notes database schema.");
            }
        }
    }
}
