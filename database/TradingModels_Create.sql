/*
    Creates the initial configurable trading-model persistence structure.

    Run against:
    - RTSDB
    - RTSIntegrationTests
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF SCHEMA_ID(N'Trading') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [Trading] AUTHORIZATION [dbo];');
END;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Trading].[TradingModels]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Trading].[TradingModels]
        (
            [Id]             bigint IDENTITY(1,1) NOT NULL,
            [ExternalId]     uniqueidentifier NOT NULL
                CONSTRAINT [DF_TradingModels_ExternalId]
                DEFAULT (NEWID()),
            [OwnerUserId]    bigint NULL,
            [Name]           nvarchar(200) NOT NULL,
            [Description]    nvarchar(2000) NULL,
            [IsActive]       bit NOT NULL
                CONSTRAINT [DF_TradingModels_IsActive]
                DEFAULT (1),
            [CreatedUtc]     datetime2(3) NOT NULL
                CONSTRAINT [DF_TradingModels_CreatedUtc]
                DEFAULT (SYSUTCDATETIME()),
            [RowVersion]     rowversion NOT NULL,

            CONSTRAINT [PK_TradingModels]
                PRIMARY KEY CLUSTERED ([Id]),

            CONSTRAINT [FK_TradingModels_OwnerUser]
                FOREIGN KEY ([OwnerUserId])
                REFERENCES [Identity].[Users]([Id])
        );

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModels_ExternalId]
            ON [Trading].[TradingModels]([ExternalId]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModels_OwnerUserId_Name]
            ON [Trading].[TradingModels]([OwnerUserId], [Name])
            WHERE [OwnerUserId] IS NOT NULL;

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModels_SystemTemplate_Name]
            ON [Trading].[TradingModels]([Name])
            WHERE [OwnerUserId] IS NULL;

        CREATE NONCLUSTERED INDEX
            [IX_TradingModels_OwnerUserId_IsActive]
            ON [Trading].[TradingModels](
                [OwnerUserId],
                [IsActive]);
    END;

    IF OBJECT_ID(
        N'[Trading].[TradingModelVersions]',
        N'U') IS NULL
    BEGIN
        CREATE TABLE [Trading].[TradingModelVersions]
        (
            [Id]                bigint IDENTITY(1,1) NOT NULL,
            [ExternalId]        uniqueidentifier NOT NULL
                CONSTRAINT [DF_TradingModelVersions_ExternalId]
                DEFAULT (NEWID()),
            [TradingModelId]    bigint NOT NULL,
            [VersionNumber]     int NOT NULL,
            [Status]            int NOT NULL,
            [CreatedUtc]        datetime2(3) NOT NULL
                CONSTRAINT [DF_TradingModelVersions_CreatedUtc]
                DEFAULT (SYSUTCDATETIME()),
            [PublishedUtc]      datetime2(3) NULL,
            [RowVersion]        rowversion NOT NULL,

            CONSTRAINT [PK_TradingModelVersions]
                PRIMARY KEY CLUSTERED ([Id]),

            CONSTRAINT [FK_TradingModelVersions_TradingModel]
                FOREIGN KEY ([TradingModelId])
                REFERENCES [Trading].[TradingModels]([Id]),

            CONSTRAINT [CK_TradingModelVersions_VersionNumber]
                CHECK ([VersionNumber] > 0),

            CONSTRAINT [CK_TradingModelVersions_Status]
                CHECK ([Status] IN (1, 2, 3)),

            CONSTRAINT [CK_TradingModelVersions_PublishedState]
                CHECK
                (
                    ([Status] = 1 AND [PublishedUtc] IS NULL)
                    OR
                    ([Status] IN (2, 3)
                        AND [PublishedUtc] IS NOT NULL)
                ),

            CONSTRAINT [CK_TradingModelVersions_PublishedTime]
                CHECK
                (
                    [PublishedUtc] IS NULL
                    OR [PublishedUtc] >= [CreatedUtc]
                )
        );

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelVersions_ExternalId]
            ON [Trading].[TradingModelVersions]([ExternalId]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelVersions_ModelId_VersionNumber]
            ON [Trading].[TradingModelVersions](
                [TradingModelId],
                [VersionNumber]);

        CREATE UNIQUE NONCLUSTERED INDEX
            [UX_TradingModelVersions_OneDraftPerModel]
            ON [Trading].[TradingModelVersions]([TradingModelId])
            WHERE [Status] = 1;

        CREATE NONCLUSTERED INDEX
            [IX_TradingModelVersions_ModelId_Status]
            ON [Trading].[TradingModelVersions](
                [TradingModelId],
                [Status]);
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

PRINT N'Trading-model tables created successfully.';
GO