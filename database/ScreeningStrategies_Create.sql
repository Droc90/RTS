/*
    Adds versioned configurable screening strategies.

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
        N'[Trading].[ScreeningStrategies]',
        N'U') IS NULL
    BEGIN
        CREATE TABLE [Trading].[ScreeningStrategies]
        (
            [Id]          bigint IDENTITY(1,1) NOT NULL,
            [ExternalId]  uniqueidentifier NOT NULL
                CONSTRAINT [DF_ScreeningStrategies_ExternalId]
                DEFAULT (NEWID()),
            [OwnerUserId] bigint NULL,
            [Name]        nvarchar(200) NOT NULL,
            [Description] nvarchar(2000) NULL,
            [IsActive]    bit NOT NULL
                CONSTRAINT [DF_ScreeningStrategies_IsActive]
                DEFAULT (1),
            [CreatedUtc]  datetime2(3) NOT NULL
                CONSTRAINT [DF_ScreeningStrategies_CreatedUtc]
                DEFAULT (SYSUTCDATETIME()),
            [RowVersion]  rowversion NOT NULL,

            CONSTRAINT [PK_ScreeningStrategies]
                PRIMARY KEY CLUSTERED ([Id]),

            CONSTRAINT [FK_ScreeningStrategies_OwnerUser]
                FOREIGN KEY ([OwnerUserId])
                REFERENCES [Identity].[Users]([Id])
        );

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_ScreeningStrategies_ExternalId]
            ON [Trading].[ScreeningStrategies]([ExternalId]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_ScreeningStrategies_OwnerUserId_Name]
            ON [Trading].[ScreeningStrategies](
                [OwnerUserId],
                [Name])
            WHERE [OwnerUserId] IS NOT NULL;

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_ScreeningStrategies_SystemTemplate_Name]
            ON [Trading].[ScreeningStrategies]([Name])
            WHERE [OwnerUserId] IS NULL;

        CREATE NONCLUSTERED INDEX
            [IX_ScreeningStrategies_OwnerUserId_IsActive]
            ON [Trading].[ScreeningStrategies](
                [OwnerUserId],
                [IsActive]);
    END;

    IF OBJECT_ID(
        N'[Trading].[ScreeningStrategyVersions]',
        N'U') IS NULL
    BEGIN
        CREATE TABLE [Trading].[ScreeningStrategyVersions]
        (
            [Id]                  bigint IDENTITY(1,1) NOT NULL,
            [ExternalId]          uniqueidentifier NOT NULL
                CONSTRAINT [DF_ScreeningStrategyVersions_ExternalId]
                DEFAULT (NEWID()),
            [ScreeningStrategyId] bigint NOT NULL,
            [VersionNumber]       int NOT NULL,
            [Status]              int NOT NULL,
            [CreatedUtc]          datetime2(3) NOT NULL
                CONSTRAINT [DF_ScreeningStrategyVersions_CreatedUtc]
                DEFAULT (SYSUTCDATETIME()),
            [PublishedUtc]        datetime2(3) NULL,
            [RowVersion]          rowversion NOT NULL,

            CONSTRAINT [PK_ScreeningStrategyVersions]
                PRIMARY KEY CLUSTERED ([Id]),

            CONSTRAINT [FK_ScreeningStrategyVersions_ScreeningStrategy]
                FOREIGN KEY ([ScreeningStrategyId])
                REFERENCES [Trading].[ScreeningStrategies]([Id]),

            CONSTRAINT [CK_ScreeningStrategyVersions_VersionNumber]
                CHECK ([VersionNumber] > 0),

            CONSTRAINT [CK_ScreeningStrategyVersions_Status]
                CHECK ([Status] IN (1, 2, 3)),

            CONSTRAINT [CK_ScreeningStrategyVersions_PublishedState]
                CHECK
                (
                    ([Status] = 1 AND [PublishedUtc] IS NULL)
                    OR
                    ([Status] IN (2, 3)
                        AND [PublishedUtc] IS NOT NULL)
                ),

            CONSTRAINT [CK_ScreeningStrategyVersions_PublishedTime]
                CHECK
                (
                    [PublishedUtc] IS NULL
                    OR [PublishedUtc] >= [CreatedUtc]
                )
        );

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_ScreeningStrategyVersions_ExternalId]
            ON [Trading].[ScreeningStrategyVersions]([ExternalId]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_ScreeningStrategyVersions_StrategyId_VersionNumber]
            ON [Trading].[ScreeningStrategyVersions](
                [ScreeningStrategyId],
                [VersionNumber]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_ScreeningStrategyVersions_OneDraftPerStrategy]
            ON [Trading].[ScreeningStrategyVersions](
                [ScreeningStrategyId])
            WHERE [Status] = 1;

        CREATE NONCLUSTERED INDEX
            [IX_ScreeningStrategyVersions_StrategyId_Status]
            ON [Trading].[ScreeningStrategyVersions](
                [ScreeningStrategyId],
                [Status]);
    END;

    IF OBJECT_ID(
        N'[Trading].[ScreeningRules]',
        N'U') IS NULL
    BEGIN
        CREATE TABLE [Trading].[ScreeningRules]
        (
            [Id]                         bigint IDENTITY(1,1) NOT NULL,
            [ExternalId]                 uniqueidentifier NOT NULL
                CONSTRAINT [DF_ScreeningRules_ExternalId]
                DEFAULT (NEWID()),
            [ScreeningStrategyVersionId] bigint NOT NULL,
            [Name]                       nvarchar(150) NOT NULL,
            [MetricKey]                  nvarchar(150) NOT NULL,
            [Purpose]                    int NOT NULL,
            [ComparisonOperator]         int NOT NULL,
            [PrimaryValueType]           int NOT NULL,
            [PrimaryValue]               nvarchar(500) NOT NULL,
            [SecondaryValueType]         int NULL,
            [SecondaryValue]             nvarchar(500) NULL,
            [Weight]                     decimal(9,4) NULL,
            [DisplayOrder]               int NOT NULL,
            [IsEnabled]                  bit NOT NULL
                CONSTRAINT [DF_ScreeningRules_IsEnabled]
                DEFAULT (1),
            [RowVersion]                 rowversion NOT NULL,

            CONSTRAINT [PK_ScreeningRules]
                PRIMARY KEY CLUSTERED ([Id]),

            CONSTRAINT [FK_ScreeningRules_ScreeningStrategyVersion]
                FOREIGN KEY ([ScreeningStrategyVersionId])
                REFERENCES [Trading].[ScreeningStrategyVersions]([Id]),

            CONSTRAINT [CK_ScreeningRules_Purpose]
                CHECK ([Purpose] IN (1, 2, 3)),

            CONSTRAINT [CK_ScreeningRules_Operator]
                CHECK ([ComparisonOperator] BETWEEN 1 AND 12),

            CONSTRAINT [CK_ScreeningRules_PrimaryValueType]
                CHECK ([PrimaryValueType] IN (1, 2, 3, 4, 5)),

            CONSTRAINT [CK_ScreeningRules_SecondaryValueType]
                CHECK
                (
                    [SecondaryValueType] IS NULL
                    OR [SecondaryValueType] IN (1, 2, 3, 4, 5)
                ),

            CONSTRAINT [CK_ScreeningRules_SecondaryValueState]
                CHECK
                (
                    ([SecondaryValueType] IS NULL
                        AND [SecondaryValue] IS NULL)
                    OR
                    ([SecondaryValueType] IS NOT NULL
                        AND [SecondaryValue] IS NOT NULL)
                ),

            CONSTRAINT [CK_ScreeningRules_RangeState]
                CHECK
                (
                    ([ComparisonOperator] IN (7, 8)
                        AND [SecondaryValue] IS NOT NULL)
                    OR
                    ([ComparisonOperator] NOT IN (7, 8)
                        AND [SecondaryValue] IS NULL)
                ),

            CONSTRAINT [CK_ScreeningRules_ValueTypesMatch]
                CHECK
                (
                    [SecondaryValueType] IS NULL
                    OR [SecondaryValueType] = [PrimaryValueType]
                ),

            CONSTRAINT [CK_ScreeningRules_Weight]
                CHECK
                (
                    ([Purpose] = 2
                        AND [Weight] > 0
                        AND [Weight] <= 100)
                    OR
                    ([Purpose] <> 2
                        AND [Weight] IS NULL)
                ),

            CONSTRAINT [CK_ScreeningRules_DisplayOrder]
                CHECK ([DisplayOrder] > 0)
        );

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_ScreeningRules_ExternalId]
            ON [Trading].[ScreeningRules]([ExternalId]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_ScreeningRules_VersionId_Name]
            ON [Trading].[ScreeningRules](
                [ScreeningStrategyVersionId],
                [Name]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_ScreeningRules_VersionId_DisplayOrder]
            ON [Trading].[ScreeningRules](
                [ScreeningStrategyVersionId],
                [DisplayOrder]);

        CREATE NONCLUSTERED INDEX
            [IX_ScreeningRules_VersionId_Purpose]
            ON [Trading].[ScreeningRules](
                [ScreeningStrategyVersionId],
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

PRINT N'Screening-strategy tables created successfully.';
GO