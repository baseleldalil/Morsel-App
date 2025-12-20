-- Add CampaignTemplates table to existing database
CREATE TABLE IF NOT EXISTS CampaignTemplates (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    MessageContent TEXT NOT NULL,
    ImageUrl TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    Category TEXT NOT NULL DEFAULT 'General',
    IsGlobal INTEGER NOT NULL DEFAULT 0,
    TimesUsed REAL NOT NULL DEFAULT 0
);

-- Add TemplateAttachments table
CREATE TABLE IF NOT EXISTS TemplateAttachments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CampaignTemplateId INTEGER NOT NULL,
    FileName TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    FileSize INTEGER,
    ContentType TEXT,
    UploadedAt TEXT NOT NULL,
    FOREIGN KEY (CampaignTemplateId) REFERENCES CampaignTemplates(Id) ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS IX_TemplateAttachments_CampaignTemplateId
ON TemplateAttachments(CampaignTemplateId);

CREATE INDEX IF NOT EXISTS IX_CampaignTemplates_Category
ON CampaignTemplates(Category);
