/*
    Adds configurable evaluation criteria to trading-model versions.

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
        N'[Trading].[TradingModelCriteria]',
        N'U') IS NULL
    BEGIN
        CREATE TABLE [Trading].[TradingModelCriteria]
        (
            [Id]                    bigint IDENTITY(1,1) NOT NULL,
            [ExternalId]            uniqueidentifier NOT NULL
                CONSTRAINT [DF_TradingModelCriteria_ExternalId]
                DEFAULT (NEWID()),
            [TradingModelVersionId] bigint NOT NULL,
            [Name]                  nvarchar(150) NOT NULL,
            [MetricKey]             nvarchar(150) NOT NULL,
            [Purpose]               int NOT NULL,
            [ComparisonOperator]    int NOT NULL,
            [PrimaryValueType]      int NOT NULL,
            [PrimaryValue]          nvarchar(500) NOT NULL,
            [SecondaryValueType]    int NULL,
            [SecondaryValue]        nvarchar(500) NULL,
            [Weight]                decimal(9,4) NULL,
            [DisplayOrder]          int NOT NULL,
            [IsEnabled]             bit NOT NULL
                CONSTRAINT [DF_TradingModelCriteria_IsEnabled]
                DEFAULT (1),
            [RowVersion]            rowversion NOT NULL,

            CONSTRAINT [PK_TradingModelCriteria]
                PRIMARY KEY CLUSTERED ([Id]),

            CONSTRAINT [FK_TradingModelCriteria_TradingModelVersion]
                FOREIGN KEY ([TradingModelVersionId])
                REFERENCES [Trading].[TradingModelVersions]([Id]),

            CONSTRAINT [CK_TradingModelCriteria_Purpose]
                CHECK ([Purpose] IN (1, 2, 3)),

            CONSTRAINT [CK_TradingModelCriteria_Operator]
                CHECK ([ComparisonOperator] BETWEEN 1 AND 12),

            CONSTRAINT [CK_TradingModelCriteria_PrimaryValueType]
                CHECK ([PrimaryValueType] IN (1, 2, 3, 4, 5)),

            CONSTRAINT [CK_TradingModelCriteria_SecondaryValueType]
                CHECK
                (
                    [SecondaryValueType] IS NULL
                    OR [SecondaryValueType] IN (1, 2, 3, 4, 5)
                ),

            CONSTRAINT [CK_TradingModelCriteria_SecondaryValueState]
                CHECK
                (
                    ([SecondaryValueType] IS NULL
                        AND [SecondaryValue] IS NULL)
                    OR
                    ([SecondaryValueType] IS NOT NULL
                        AND [SecondaryValue] IS NOT NULL)
                ),

            CONSTRAINT [CK_TradingModelCriteria_RangeState]
                CHECK
                (
                    ([ComparisonOperator] IN (7, 8)
                        AND [SecondaryValue] IS NOT NULL)
                    OR
                    ([ComparisonOperator] NOT IN (7, 8)
                        AND [SecondaryValue] IS NULL)
                ),

            CONSTRAINT [CK_TradingModelCriteria_ValueTypesMatch]
                CHECK
                (
                    [SecondaryValueType] IS NULL
                    OR [SecondaryValueType] = [PrimaryValueType]
                ),

            CONSTRAINT [CK_TradingModelCriteria_Weight]
                CHECK
                (
                    ([Purpose] = 2
                        AND [Weight] > 0
                        AND [Weight] <= 100)
                    OR
                    ([Purpose] <> 2
                        AND [Weight] IS NULL)
                ),

            CONSTRAINT [CK_TradingModelCriteria_DisplayOrder]
                CHECK ([DisplayOrder] > 0)
        );

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelCriteria_ExternalId]
            ON [Trading].[TradingModelCriteria]([ExternalId]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelCriteria_VersionId_Name]
            ON [Trading].[TradingModelCriteria](
                [TradingModelVersionId],
                [Name]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelCriteria_VersionId_DisplayOrder]
            ON [Trading].[TradingModelCriteria](
                [TradingModelVersionId],
                [DisplayOrder]);

        CREATE NONCLUSTERED INDEX
            [IX_TradingModelCriteria_VersionId_Purpose]
            ON [Trading].[TradingModelCriteria](
                [TradingModelVersionId],
                [Purpose]);
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

PRINT N'Trading-model criteria table created successfully.';
GO