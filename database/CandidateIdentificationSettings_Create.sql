SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[Trading].[CandidateIdentificationSettings]', N'U') IS NULL
BEGIN
    CREATE TABLE [Trading].[CandidateIdentificationSettings]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CandidateIdentificationSettings] PRIMARY KEY,
        [ExternalId] uniqueidentifier NOT NULL,
        [UserId] bigint NOT NULL,
        [SettingsJson] nvarchar(max) NOT NULL,
        [CreatedUtc] datetime2(3) NOT NULL,
        [ModifiedUtc] datetime2(3) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [FK_CandidateIdentificationSettings_User] FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_CandidateIdentificationSettings_SettingsJson] CHECK (ISJSON([SettingsJson]) = 1)
    );
    CREATE UNIQUE INDEX [UX_CandidateIdentificationSettings_ExternalId] ON [Trading].[CandidateIdentificationSettings]([ExternalId]);
    CREATE UNIQUE INDEX [UX_CandidateIdentificationSettings_UserId] ON [Trading].[CandidateIdentificationSettings]([UserId]);
END;

COMMIT TRANSACTION;
GO
PRINT N'Candidate-identification settings table created successfully.';
GO
