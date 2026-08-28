SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID(N'[Trading].[DiscoveryCandidates]', N'U') IS NULL
    THROW 50000, 'Run CandidateDiscovery_Create.sql before CandidateWatchlistMembership_Update.sql.', 1;

IF COL_LENGTH(N'Trading.DiscoveryCandidates', N'IsWatchlisted') IS NULL
BEGIN
    EXEC(N'ALTER TABLE [Trading].[DiscoveryCandidates]
        ADD [IsWatchlisted] bit NOT NULL
            CONSTRAINT [DF_DiscoveryCandidates_IsWatchlisted] DEFAULT 0;');
END;

EXEC(N'UPDATE [Trading].[DiscoveryCandidates]
    SET [IsWatchlisted] = 1
    WHERE [Source] = 2 AND [IsWatchlisted] = 0;');

COMMIT TRANSACTION;
PRINT N'Candidate watchlist membership added successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
