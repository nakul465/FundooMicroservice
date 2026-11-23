-- Fundoo Notes Service Database Initialization
CREATE TABLE IF NOT EXISTS "Notes" (
    "NoteId" SERIAL PRIMARY KEY,
    "Title" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "Colour" VARCHAR(50),
    "IsArchived" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "UserId" INTEGER NOT NULL
);
