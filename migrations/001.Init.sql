CREATE TABLE [dbo].[Users]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ChannelId] VARCHAR(50) NULL DEFAULT NULL,
    [ChannelAccountId] NVARCHAR(50) NULL DEFAULT NULL,
    [ChannelAccountName] NVARCHAR(50) NULL DEFAULT NULL,
    [CreatedAt] DATETIME NULL DEFAULT(getdate())
)

CREATE TABLE [dbo].[Exercises]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ItemListID] INT NULL DEFAULT NULL,
    [ItemListTitle] NVARCHAR(50) NULL DEFAULT NULL,
    [CreatedAt] DATETIME NULL DEFAULT(getdate())
)

CREATE TABLE [dbo].[Answers]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [User] INT NULL DEFAULT NULL, 
    [Exercise] INT NULL DEFAULT NULL, 
    [Answer] SMALLINT NULL DEFAULT NULL, 
    [IsCorrect] BIT NULL DEFAULT NULL, 
    [CreatedAt] DATETIME NULL DEFAULT(getdate()),
    CONSTRAINT [FK_Answers_ToUsers] FOREIGN KEY ([User]) REFERENCES [Users]([Id]),
    CONSTRAINT [FK_Answers_ToExercises] FOREIGN KEY ([Exercise]) REFERENCES [Exercises]([Id])
)