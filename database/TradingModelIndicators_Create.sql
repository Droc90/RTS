/*
    Adds configurable indicators and parameters to trading-model timeframes.

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
        N'[Trading].[TradingModelIndicators]',
        N'U') IS NULL
    BEGIN
        CREATE TABLE [Trading].[TradingModelIndicators]
        (
            [Id]                      bigint IDENTITY(1,1) NOT NULL,
            [ExternalId]              uniqueidentifier NOT NULL
                CONSTRAINT [DF_TradingModelIndicators_ExternalId]
                DEFAULT (NEWID()),
            [TradingModelTimeframeId] bigint NOT NULL,
            [IndicatorType]           int NOT NULL,
            [Name]                    nvarchar(150) NOT NULL,
            [Pane]                    int NOT NULL,
            [DisplayOrder]            int NOT NULL,
            [IsEnabled]               bit NOT NULL
                CONSTRAINT [DF_TradingModelIndicators_IsEnabled]
                DEFAULT (1),
            [RowVersion]              rowversion NOT NULL,

            CONSTRAINT [PK_TradingModelIndicators]
                PRIMARY KEY CLUSTERED ([Id]),

            CONSTRAINT [FK_TradingModelIndicators_TradingModelTimeframe]
                FOREIGN KEY ([TradingModelTimeframeId])
                REFERENCES [Trading].[TradingModelTimeframes]([Id]),

            CONSTRAINT [CK_TradingModelIndicators_IndicatorType]
                CHECK ([IndicatorType] IN (1, 2, 3, 4, 5)),

            CONSTRAINT [CK_TradingModelIndicators_Pane]
                CHECK ([Pane] IN (1, 2, 3)),

            CONSTRAINT [CK_TradingModelIndicators_DisplayOrder]
                CHECK ([DisplayOrder] > 0)
        );

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelIndicators_ExternalId]
            ON [Trading].[TradingModelIndicators]([ExternalId]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelIndicators_TimeframeId_Name]
            ON [Trading].[TradingModelIndicators](
                [TradingModelTimeframeId],
                [Name]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelIndicators_TimeframeId_DisplayOrder]
            ON [Trading].[TradingModelIndicators](
                [TradingModelTimeframeId],
                [DisplayOrder]);

        CREATE NONCLUSTERED INDEX
            [IX_TradingModelIndicators_TimeframeId_IndicatorType]
            ON [Trading].[TradingModelIndicators](
                [TradingModelTimeframeId],
                [IndicatorType]);
    END;

    IF OBJECT_ID(
        N'[Trading].[TradingModelIndicatorParameters]',
        N'U') IS NULL
    BEGIN
        CREATE TABLE [Trading].[TradingModelIndicatorParameters]
        (
            [Id]                      bigint IDENTITY(1,1) NOT NULL,
            [ExternalId]              uniqueidentifier NOT NULL
                CONSTRAINT [DF_TradingModelIndicatorParameters_ExternalId]
                DEFAULT (NEWID()),
            [TradingModelIndicatorId] bigint NOT NULL,
            [ParameterKey]            nvarchar(100) NOT NULL,
            [ValueType]               int NOT NULL,
            [ParameterValue]          nvarchar(500) NOT NULL,
            [RowVersion]              rowversion NOT NULL,

            CONSTRAINT [PK_TradingModelIndicatorParameters]
                PRIMARY KEY CLUSTERED ([Id]),

            CONSTRAINT [FK_TradingModelIndicatorParameters_TradingModelIndicator]
                FOREIGN KEY ([TradingModelIndicatorId])
                REFERENCES [Trading].[TradingModelIndicators]([Id]),

            CONSTRAINT [CK_TradingModelIndicatorParameters_ValueType]
                CHECK ([ValueType] IN (1, 2, 3, 4))
        );

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelIndicatorParameters_ExternalId]
            ON [Trading].[TradingModelIndicatorParameters]([ExternalId]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelIndicatorParameters_IndicatorId_Key]
            ON [Trading].[TradingModelIndicatorParameters](
                [TradingModelIndicatorId],
                [ParameterKey]);
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

PRINT N'Trading-model indicator tables created successfully.';
GO