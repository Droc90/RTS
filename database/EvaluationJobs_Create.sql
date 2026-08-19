SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[Trading].[EvaluationJobs]', N'U') IS NULL
BEGIN
    CREATE TABLE [Trading].[EvaluationJobs]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EvaluationJobs] PRIMARY KEY,
        [ExternalId] uniqueidentifier NOT NULL,
        [OwnerUserId] bigint NOT NULL,
        [DiscoveryCandidateId] bigint NOT NULL,
        [Symbol] nvarchar(15) NOT NULL,
        [Status] int NOT NULL,
        [ProgressPercent] int NOT NULL,
        [ProgressMessage] nvarchar(500) NULL,
        [ErrorMessage] nvarchar(4000) NULL,
        [AttemptCount] int NOT NULL,
        [CreatedUtc] datetime2(3) NOT NULL,
        [StartedUtc] datetime2(3) NULL,
        [CompletedUtc] datetime2(3) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [FK_EvaluationJobs_OwnerUser] FOREIGN KEY ([OwnerUserId]) REFERENCES [Identity].[Users]([Id]),
        CONSTRAINT [FK_EvaluationJobs_DiscoveryCandidate] FOREIGN KEY ([DiscoveryCandidateId]) REFERENCES [Trading].[DiscoveryCandidates]([Id]),
        CONSTRAINT [CK_EvaluationJobs_Status] CHECK ([Status] BETWEEN 1 AND 5),
        CONSTRAINT [CK_EvaluationJobs_Progress] CHECK ([ProgressPercent] BETWEEN 0 AND 100),
        CONSTRAINT [CK_EvaluationJobs_Attempts] CHECK ([AttemptCount] >= 0)
    );
    CREATE UNIQUE INDEX [UX_EvaluationJobs_ExternalId] ON [Trading].[EvaluationJobs]([ExternalId]);
    CREATE INDEX [IX_EvaluationJobs_OwnerUserId_Status_CreatedUtc] ON [Trading].[EvaluationJobs]([OwnerUserId], [Status], [CreatedUtc] DESC);
END;

IF OBJECT_ID(N'[Trading].[MarketDataSnapshots]', N'U') IS NULL
BEGIN
    CREATE TABLE [Trading].[MarketDataSnapshots]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_MarketDataSnapshots] PRIMARY KEY,
        [ExternalId] uniqueidentifier NOT NULL,
        [EvaluationJobId] bigint NOT NULL,
        [Symbol] nvarchar(15) NOT NULL,
        [ProviderKey] nvarchar(100) NOT NULL,
        [RetrievedUtc] datetime2(3) NOT NULL,
        [RequestJson] nvarchar(max) NOT NULL,
        [BarsJson] nvarchar(max) NOT NULL,
        [MissingBarsJson] nvarchar(max) NOT NULL,
        CONSTRAINT [FK_MarketDataSnapshots_EvaluationJob] FOREIGN KEY ([EvaluationJobId]) REFERENCES [Trading].[EvaluationJobs]([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_MarketDataSnapshots_RequestJson] CHECK (ISJSON([RequestJson]) = 1),
        CONSTRAINT [CK_MarketDataSnapshots_BarsJson] CHECK (ISJSON([BarsJson]) = 1),
        CONSTRAINT [CK_MarketDataSnapshots_MissingBarsJson] CHECK (ISJSON([MissingBarsJson]) = 1)
    );
    CREATE UNIQUE INDEX [UX_MarketDataSnapshots_ExternalId] ON [Trading].[MarketDataSnapshots]([ExternalId]);
    CREATE INDEX [IX_MarketDataSnapshots_EvaluationJobId_RetrievedUtc] ON [Trading].[MarketDataSnapshots]([EvaluationJobId], [RetrievedUtc] DESC);
END;

COMMIT TRANSACTION;
GO
PRINT N'Evaluation-job and market-data snapshot tables created successfully.';
GO
