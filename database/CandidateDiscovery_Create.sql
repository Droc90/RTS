SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'[Trading].[DiscoveryRuns]', N'U') IS NULL
BEGIN
    CREATE TABLE [Trading].[DiscoveryRuns]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DiscoveryRuns] PRIMARY KEY,
        [ExternalId] uniqueidentifier NOT NULL,
        [OwnerUserId] bigint NOT NULL,
        [ScreeningStrategyVersionId] bigint NOT NULL,
        [UniverseKey] nvarchar(100) NOT NULL,
        [StartedUtc] datetime2(3) NOT NULL,
        [CompletedUtc] datetime2(3) NOT NULL,
        [CriteriaSnapshot] nvarchar(max) NOT NULL,
        CONSTRAINT [FK_DiscoveryRuns_OwnerUser] FOREIGN KEY ([OwnerUserId]) REFERENCES [Identity].[Users]([Id]),
        CONSTRAINT [FK_DiscoveryRuns_StrategyVersion] FOREIGN KEY ([ScreeningStrategyVersionId]) REFERENCES [Trading].[ScreeningStrategyVersions]([Id]),
        CONSTRAINT [CK_DiscoveryRuns_Timestamps] CHECK ([CompletedUtc] >= [StartedUtc])
    );
    CREATE UNIQUE INDEX [UX_DiscoveryRuns_ExternalId] ON [Trading].[DiscoveryRuns]([ExternalId]);
    CREATE INDEX [IX_DiscoveryRuns_OwnerUserId_CompletedUtc] ON [Trading].[DiscoveryRuns]([OwnerUserId], [CompletedUtc] DESC);
END;

IF OBJECT_ID(N'[Trading].[DiscoveryCandidates]', N'U') IS NULL
BEGIN
    CREATE TABLE [Trading].[DiscoveryCandidates]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DiscoveryCandidates] PRIMARY KEY,
        [ExternalId] uniqueidentifier NOT NULL,
        [DiscoveryRunId] bigint NOT NULL,
        [Symbol] nvarchar(15) NOT NULL,
        [AssetType] int NOT NULL,
        [Source] int NOT NULL,
        [Outcome] int NOT NULL,
        [Score] decimal(9,4) NOT NULL,
        [Rank] int NOT NULL,
        [DataTimestampUtc] datetime2(3) NOT NULL,
        [Status] int NOT NULL,
        [IsWatchlisted] bit NOT NULL CONSTRAINT [DF_DiscoveryCandidates_IsWatchlisted] DEFAULT 0,
        [FactorsJson] nvarchar(max) NOT NULL,
        [EvidenceJson] nvarchar(max) NOT NULL,
        [CreatedUtc] datetime2(3) NOT NULL CONSTRAINT [DF_DiscoveryCandidates_CreatedUtc] DEFAULT SYSUTCDATETIME(),
        [ModifiedUtc] datetime2(3) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [FK_DiscoveryCandidates_Run] FOREIGN KEY ([DiscoveryRunId]) REFERENCES [Trading].[DiscoveryRuns]([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_DiscoveryCandidates_AssetType] CHECK ([AssetType] BETWEEN 1 AND 3),
        CONSTRAINT [CK_DiscoveryCandidates_Source] CHECK ([Source] BETWEEN 1 AND 4),
        CONSTRAINT [CK_DiscoveryCandidates_Outcome] CHECK ([Outcome] BETWEEN 1 AND 3),
        CONSTRAINT [CK_DiscoveryCandidates_Status] CHECK ([Status] BETWEEN 1 AND 6),
        CONSTRAINT [CK_DiscoveryCandidates_Rank] CHECK ([Rank] > 0),
        CONSTRAINT [CK_DiscoveryCandidates_FactorsJson] CHECK (ISJSON([FactorsJson]) = 1),
        CONSTRAINT [CK_DiscoveryCandidates_EvidenceJson] CHECK (ISJSON([EvidenceJson]) = 1)
    );
    CREATE UNIQUE INDEX [UX_DiscoveryCandidates_ExternalId] ON [Trading].[DiscoveryCandidates]([ExternalId]);
    CREATE UNIQUE INDEX [UX_DiscoveryCandidates_RunId_Symbol] ON [Trading].[DiscoveryCandidates]([DiscoveryRunId], [Symbol]);
    CREATE INDEX [IX_DiscoveryCandidates_Status_Rank] ON [Trading].[DiscoveryCandidates]([Status], [Rank]);
END;

COMMIT TRANSACTION;
GO

PRINT N'Candidate-discovery tables created successfully.';
GO
