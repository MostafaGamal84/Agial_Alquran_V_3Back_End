SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/*
Purpose:
- Roll back the custom permission system that was added on top of the original database schema.

What this script removes:
1. dbo.UserPermission table.
2. dbo.[User].UseCustomPermissions column and its default constraint.

What this script intentionally does NOT remove:
- dbo.Permission table itself.
- Existing rows inside dbo.Permission.

Reason:
- dbo.Permission is part of the original schema in this project.
- Keeping it avoids accidental data loss if the table was already used before the custom permission system.

Usage:
1. Run on the target database.
2. The script is idempotent:
   - if dbo.UserPermission does not exist, it will skip it.
   - if dbo.[User].UseCustomPermissions does not exist, it will skip it.
*/
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.UserPermission', N'U') IS NOT NULL
    BEGIN
        PRINT N'Dropping dbo.UserPermission table...';
        DROP TABLE dbo.UserPermission;
        PRINT N'dbo.UserPermission table dropped successfully.';
    END
    ELSE
    BEGIN
        PRINT N'dbo.UserPermission table does not exist. Nothing to drop.';
    END;

    IF COL_LENGTH(N'dbo.[User]', N'UseCustomPermissions') IS NOT NULL
    BEGIN
        DECLARE @DefaultConstraintName SYSNAME;
        DECLARE @DropConstraintSql NVARCHAR(MAX);

        SELECT TOP (1) @DefaultConstraintName = dc.name
        FROM sys.default_constraints AS dc
        INNER JOIN sys.columns AS c
            ON c.default_object_id = dc.object_id
        INNER JOIN sys.tables AS t
            ON t.object_id = c.object_id
        INNER JOIN sys.schemas AS s
            ON s.schema_id = t.schema_id
        WHERE s.name = N'dbo'
          AND t.name = N'User'
          AND c.name = N'UseCustomPermissions';

        IF @DefaultConstraintName IS NOT NULL
        BEGIN
            SET @DropConstraintSql =
                N'ALTER TABLE dbo.[User] DROP CONSTRAINT ' + QUOTENAME(@DefaultConstraintName) + N';';

            PRINT N'Dropping default constraint on dbo.[User].UseCustomPermissions...';
            EXEC sp_executesql @DropConstraintSql;
        END;

        PRINT N'Dropping dbo.[User].UseCustomPermissions column...';
        ALTER TABLE dbo.[User] DROP COLUMN UseCustomPermissions;
        PRINT N'dbo.[User].UseCustomPermissions column dropped successfully.';
    END
    ELSE
    BEGIN
        PRINT N'dbo.[User].UseCustomPermissions column does not exist. Nothing to drop.';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
GO

SELECT
    UserPermissionTableExists =
        CASE WHEN OBJECT_ID(N'dbo.UserPermission', N'U') IS NULL THEN 0 ELSE 1 END,
    UseCustomPermissionsColumnExists =
        CASE WHEN COL_LENGTH(N'dbo.[User]', N'UseCustomPermissions') IS NULL THEN 0 ELSE 1 END;
GO
