IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE TABLE [References] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Designation] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_References] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE TABLE [Cotes] (
        [Id] int NOT NULL IDENTITY,
        [ReferenceId] int NOT NULL,
        [Ordre] int NOT NULL,
        [Nom] nvarchar(80) NOT NULL,
        [Nominal] float NOT NULL,
        [LimiteInferieure] float NOT NULL,
        [LimiteSuperieure] float NOT NULL,
        CONSTRAINT [PK_Cotes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Cotes_Limites] CHECK ([LimiteInferieure] < [LimiteSuperieure]),
        CONSTRAINT [FK_Cotes_References_ReferenceId] FOREIGN KEY ([ReferenceId]) REFERENCES [References] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE TABLE [OrdresFabrication] (
        [Id] int NOT NULL IDENTITY,
        [Numero] nvarchar(30) NOT NULL,
        [ReferenceId] int NOT NULL,
        [Quantite] int NOT NULL,
        [OuvertLe] datetime2 NOT NULL,
        [Clos] bit NOT NULL,
        CONSTRAINT [PK_OrdresFabrication] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrdresFabrication_References_ReferenceId] FOREIGN KEY ([ReferenceId]) REFERENCES [References] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE TABLE [Pieces] (
        [Id] int NOT NULL IDENTITY,
        [OrdreFabricationId] int NOT NULL,
        [Numero] nvarchar(40) NOT NULL,
        [ControleeLe] datetime2 NOT NULL,
        [Operateur] nvarchar(60) NOT NULL,
        [Etat] nvarchar(15) NOT NULL,
        [MotifQuarantaine] nvarchar(300) NULL,
        [QuarantaineLe] datetime2 NULL,
        CONSTRAINT [PK_Pieces] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pieces_OrdresFabrication_OrdreFabricationId] FOREIGN KEY ([OrdreFabricationId]) REFERENCES [OrdresFabrication] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE TABLE [Mesures] (
        [Id] bigint NOT NULL IDENTITY,
        [PieceControleeId] int NOT NULL,
        [CoteId] int NOT NULL,
        [Valeur] float NOT NULL,
        CONSTRAINT [PK_Mesures] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Mesures_Cotes_CoteId] FOREIGN KEY ([CoteId]) REFERENCES [Cotes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Mesures_Pieces_PieceControleeId] FOREIGN KEY ([PieceControleeId]) REFERENCES [Pieces] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Cotes_ReferenceId_Ordre] ON [Cotes] ([ReferenceId], [Ordre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE INDEX [IX_Mesures_CoteId] ON [Mesures] ([CoteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE INDEX [IX_Mesures_PieceControleeId] ON [Mesures] ([PieceControleeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrdresFabrication_Numero] ON [OrdresFabrication] ([Numero]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE INDEX [IX_OrdresFabrication_ReferenceId] ON [OrdresFabrication] ([ReferenceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE INDEX [IX_Pieces_ControleeLe] ON [Pieces] ([ControleeLe]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Pieces_OrdreFabricationId_Numero] ON [Pieces] ([OrdreFabricationId], [Numero]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_References_Code] ON [References] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116200444_SchemaInitial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261116200444_SchemaInitial', N'10.0.15');
END;

COMMIT;
GO

