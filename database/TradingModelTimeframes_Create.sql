/*
    Adds configurable market-data timeframes to trading-model versions.

    Run against:
    - RTSDB
    - RTSIntegrationTests
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(
        N'[Trading].[TradingModelTimeframes]',
        N'U') IS NULL
    BEGIN
        CREATE TABLE [Trading].[TradingModelTimeframes]
        (
            [Id]                    bigint IDENTITY(1,1) NOT NULL,
            [ExternalId]            uniqueidentifier NOT NULL
                CONSTRAINT [DF_TradingModelTimeframes_ExternalId]
                DEFAULT (NEWID()),
            [TradingModelVersionId] bigint NOT NULL,
            [Name]                  nvarchar(100) NOT NULL,
            [DisplayOrder]          int NOT NULL,
            [LookbackValue]         int NOT NULL,
            [LookbackUnit]          int NOT NULL,
            [BarIntervalValue]      int NOT NULL,
            [BarIntervalUnit]       int NOT NULL,
            [MarketSessionMode]     int NOT NULL,
            [RowVersion]            rowversion NOT NULL,

            CONSTRAINT [PK_TradingModelTimeframes]
                PRIMARY KEY CLUSTERED ([Id]),

            CONSTRAINT [FK_TradingModelTimeframes_TradingModelVersion]
                FOREIGN KEY ([TradingModelVersionId])
                REFERENCES [Trading].[TradingModelVersions]([Id]),

            CONSTRAINT [CK_TradingModelTimeframes_DisplayOrder]
                CHECK ([DisplayOrder] > 0),

            CONSTRAINT [CK_TradingModelTimeframes_LookbackValue]
                CHECK ([LookbackValue] > 0),

            CONSTRAINT [CK_TradingModelTimeframes_LookbackUnit]
                CHECK ([LookbackUnit] IN (1, 2, 3, 4, 5)),

            CONSTRAINT [CK_TradingModelTimeframes_BarIntervalValue]
                CHECK ([BarIntervalValue] > 0),

            CONSTRAINT [CK_TradingModelTimeframes_BarIntervalUnit]
                CHECK ([BarIntervalUnit] IN (1, 2, 3, 4)),

            CONSTRAINT [CK_TradingModelTimeframes_MarketSessionMode]
                CHECK ([MarketSessionMode] IN (1, 2))
        );

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelTimeframes_ExternalId]
            ON [Trading].[TradingModelTimeframes]([ExternalId]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelTimeframes_VersionId_Name]
            ON [Trading].[TradingModelTimeframes](
                [TradingModelVersionId],
                [Name]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelTimeframes_VersionId_DisplayOrder]
            ON [Trading].[TradingModelTimeframes](
                [TradingModelVersionId],
                [DisplayOrder]);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
GO

PRINT N'Trading-model timeframe table created successfully.';
GO