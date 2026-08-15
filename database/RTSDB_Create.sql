/*
    RTSDB - Ranked Trading System Database
    Target: SQL Server 2025 Developer Edition (compatibility level 170) / Azure SQL Database
    Purpose: Database foundation for the Ranked Trading System application

    Notes:
    - Internal keys use bigint IDENTITY.
    - External/public identifiers use uniqueidentifier with NEWID().
    - All application timestamps are stored in UTC.
    - ASP.NET Core Identity tables use clean schema-qualified names in the [Identity] schema.
    - No seed data is included.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* Create the database when running against a local SQL Server instance.
   For Azure SQL Database, create the database through Azure first, then run
   the remainder of this script while connected to that database. */
IF DB_ID(N'RTSDB') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [RTSDB]');
END;
GO

USE [RTSDB];
GO

ALTER DATABASE [RTSDB]
SET COMPATIBILITY_LEVEL = 170;
GO

/* Schemas */
IF SCHEMA_ID(N'Identity') IS NULL EXEC(N'CREATE SCHEMA [Identity] AUTHORIZATION [dbo];');
IF SCHEMA_ID(N'Profile') IS NULL EXEC(N'CREATE SCHEMA [Profile] AUTHORIZATION [dbo];');
IF SCHEMA_ID(N'Administration') IS NULL EXEC(N'CREATE SCHEMA [Administration] AUTHORIZATION [dbo];');
IF SCHEMA_ID(N'Audit') IS NULL EXEC(N'CREATE SCHEMA [Audit] AUTHORIZATION [dbo];');
IF SCHEMA_ID(N'Communication') IS NULL EXEC(N'CREATE SCHEMA [Communication] AUTHORIZATION [dbo];');
IF SCHEMA_ID(N'Storage') IS NULL EXEC(N'CREATE SCHEMA [Storage] AUTHORIZATION [dbo];');
GO

BEGIN TRANSACTION;

/* =========================================================
   Identity
   ========================================================= */

CREATE TABLE [Identity].[Users]
(
    [Id]                    bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]            uniqueidentifier NOT NULL CONSTRAINT [DF_Users_ExternalId] DEFAULT (NEWID()),
    [UserName]              nvarchar(256) NULL,
    [NormalizedUserName]    nvarchar(256) NULL,
    [Email]                 nvarchar(256) NULL,
    [NormalizedEmail]       nvarchar(256) NULL,
    [EmailConfirmed]        bit NOT NULL CONSTRAINT [DF_Users_EmailConfirmed] DEFAULT (0),
    [PasswordHash]          nvarchar(max) NULL,
    [SecurityStamp]         nvarchar(max) NULL,
    [ConcurrencyStamp]      nvarchar(max) NULL,
    [PhoneNumber]           nvarchar(max) NULL,
    [PhoneNumberConfirmed]  bit NOT NULL CONSTRAINT [DF_Users_PhoneNumberConfirmed] DEFAULT (0),
    [TwoFactorEnabled]      bit NOT NULL CONSTRAINT [DF_Users_TwoFactorEnabled] DEFAULT (0),
    [LockoutEnd]            datetimeoffset(7) NULL,
    [LockoutEnabled]        bit NOT NULL CONSTRAINT [DF_Users_LockoutEnabled] DEFAULT (1),
    [AccessFailedCount]     int NOT NULL CONSTRAINT [DF_Users_AccessFailedCount] DEFAULT (0),
    [IsActive]              bit NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
    [CreatedUtc]            datetime2(3) NOT NULL CONSTRAINT [DF_Users_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedByUserId]       bigint NULL,
    [ModifiedUtc]           datetime2(3) NULL,
    [ModifiedByUserId]      bigint NULL,
    [LastLoginUtc]          datetime2(3) NULL,
    [IsDeleted]             bit NOT NULL CONSTRAINT [DF_Users_IsDeleted] DEFAULT (0),
    [DeletedUtc]            datetime2(3) NULL,
    [DeletedByUserId]       bigint NULL,
    [RowVersion]            rowversion NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_Users_AccessFailedCount] CHECK ([AccessFailedCount] >= 0),
    CONSTRAINT [CK_Users_DeleteState] CHECK
    (
        ([IsDeleted] = 0 AND [DeletedUtc] IS NULL AND [DeletedByUserId] IS NULL)
        OR
        ([IsDeleted] = 1 AND [DeletedUtc] IS NOT NULL)
    )
);

CREATE TABLE [Identity].[Roles]
(
    [Id]                  bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]          uniqueidentifier NOT NULL CONSTRAINT [DF_Roles_ExternalId] DEFAULT (NEWID()),
    [Name]                nvarchar(256) NULL,
    [NormalizedName]      nvarchar(256) NULL,
    [ConcurrencyStamp]    nvarchar(max) NULL,
    [Description]         nvarchar(500) NULL,
    [IsActive]            bit NOT NULL CONSTRAINT [DF_Roles_IsActive] DEFAULT (1),
    [CreatedUtc]          datetime2(3) NOT NULL CONSTRAINT [DF_Roles_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedByUserId]     bigint NULL,
    [ModifiedUtc]         datetime2(3) NULL,
    [ModifiedByUserId]    bigint NULL,
    [RowVersion]          rowversion NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [Identity].[UserRoles]
(
    [UserId] bigint NOT NULL,
    [RoleId] bigint NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserId], [RoleId])
);

CREATE TABLE [Identity].[UserClaims]
(
    [Id]         int IDENTITY(1,1) NOT NULL,
    [UserId]     bigint NOT NULL,
    [ClaimType]  nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_UserClaims] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [Identity].[RoleClaims]
(
    [Id]         int IDENTITY(1,1) NOT NULL,
    [RoleId]     bigint NOT NULL,
    [ClaimType]  nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_RoleClaims] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [Identity].[UserLogins]
(
    [LoginProvider]       nvarchar(128) NOT NULL,
    [ProviderKey]         nvarchar(128) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId]              bigint NOT NULL,
    CONSTRAINT [PK_UserLogins] PRIMARY KEY CLUSTERED ([LoginProvider], [ProviderKey])
);

CREATE TABLE [Identity].[UserTokens]
(
    [UserId]        bigint NOT NULL,
    [LoginProvider] nvarchar(128) NOT NULL,
    [Name]          nvarchar(128) NOT NULL,
    [Value]         nvarchar(max) NULL,
    CONSTRAINT [PK_UserTokens] PRIMARY KEY CLUSTERED ([UserId], [LoginProvider], [Name])
);

/* =========================================================
   Profile
   ========================================================= */

CREATE TABLE [Profile].[UserProfiles]
(
    [Id]                      bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]              uniqueidentifier NOT NULL CONSTRAINT [DF_UserProfiles_ExternalId] DEFAULT (NEWID()),
    [UserId]                  bigint NOT NULL,
    [DisplayName]             nvarchar(150) NULL,
    [FirstName]               nvarchar(100) NULL,
    [LastName]                nvarchar(100) NULL,
    [TimeZoneId]              nvarchar(100) NULL,
    [Locale]                  nvarchar(20) NULL,
    [PreferredDateFormat]     nvarchar(50) NULL,
    [IsOnboardingComplete]    bit NOT NULL CONSTRAINT [DF_UserProfiles_IsOnboardingComplete] DEFAULT (0),
    [CreatedUtc]              datetime2(3) NOT NULL CONSTRAINT [DF_UserProfiles_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedByUserId]         bigint NULL,
    [ModifiedUtc]             datetime2(3) NULL,
    [ModifiedByUserId]        bigint NULL,
    [IsDeleted]               bit NOT NULL CONSTRAINT [DF_UserProfiles_IsDeleted] DEFAULT (0),
    [DeletedUtc]              datetime2(3) NULL,
    [DeletedByUserId]         bigint NULL,
    [RowVersion]              rowversion NOT NULL,
    CONSTRAINT [PK_UserProfiles] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_UserProfiles_DeleteState] CHECK
    (
        ([IsDeleted] = 0 AND [DeletedUtc] IS NULL AND [DeletedByUserId] IS NULL)
        OR
        ([IsDeleted] = 1 AND [DeletedUtc] IS NOT NULL)
    )
);

CREATE TABLE [Profile].[UserPreferences]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [UserId]             bigint NOT NULL,
    [PreferenceKey]      nvarchar(150) NOT NULL,
    [PreferenceValue]    nvarchar(max) NULL,
    [ValueType]          varchar(30) NOT NULL,
    [CreatedUtc]         datetime2(3) NOT NULL CONSTRAINT [DF_UserPreferences_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedByUserId]    bigint NULL,
    [ModifiedUtc]        datetime2(3) NULL,
    [ModifiedByUserId]   bigint NULL,
    [RowVersion]         rowversion NOT NULL,
    CONSTRAINT [PK_UserPreferences] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_UserPreferences_ValueType] CHECK ([ValueType] IN ('String','Boolean','Integer','Decimal','DateTime','Json'))
);

CREATE TABLE [Profile].[UserAgreements]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]         uniqueidentifier NOT NULL CONSTRAINT [DF_UserAgreements_ExternalId] DEFAULT (NEWID()),
    [UserId]             bigint NOT NULL,
    [AgreementType]      nvarchar(100) NOT NULL,
    [AgreementVersion]   nvarchar(50) NOT NULL,
    [ActionType]         varchar(20) NOT NULL,
    [OccurredUtc]        datetime2(3) NOT NULL CONSTRAINT [DF_UserAgreements_OccurredUtc] DEFAULT (SYSUTCDATETIME()),
    [IpAddress]          varchar(45) NULL,
    [UserAgent]          nvarchar(500) NULL,
    [CorrelationId]      uniqueidentifier NULL,
    [DocumentHash]       nvarchar(128) NULL,
    CONSTRAINT [PK_UserAgreements] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_UserAgreements_ActionType] CHECK ([ActionType] IN ('Accepted','Withdrawn'))
);

/* =========================================================
   Administration
   ========================================================= */

CREATE TABLE [Administration].[SettingDefinitions]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]         uniqueidentifier NOT NULL CONSTRAINT [DF_SettingDefinitions_ExternalId] DEFAULT (NEWID()),
    [SettingKey]         nvarchar(150) NOT NULL,
    [DisplayName]        nvarchar(200) NULL,
    [Description]        nvarchar(1000) NULL,
    [Category]           nvarchar(100) NULL,
    [ValueType]          varchar(30) NOT NULL,
    [DefaultValue]       nvarchar(max) NULL,
    [IsRequired]         bit NOT NULL CONSTRAINT [DF_SettingDefinitions_IsRequired] DEFAULT (0),
    [IsSystemDefined]    bit NOT NULL CONSTRAINT [DF_SettingDefinitions_IsSystemDefined] DEFAULT (0),
    [IsEditable]         bit NOT NULL CONSTRAINT [DF_SettingDefinitions_IsEditable] DEFAULT (1),
    [IsActive]           bit NOT NULL CONSTRAINT [DF_SettingDefinitions_IsActive] DEFAULT (1),
    [ValidationPattern]  nvarchar(1000) NULL,
    [MinimumValue]       decimal(38,10) NULL,
    [MaximumValue]       decimal(38,10) NULL,
    [CreatedUtc]         datetime2(3) NOT NULL CONSTRAINT [DF_SettingDefinitions_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedByUserId]    bigint NULL,
    [ModifiedUtc]        datetime2(3) NULL,
    [ModifiedByUserId]   bigint NULL,
    [RowVersion]         rowversion NOT NULL,
    CONSTRAINT [PK_SettingDefinitions] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_SettingDefinitions_ValueType] CHECK ([ValueType] IN ('String','Boolean','Integer','Decimal','DateTime','Json')),
    CONSTRAINT [CK_SettingDefinitions_ValueRange] CHECK ([MinimumValue] IS NULL OR [MaximumValue] IS NULL OR [MaximumValue] >= [MinimumValue])
);

CREATE TABLE [Administration].[SettingValues]
(
    [Id]                   bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]           uniqueidentifier NOT NULL CONSTRAINT [DF_SettingValues_ExternalId] DEFAULT (NEWID()),
    [SettingDefinitionId]  bigint NOT NULL,
    [ScopeType]             varchar(30) NOT NULL CONSTRAINT [DF_SettingValues_ScopeType] DEFAULT ('Application'),
    [ScopeExternalId]       uniqueidentifier NULL,
    [EnvironmentName]       nvarchar(100) NULL,
    [SettingValue]          nvarchar(max) NULL,
    [IsActive]              bit NOT NULL CONSTRAINT [DF_SettingValues_IsActive] DEFAULT (1),
    [CreatedUtc]            datetime2(3) NOT NULL CONSTRAINT [DF_SettingValues_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedByUserId]       bigint NULL,
    [ModifiedUtc]           datetime2(3) NULL,
    [ModifiedByUserId]      bigint NULL,
    [RowVersion]            rowversion NOT NULL,
    CONSTRAINT [PK_SettingValues] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_SettingValues_ScopeType] CHECK ([ScopeType] IN ('Application','Environment','Organization','User')),
    CONSTRAINT [CK_SettingValues_Scope] CHECK
    (
        ([ScopeType] = 'Application' AND [ScopeExternalId] IS NULL AND [EnvironmentName] IS NULL)
        OR ([ScopeType] = 'Environment' AND [ScopeExternalId] IS NULL AND [EnvironmentName] IS NOT NULL)
        OR ([ScopeType] IN ('Organization','User') AND [ScopeExternalId] IS NOT NULL)
    )
);

CREATE TABLE [Administration].[FeatureFlags]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]         uniqueidentifier NOT NULL CONSTRAINT [DF_FeatureFlags_ExternalId] DEFAULT (NEWID()),
    [FeatureKey]         nvarchar(150) NOT NULL,
    [DisplayName]        nvarchar(200) NULL,
    [Description]        nvarchar(1000) NULL,
    [IsEnabled]          bit NOT NULL CONSTRAINT [DF_FeatureFlags_IsEnabled] DEFAULT (0),
    [StartUtc]           datetime2(3) NULL,
    [EndUtc]             datetime2(3) NULL,
    [IsActive]           bit NOT NULL CONSTRAINT [DF_FeatureFlags_IsActive] DEFAULT (1),
    [CreatedUtc]         datetime2(3) NOT NULL CONSTRAINT [DF_FeatureFlags_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedByUserId]    bigint NULL,
    [ModifiedUtc]        datetime2(3) NULL,
    [ModifiedByUserId]   bigint NULL,
    [RowVersion]         rowversion NOT NULL,
    CONSTRAINT [PK_FeatureFlags] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_FeatureFlags_DateRange] CHECK ([StartUtc] IS NULL OR [EndUtc] IS NULL OR [EndUtc] > [StartUtc])
);

/* =========================================================
   Audit
   ========================================================= */

CREATE TABLE [Audit].[AuditLogs]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]         uniqueidentifier NOT NULL CONSTRAINT [DF_AuditLogs_ExternalId] DEFAULT (NEWID()),
    [UserId]             bigint NULL,
    [EventCategory]      nvarchar(100) NULL,
    [Action]             nvarchar(150) NOT NULL,
    [EntityType]         nvarchar(150) NULL,
    [EntityId]           bigint NULL,
    [EntityExternalId]   uniqueidentifier NULL,
    [OldValues]          nvarchar(max) NULL,
    [NewValues]          nvarchar(max) NULL,
    [IpAddress]          varchar(45) NULL,
    [UserAgent]          nvarchar(500) NULL,
    [CorrelationId]      uniqueidentifier NULL,
    [IsSuccess]          bit NOT NULL CONSTRAINT [DF_AuditLogs_IsSuccess] DEFAULT (1),
    [FailureReason]      nvarchar(1000) NULL,
    [CreatedUtc]         datetime2(3) NOT NULL CONSTRAINT [DF_AuditLogs_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_AuditLogs_OldValues_Json] CHECK ([OldValues] IS NULL OR ISJSON([OldValues]) = 1),
    CONSTRAINT [CK_AuditLogs_NewValues_Json] CHECK ([NewValues] IS NULL OR ISJSON([NewValues]) = 1)
);

CREATE TABLE [Audit].[LoginHistory]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]         uniqueidentifier NOT NULL CONSTRAINT [DF_LoginHistory_ExternalId] DEFAULT (NEWID()),
    [UserId]             bigint NULL,
    [IdentifierHash]     varbinary(32) NULL,
    [EventType]          varchar(50) NOT NULL,
    [IsSuccess]          bit NOT NULL,
    [FailureCode]        varchar(100) NULL,
    [IpAddress]          varchar(45) NULL,
    [UserAgent]          nvarchar(500) NULL,
    [CorrelationId]      uniqueidentifier NULL,
    [CreatedUtc]         datetime2(3) NOT NULL CONSTRAINT [DF_LoginHistory_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_LoginHistory] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [Audit].[ApplicationErrors]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]         uniqueidentifier NOT NULL CONSTRAINT [DF_ApplicationErrors_ExternalId] DEFAULT (NEWID()),
    [UserId]             bigint NULL,
    [CorrelationId]      uniqueidentifier NULL,
    [ErrorType]          nvarchar(256) NOT NULL,
    [ErrorCode]          nvarchar(100) NULL,
    [SafeMessage]        nvarchar(1000) NULL,
    [DiagnosticDetails]  nvarchar(max) NULL,
    [Source]             nvarchar(256) NULL,
    [RequestPath]        nvarchar(2048) NULL,
    [HttpMethod]         varchar(10) NULL,
    [StatusCode]         int NULL,
    [OccurredUtc]        datetime2(3) NOT NULL CONSTRAINT [DF_ApplicationErrors_OccurredUtc] DEFAULT (SYSUTCDATETIME()),
    [ResolutionStatus]   varchar(30) NOT NULL CONSTRAINT [DF_ApplicationErrors_ResolutionStatus] DEFAULT ('New'),
    [ResolvedUtc]        datetime2(3) NULL,
    [ResolvedByUserId]   bigint NULL,
    [ResolutionNotes]    nvarchar(2000) NULL,
    [RowVersion]         rowversion NOT NULL,
    CONSTRAINT [PK_ApplicationErrors] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_ApplicationErrors_ResolutionStatus] CHECK ([ResolutionStatus] IN ('New','Investigating','Resolved','Ignored')),
    CONSTRAINT [CK_ApplicationErrors_ResolutionState] CHECK
    (
        ([ResolutionStatus] IN ('New','Investigating') AND [ResolvedUtc] IS NULL)
        OR
        ([ResolutionStatus] IN ('Resolved','Ignored') AND [ResolvedUtc] IS NOT NULL)
    ),
    CONSTRAINT [CK_ApplicationErrors_StatusCode] CHECK ([StatusCode] IS NULL OR ([StatusCode] BETWEEN 100 AND 599))
);

/* =========================================================
   Communication
   ========================================================= */

CREATE TABLE [Communication].[Notifications]
(
    [Id]                         bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]                 uniqueidentifier NOT NULL CONSTRAINT [DF_Notifications_ExternalId] DEFAULT (NEWID()),
    [RecipientUserId]            bigint NOT NULL,
    [NotificationType]           nvarchar(100) NOT NULL,
    [Title]                      nvarchar(250) NOT NULL,
    [Message]                    nvarchar(max) NOT NULL,
    [RelatedEntityType]          nvarchar(150) NULL,
    [RelatedEntityExternalId]    uniqueidentifier NULL,
    [CreatedUtc]                 datetime2(3) NOT NULL CONSTRAINT [DF_Notifications_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedByUserId]            bigint NULL,
    [ReadUtc]                    datetime2(3) NULL,
    [ExpiresUtc]                 datetime2(3) NULL,
    [IsDeleted]                  bit NOT NULL CONSTRAINT [DF_Notifications_IsDeleted] DEFAULT (0),
    [DeletedUtc]                 datetime2(3) NULL,
    [DeletedByUserId]            bigint NULL,
    [RowVersion]                 rowversion NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_Notifications_DeleteState] CHECK
    (
        ([IsDeleted] = 0 AND [DeletedUtc] IS NULL AND [DeletedByUserId] IS NULL)
        OR
        ([IsDeleted] = 1 AND [DeletedUtc] IS NOT NULL)
    ),
    CONSTRAINT [CK_Notifications_Expiration] CHECK ([ExpiresUtc] IS NULL OR [ExpiresUtc] > [CreatedUtc]),
    CONSTRAINT [CK_Notifications_ReadTime] CHECK ([ReadUtc] IS NULL OR [ReadUtc] >= [CreatedUtc])
);

CREATE TABLE [Communication].[EmailHistory]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]         uniqueidentifier NOT NULL CONSTRAINT [DF_EmailHistory_ExternalId] DEFAULT (NEWID()),
    [RecipientUserId]    bigint NULL,
    [RecipientEmail]     nvarchar(320) NOT NULL,
    [EmailType]          nvarchar(100) NOT NULL,
    [TemplateName]       nvarchar(150) NULL,
    [Subject]            nvarchar(500) NOT NULL,
    [Provider]           nvarchar(100) NULL,
    [ProviderMessageId]  nvarchar(256) NULL,
    [DeliveryStatus]     varchar(50) NOT NULL CONSTRAINT [DF_EmailHistory_DeliveryStatus] DEFAULT ('Queued'),
    [QueuedUtc]          datetime2(3) NOT NULL CONSTRAINT [DF_EmailHistory_QueuedUtc] DEFAULT (SYSUTCDATETIME()),
    [SentUtc]            datetime2(3) NULL,
    [DeliveredUtc]       datetime2(3) NULL,
    [FailedUtc]          datetime2(3) NULL,
    [FailureReason]      nvarchar(2000) NULL,
    [CorrelationId]      uniqueidentifier NULL,
    [RowVersion]         rowversion NOT NULL,
    CONSTRAINT [PK_EmailHistory] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_EmailHistory_DeliveryStatus] CHECK ([DeliveryStatus] IN ('Queued','Sent','Delivered','Failed','Bounced','Cancelled')),
    CONSTRAINT [CK_EmailHistory_Timestamps] CHECK
    (
        ([SentUtc] IS NULL OR [SentUtc] >= [QueuedUtc])
        AND ([DeliveredUtc] IS NULL OR [DeliveredUtc] >= [QueuedUtc])
        AND ([FailedUtc] IS NULL OR [FailedUtc] >= [QueuedUtc])
    )
);

/* =========================================================
   Storage
   ========================================================= */

CREATE TABLE [Storage].[StoredFiles]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [ExternalId]         uniqueidentifier NOT NULL CONSTRAINT [DF_StoredFiles_ExternalId] DEFAULT (NEWID()),
    [OwnerUserId]        bigint NULL,
    [OriginalFileName]   nvarchar(260) NOT NULL,
    [SanitizedFileName]  nvarchar(260) NOT NULL,
    [StorageProvider]    nvarchar(50) NOT NULL,
    [StorageContainer]   nvarchar(255) NULL,
    [StorageKey]         nvarchar(1024) NOT NULL,
    [StorageLocationHash] AS
    (
        CONVERT(binary(32), HASHBYTES
        (
            'SHA2_256',
            CONCAT
            (
                [StorageProvider], NCHAR(31),
                CASE
                    WHEN [StorageContainer] IS NULL THEN N'<NULL>'
                    ELSE CONCAT(N'<VALUE>', [StorageContainer])
                END,
                NCHAR(31), [StorageKey]
            )
        ))
    ) PERSISTED,
    [ContentType]        nvarchar(255) NULL,
    [SizeBytes]          bigint NOT NULL,
    [ContentHash]        varbinary(32) NULL,
    [UploadStatus]       varchar(30) NOT NULL CONSTRAINT [DF_StoredFiles_UploadStatus] DEFAULT ('Pending'),
    [CreatedUtc]         datetime2(3) NOT NULL CONSTRAINT [DF_StoredFiles_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedByUserId]    bigint NULL,
    [ModifiedUtc]        datetime2(3) NULL,
    [ModifiedByUserId]   bigint NULL,
    [IsDeleted]          bit NOT NULL CONSTRAINT [DF_StoredFiles_IsDeleted] DEFAULT (0),
    [DeletedUtc]         datetime2(3) NULL,
    [DeletedByUserId]    bigint NULL,
    [RowVersion]         rowversion NOT NULL,
    CONSTRAINT [PK_StoredFiles] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_StoredFiles_SizeBytes] CHECK ([SizeBytes] >= 0),
    CONSTRAINT [CK_StoredFiles_UploadStatus] CHECK ([UploadStatus] IN ('Pending','Available','Failed','Quarantined')),
    CONSTRAINT [CK_StoredFiles_DeleteState] CHECK
    (
        ([IsDeleted] = 0 AND [DeletedUtc] IS NULL AND [DeletedByUserId] IS NULL)
        OR
        ([IsDeleted] = 1 AND [DeletedUtc] IS NOT NULL)
    )
);

/* =========================================================
   Foreign keys
   ========================================================= */

/* Identity self-references */
ALTER TABLE [Identity].[Users] ADD CONSTRAINT [FK_Users_CreatedByUser]
    FOREIGN KEY ([CreatedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Identity].[Users] ADD CONSTRAINT [FK_Users_ModifiedByUser]
    FOREIGN KEY ([ModifiedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Identity].[Users] ADD CONSTRAINT [FK_Users_DeletedByUser]
    FOREIGN KEY ([DeletedByUserId]) REFERENCES [Identity].[Users]([Id]);

ALTER TABLE [Identity].[Roles] ADD CONSTRAINT [FK_Roles_CreatedByUser]
    FOREIGN KEY ([CreatedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Identity].[Roles] ADD CONSTRAINT [FK_Roles_ModifiedByUser]
    FOREIGN KEY ([ModifiedByUserId]) REFERENCES [Identity].[Users]([Id]);

ALTER TABLE [Identity].[UserRoles] ADD CONSTRAINT [FK_UserRoles_User]
    FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Identity].[UserRoles] ADD CONSTRAINT [FK_UserRoles_Role]
    FOREIGN KEY ([RoleId]) REFERENCES [Identity].[Roles]([Id]);

ALTER TABLE [Identity].[UserClaims] ADD CONSTRAINT [FK_UserClaims_User]
    FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Identity].[RoleClaims] ADD CONSTRAINT [FK_RoleClaims_Role]
    FOREIGN KEY ([RoleId]) REFERENCES [Identity].[Roles]([Id]);
ALTER TABLE [Identity].[UserLogins] ADD CONSTRAINT [FK_UserLogins_User]
    FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Identity].[UserTokens] ADD CONSTRAINT [FK_UserTokens_User]
    FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users]([Id]);

/* Profile */
ALTER TABLE [Profile].[UserProfiles] ADD CONSTRAINT [FK_UserProfiles_User]
    FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Profile].[UserProfiles] ADD CONSTRAINT [FK_UserProfiles_CreatedByUser]
    FOREIGN KEY ([CreatedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Profile].[UserProfiles] ADD CONSTRAINT [FK_UserProfiles_ModifiedByUser]
    FOREIGN KEY ([ModifiedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Profile].[UserProfiles] ADD CONSTRAINT [FK_UserProfiles_DeletedByUser]
    FOREIGN KEY ([DeletedByUserId]) REFERENCES [Identity].[Users]([Id]);

ALTER TABLE [Profile].[UserPreferences] ADD CONSTRAINT [FK_UserPreferences_User]
    FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Profile].[UserPreferences] ADD CONSTRAINT [FK_UserPreferences_CreatedByUser]
    FOREIGN KEY ([CreatedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Profile].[UserPreferences] ADD CONSTRAINT [FK_UserPreferences_ModifiedByUser]
    FOREIGN KEY ([ModifiedByUserId]) REFERENCES [Identity].[Users]([Id]);

ALTER TABLE [Profile].[UserAgreements] ADD CONSTRAINT [FK_UserAgreements_User]
    FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users]([Id]);

/* Administration */
ALTER TABLE [Administration].[SettingDefinitions] ADD CONSTRAINT [FK_SettingDefinitions_CreatedByUser]
    FOREIGN KEY ([CreatedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Administration].[SettingDefinitions] ADD CONSTRAINT [FK_SettingDefinitions_ModifiedByUser]
    FOREIGN KEY ([ModifiedByUserId]) REFERENCES [Identity].[Users]([Id]);

ALTER TABLE [Administration].[SettingValues] ADD CONSTRAINT [FK_SettingValues_SettingDefinition]
    FOREIGN KEY ([SettingDefinitionId]) REFERENCES [Administration].[SettingDefinitions]([Id]);
ALTER TABLE [Administration].[SettingValues] ADD CONSTRAINT [FK_SettingValues_CreatedByUser]
    FOREIGN KEY ([CreatedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Administration].[SettingValues] ADD CONSTRAINT [FK_SettingValues_ModifiedByUser]
    FOREIGN KEY ([ModifiedByUserId]) REFERENCES [Identity].[Users]([Id]);

ALTER TABLE [Administration].[FeatureFlags] ADD CONSTRAINT [FK_FeatureFlags_CreatedByUser]
    FOREIGN KEY ([CreatedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Administration].[FeatureFlags] ADD CONSTRAINT [FK_FeatureFlags_ModifiedByUser]
    FOREIGN KEY ([ModifiedByUserId]) REFERENCES [Identity].[Users]([Id]);

/* Audit */
ALTER TABLE [Audit].[AuditLogs] ADD CONSTRAINT [FK_AuditLogs_User]
    FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Audit].[LoginHistory] ADD CONSTRAINT [FK_LoginHistory_User]
    FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Audit].[ApplicationErrors] ADD CONSTRAINT [FK_ApplicationErrors_User]
    FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Audit].[ApplicationErrors] ADD CONSTRAINT [FK_ApplicationErrors_ResolvedByUser]
    FOREIGN KEY ([ResolvedByUserId]) REFERENCES [Identity].[Users]([Id]);

/* Communication */
ALTER TABLE [Communication].[Notifications] ADD CONSTRAINT [FK_Notifications_RecipientUser]
    FOREIGN KEY ([RecipientUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Communication].[Notifications] ADD CONSTRAINT [FK_Notifications_CreatedByUser]
    FOREIGN KEY ([CreatedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Communication].[Notifications] ADD CONSTRAINT [FK_Notifications_DeletedByUser]
    FOREIGN KEY ([DeletedByUserId]) REFERENCES [Identity].[Users]([Id]);

ALTER TABLE [Communication].[EmailHistory] ADD CONSTRAINT [FK_EmailHistory_RecipientUser]
    FOREIGN KEY ([RecipientUserId]) REFERENCES [Identity].[Users]([Id]);

/* Storage */
ALTER TABLE [Storage].[StoredFiles] ADD CONSTRAINT [FK_StoredFiles_OwnerUser]
    FOREIGN KEY ([OwnerUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Storage].[StoredFiles] ADD CONSTRAINT [FK_StoredFiles_CreatedByUser]
    FOREIGN KEY ([CreatedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Storage].[StoredFiles] ADD CONSTRAINT [FK_StoredFiles_ModifiedByUser]
    FOREIGN KEY ([ModifiedByUserId]) REFERENCES [Identity].[Users]([Id]);
ALTER TABLE [Storage].[StoredFiles] ADD CONSTRAINT [FK_StoredFiles_DeletedByUser]
    FOREIGN KEY ([DeletedByUserId]) REFERENCES [Identity].[Users]([Id]);

/* =========================================================
   Indexes
   ========================================================= */

/* Identity */
CREATE UNIQUE NONCLUSTERED INDEX [UX_Users_ExternalId]
    ON [Identity].[Users]([ExternalId]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_Users_NormalizedUserName]
    ON [Identity].[Users]([NormalizedUserName])
    WHERE [NormalizedUserName] IS NOT NULL;
CREATE NONCLUSTERED INDEX [IX_Users_NormalizedEmail]
    ON [Identity].[Users]([NormalizedEmail]);
CREATE NONCLUSTERED INDEX [IX_Users_IsActive_IsDeleted]
    ON [Identity].[Users]([IsActive], [IsDeleted]);

CREATE UNIQUE NONCLUSTERED INDEX [UX_Roles_ExternalId]
    ON [Identity].[Roles]([ExternalId]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_Roles_NormalizedName]
    ON [Identity].[Roles]([NormalizedName])
    WHERE [NormalizedName] IS NOT NULL;
CREATE NONCLUSTERED INDEX [IX_Roles_IsActive]
    ON [Identity].[Roles]([IsActive]);

CREATE NONCLUSTERED INDEX [IX_UserRoles_RoleId]
    ON [Identity].[UserRoles]([RoleId]);
CREATE NONCLUSTERED INDEX [IX_UserClaims_UserId]
    ON [Identity].[UserClaims]([UserId]);
CREATE NONCLUSTERED INDEX [IX_RoleClaims_RoleId]
    ON [Identity].[RoleClaims]([RoleId]);
CREATE NONCLUSTERED INDEX [IX_UserLogins_UserId]
    ON [Identity].[UserLogins]([UserId]);

/* Profile */
CREATE UNIQUE NONCLUSTERED INDEX [UX_UserProfiles_ExternalId]
    ON [Profile].[UserProfiles]([ExternalId]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_UserProfiles_UserId]
    ON [Profile].[UserProfiles]([UserId]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_UserPreferences_UserId_PreferenceKey]
    ON [Profile].[UserPreferences]([UserId], [PreferenceKey]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_UserAgreements_ExternalId]
    ON [Profile].[UserAgreements]([ExternalId]);
CREATE NONCLUSTERED INDEX [IX_UserAgreements_UserId_Type_OccurredUtc]
    ON [Profile].[UserAgreements]([UserId], [AgreementType], [OccurredUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_UserAgreements_CorrelationId]
    ON [Profile].[UserAgreements]([CorrelationId])
    WHERE [CorrelationId] IS NOT NULL;

/* Administration */
CREATE UNIQUE NONCLUSTERED INDEX [UX_SettingDefinitions_ExternalId]
    ON [Administration].[SettingDefinitions]([ExternalId]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_SettingDefinitions_SettingKey]
    ON [Administration].[SettingDefinitions]([SettingKey]);
CREATE NONCLUSTERED INDEX [IX_SettingDefinitions_Category_IsActive]
    ON [Administration].[SettingDefinitions]([Category], [IsActive]);

CREATE UNIQUE NONCLUSTERED INDEX [UX_SettingValues_ExternalId]
    ON [Administration].[SettingValues]([ExternalId]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_SettingValues_Definition_Scope]
    ON [Administration].[SettingValues]([SettingDefinitionId], [ScopeType], [ScopeExternalId], [EnvironmentName]);
CREATE NONCLUSTERED INDEX [IX_SettingValues_Scope]
    ON [Administration].[SettingValues]([ScopeType], [ScopeExternalId], [EnvironmentName], [IsActive]);

CREATE UNIQUE NONCLUSTERED INDEX [UX_FeatureFlags_ExternalId]
    ON [Administration].[FeatureFlags]([ExternalId]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_FeatureFlags_FeatureKey]
    ON [Administration].[FeatureFlags]([FeatureKey]);
CREATE NONCLUSTERED INDEX [IX_FeatureFlags_IsActive_IsEnabled]
    ON [Administration].[FeatureFlags]([IsActive], [IsEnabled]);

/* Audit */
CREATE UNIQUE NONCLUSTERED INDEX [UX_AuditLogs_ExternalId]
    ON [Audit].[AuditLogs]([ExternalId]);
CREATE NONCLUSTERED INDEX [IX_AuditLogs_UserId_CreatedUtc]
    ON [Audit].[AuditLogs]([UserId], [CreatedUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_AuditLogs_EntityType_EntityId_CreatedUtc]
    ON [Audit].[AuditLogs]([EntityType], [EntityId], [CreatedUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_AuditLogs_EntityExternalId]
    ON [Audit].[AuditLogs]([EntityExternalId])
    WHERE [EntityExternalId] IS NOT NULL;
CREATE NONCLUSTERED INDEX [IX_AuditLogs_CorrelationId]
    ON [Audit].[AuditLogs]([CorrelationId])
    WHERE [CorrelationId] IS NOT NULL;
CREATE NONCLUSTERED INDEX [IX_AuditLogs_Action_CreatedUtc]
    ON [Audit].[AuditLogs]([Action], [CreatedUtc] DESC);

CREATE UNIQUE NONCLUSTERED INDEX [UX_LoginHistory_ExternalId]
    ON [Audit].[LoginHistory]([ExternalId]);
CREATE NONCLUSTERED INDEX [IX_LoginHistory_UserId_CreatedUtc]
    ON [Audit].[LoginHistory]([UserId], [CreatedUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_LoginHistory_IdentifierHash_CreatedUtc]
    ON [Audit].[LoginHistory]([IdentifierHash], [CreatedUtc] DESC)
    WHERE [IdentifierHash] IS NOT NULL;
CREATE NONCLUSTERED INDEX [IX_LoginHistory_IpAddress_CreatedUtc]
    ON [Audit].[LoginHistory]([IpAddress], [CreatedUtc] DESC)
    WHERE [IpAddress] IS NOT NULL;
CREATE NONCLUSTERED INDEX [IX_LoginHistory_EventType_CreatedUtc]
    ON [Audit].[LoginHistory]([EventType], [CreatedUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_LoginHistory_CorrelationId]
    ON [Audit].[LoginHistory]([CorrelationId])
    WHERE [CorrelationId] IS NOT NULL;

CREATE UNIQUE NONCLUSTERED INDEX [UX_ApplicationErrors_ExternalId]
    ON [Audit].[ApplicationErrors]([ExternalId]);
CREATE NONCLUSTERED INDEX [IX_ApplicationErrors_CorrelationId]
    ON [Audit].[ApplicationErrors]([CorrelationId])
    WHERE [CorrelationId] IS NOT NULL;
CREATE NONCLUSTERED INDEX [IX_ApplicationErrors_UserId_OccurredUtc]
    ON [Audit].[ApplicationErrors]([UserId], [OccurredUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_ApplicationErrors_ResolutionStatus_OccurredUtc]
    ON [Audit].[ApplicationErrors]([ResolutionStatus], [OccurredUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_ApplicationErrors_ErrorCode_OccurredUtc]
    ON [Audit].[ApplicationErrors]([ErrorCode], [OccurredUtc] DESC)
    WHERE [ErrorCode] IS NOT NULL;

/* Communication */
CREATE UNIQUE NONCLUSTERED INDEX [UX_Notifications_ExternalId]
    ON [Communication].[Notifications]([ExternalId]);
CREATE NONCLUSTERED INDEX [IX_Notifications_Recipient_Read_Created]
    ON [Communication].[Notifications]([RecipientUserId], [ReadUtc], [CreatedUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_Notifications_Recipient_Deleted_Created]
    ON [Communication].[Notifications]([RecipientUserId], [IsDeleted], [CreatedUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_Notifications_ExpiresUtc]
    ON [Communication].[Notifications]([ExpiresUtc])
    WHERE [ExpiresUtc] IS NOT NULL;
CREATE NONCLUSTERED INDEX [IX_Notifications_RelatedEntityExternalId]
    ON [Communication].[Notifications]([RelatedEntityExternalId])
    WHERE [RelatedEntityExternalId] IS NOT NULL;

CREATE UNIQUE NONCLUSTERED INDEX [UX_EmailHistory_ExternalId]
    ON [Communication].[EmailHistory]([ExternalId]);
CREATE NONCLUSTERED INDEX [IX_EmailHistory_RecipientUserId_QueuedUtc]
    ON [Communication].[EmailHistory]([RecipientUserId], [QueuedUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_EmailHistory_RecipientEmail_QueuedUtc]
    ON [Communication].[EmailHistory]([RecipientEmail], [QueuedUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_EmailHistory_ProviderMessageId]
    ON [Communication].[EmailHistory]([ProviderMessageId])
    WHERE [ProviderMessageId] IS NOT NULL;
CREATE NONCLUSTERED INDEX [IX_EmailHistory_DeliveryStatus_QueuedUtc]
    ON [Communication].[EmailHistory]([DeliveryStatus], [QueuedUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_EmailHistory_CorrelationId]
    ON [Communication].[EmailHistory]([CorrelationId])
    WHERE [CorrelationId] IS NOT NULL;

/* Storage */
CREATE UNIQUE NONCLUSTERED INDEX [UX_StoredFiles_ExternalId]
    ON [Storage].[StoredFiles]([ExternalId]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_StoredFiles_StorageLocationHash]
    ON [Storage].[StoredFiles]([StorageLocationHash]);
CREATE NONCLUSTERED INDEX [IX_StoredFiles_OwnerUserId_CreatedUtc]
    ON [Storage].[StoredFiles]([OwnerUserId], [CreatedUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_StoredFiles_UploadStatus_CreatedUtc]
    ON [Storage].[StoredFiles]([UploadStatus], [CreatedUtc] DESC);
CREATE NONCLUSTERED INDEX [IX_StoredFiles_ContentHash]
    ON [Storage].[StoredFiles]([ContentHash])
    WHERE [ContentHash] IS NOT NULL;

COMMIT TRANSACTION;
GO

PRINT N'RTSDB creation completed successfully.';
GO
