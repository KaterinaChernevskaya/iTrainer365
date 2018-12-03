CREATE TABLE [dbo].[Subscription]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [User] INT NULL DEFAULT NULL, 
    [Hour] INT NULL DEFAULT NULL,
    [Conversation] NVARCHAR(255) NULL DEFAULT NULL,
    [ServiceUrl] NVARCHAR(255) NULL DEFAULT NULL, 
    [CreatedAt] DATETIME NULL DEFAULT(getdate()),
    CONSTRAINT [FK_Subscription_ToUsers] FOREIGN KEY ([User]) REFERENCES [Users]([Id])
)

ALTER TABLE [dbo].[Users] ALTER COLUMN [ChannelAccountId] NVARCHAR(255);