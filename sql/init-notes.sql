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

-- Labels table
CREATE TABLE IF NOT EXISTS "Labels" (
    "LabelId" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "UserId" INTEGER NOT NULL
);

-- Case-insensitive uniqueness for label names per user
CREATE UNIQUE INDEX IF NOT EXISTS "ix_labels_user_name_lower" 
ON "Labels" ("UserId", LOWER("Name"));

-- NoteLabels join table with composite primary key and cascade deletes
CREATE TABLE IF NOT EXISTS "NoteLabels" (
    "NoteId" INTEGER NOT NULL REFERENCES "Notes"("NoteId") ON DELETE CASCADE,
    "LabelId" INTEGER NOT NULL REFERENCES "Labels"("LabelId") ON DELETE CASCADE,
    PRIMARY KEY ("NoteId", "LabelId")
);

CREATE INDEX IF NOT EXISTS "ix_notelabels_noteid" ON "NoteLabels" ("NoteId");
CREATE INDEX IF NOT EXISTS "ix_notelabels_labelid" ON "NoteLabels" ("LabelId");
