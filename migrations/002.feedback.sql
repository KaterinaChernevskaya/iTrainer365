CREATE TABLE [dbo].[FeedbackQuestions]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [User] INT NULL DEFAULT NULL, 
    [Conversation] NVARCHAR(255) NULL DEFAULT NULL,
    [Question] NVARCHAR(255) NULL DEFAULT NULL, 
    [CreatedAt] DATETIME NULL DEFAULT(getdate()),
    CONSTRAINT [FK_FeedbackQuestions_ToUsers] FOREIGN KEY ([User]) REFERENCES [Users]([Id])
)

CREATE TABLE [dbo].[FeedbackAnswers]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [FeedbackQuestion] INT NULL DEFAULT NULL,
    [Answer] NVARCHAR(255) NULL DEFAULT NULL,
    [CreatedAt] DATETIME NULL DEFAULT(getdate()),
    CONSTRAINT [FK_FeedbackAnswers_FeedbackQuestions] FOREIGN KEY ([FeedbackQuestion]) REFERENCES [FeedbackQuestions]([Id])
)