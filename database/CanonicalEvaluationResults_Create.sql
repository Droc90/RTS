SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[Trading].[EvaluationJobs]', N'U') IS NULL
    THROW 50000, 'Run EvaluationJobs_Create.sql before CanonicalEvaluationResults_Create.sql.', 1;

IF OBJECT_ID(N'[Trading].[EvaluationResults]', N'U') IS NULL
BEGIN
    CREATE TABLE [Trading].[EvaluationResults]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EvaluationResults] PRIMARY KEY,
        [ExternalId] uniqueidentifier NOT NULL,
        [EvaluationJobId] bigint NOT NULL,
        [CreatedUtc] datetime2(3) NOT NULL,
        [CalculationVersion] nvarchar(50) NOT NULL,
        [ConfigurationJson] nvarchar(max) NOT NULL,
        [ResultJson] nvarchar(max) NOT NULL,
        [MarketDataSnapshotExternalIdsJson] nvarchar(max) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [FK_EvaluationResults_EvaluationJob] FOREIGN KEY ([EvaluationJobId])
            REFERENCES [Trading].[EvaluationJobs]([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_EvaluationResults_ConfigurationJson] CHECK (ISJSON([ConfigurationJson]) = 1),
        CONSTRAINT [CK_EvaluationResults_ResultJson] CHECK (ISJSON([ResultJson]) = 1),
        CONSTRAINT [CK_EvaluationResults_MarketDataSnapshotExternalIdsJson]
            CHECK (ISJSON([MarketDataSnapshotExternalIdsJson]) = 1)
    );
    CREATE UNIQUE INDEX [UX_EvaluationResults_ExternalId]
        ON [Trading].[EvaluationResults]([ExternalId]);
    CREATE UNIQUE INDEX [UX_EvaluationResults_EvaluationJobId]
        ON [Trading].[EvaluationResults]([EvaluationJobId]);
END;

COMMIT TRANSACTION;
GO
PRINT N'Canonical evaluation-result table created successfully.';
GO
