    SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF SCHEMA_ID(N'Audit') IS NULL
    EXEC(N'CREATE SCHEMA [Audit] AUTHORIZATION [dbo];');

IF OBJECT_ID(N'[Identity].[Users]', N'U') IS NULL
    THROW 50000, 'The Identity.Users table must exist before AI usage tracking is installed.', 1;

IF OBJECT_ID(N'[Audit].[AiUsageRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [Audit].[AiUsageRecords]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AiUsageRecords] PRIMARY KEY,
        [ExternalId] uniqueidentifier NOT NULL CONSTRAINT [DF_AiUsageRecords_ExternalId] DEFAULT NEWID(),
        [OwnerUserId] bigint NOT NULL,
        [OperationExternalId] uniqueidentifier NULL,
        [OperationType] varchar(50) NOT NULL,
        [Provider] nvarchar(50) NOT NULL,
        [Model] nvarchar(100) NOT NULL,
        [PromptVersion] nvarchar(50) NOT NULL,
        [SchemaVersion] nvarchar(50) NOT NULL,
        [InputTokens] int NOT NULL,
        [CachedInputTokens] int NOT NULL,
        [OutputTokens] int NOT NULL,
        [ReasoningOutputTokens] int NOT NULL,
        [TotalTokens] int NOT NULL,
        [WebSearchCalls] int NOT NULL,
        [RecordedUtc] datetime2(3) NOT NULL CONSTRAINT [DF_AiUsageRecords_RecordedUtc] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [FK_AiUsageRecords_Users] FOREIGN KEY ([OwnerUserId])
            REFERENCES [Identity].[Users]([Id]),
        CONSTRAINT [CK_AiUsageRecords_Nonnegative] CHECK
        (
            [InputTokens] >= 0 AND [CachedInputTokens] >= 0 AND [OutputTokens] >= 0 AND
            [ReasoningOutputTokens] >= 0 AND [TotalTokens] >= 0 AND [WebSearchCalls] >= 0
        )
    );

    CREATE UNIQUE INDEX [UX_AiUsageRecords_ExternalId]
        ON [Audit].[AiUsageRecords]([ExternalId]);
    CREATE INDEX [IX_AiUsageRecords_RecordedUtc_OperationType]
        ON [Audit].[AiUsageRecords]([RecordedUtc], [OperationType]);
    CREATE INDEX [IX_AiUsageRecords_OwnerUserId_RecordedUtc]
        ON [Audit].[AiUsageRecords]([OwnerUserId], [RecordedUtc]);
END;

COMMIT TRANSACTION;
GO
PRINT N'AI usage tracking table created successfully.';
GO
