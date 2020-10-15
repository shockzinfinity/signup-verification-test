IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20201013092847_initial')
BEGIN
    CREATE TABLE [Accounts] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NULL,
        [FirstName] nvarchar(max) NULL,
        [LastName] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [PasswordHash] nvarchar(max) NULL,
        [AcceptTerms] bit NOT NULL,
        [Role] int NOT NULL,
        [VerificationToken] nvarchar(max) NULL,
        [VerifiedAt] datetime2 NULL,
        [ResetToken] nvarchar(max) NULL,
        [ResetTokenExpires] datetime2 NULL,
        [PasswordReset] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20201013092847_initial')
BEGIN
    CREATE TABLE [RefreshToken] (
        [Id] int NOT NULL IDENTITY,
        [AccountId] int NOT NULL,
        [Token] nvarchar(max) NULL,
        [Expires] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByIp] nvarchar(max) NULL,
        [Revoked] datetime2 NULL,
        [RevokedByIp] nvarchar(max) NULL,
        [ReplacedByToken] nvarchar(max) NULL,
        CONSTRAINT [PK_RefreshToken] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshToken_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20201013092847_initial')
BEGIN
    CREATE INDEX [IX_RefreshToken_AccountId] ON [RefreshToken] ([AccountId]);
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20201013092847_initial')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20201013092847_initial', N'3.1.8');
END;

GO

