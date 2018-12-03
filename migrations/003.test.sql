CREATE TABLE [dbo].[Tests]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Title] NVARCHAR(50) NULL DEFAULT NULL,
    [CreatedAt] DATETIME NULL DEFAULT(getdate())
)

CREATE TABLE [dbo].[TestAnswers]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [User] INT NULL DEFAULT NULL, 
    [Test] INT NULL DEFAULT NULL,
    [Exercise] INT NULL DEFAULT NULL, 
    [Answer] SMALLINT NULL DEFAULT NULL, 
    [IsCorrect] BIT NULL DEFAULT NULL, 
    [CreatedAt] DATETIME NULL DEFAULT(getdate()),
    CONSTRAINT [FK_TestAnswers_ToUsers] FOREIGN KEY ([User]) REFERENCES [Users]([Id]),
    CONSTRAINT [FK_TestAnswers_ToTests] FOREIGN KEY ([Test]) REFERENCES [Tests]([Id]),
    CONSTRAINT [FK_TestAnswers_ToExercises] FOREIGN KEY ([Exercise]) REFERENCES [Exercises]([Id])
)

CREATE TABLE [dbo].[TestResults]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [User] INT NULL DEFAULT NULL, 
    [Test] INT NULL DEFAULT NULL,
    [Result] DECIMAL(5,4) NULL DEFAULT NULL, 
    [CreatedAt] DATETIME NULL DEFAULT(getdate()),
    CONSTRAINT [FK_TestResults_ToUsers] FOREIGN KEY ([User]) REFERENCES [Users]([Id]),
    CONSTRAINT [FK_TestResults_ToTests] FOREIGN KEY ([Test]) REFERENCES [Tests]([Id])
)