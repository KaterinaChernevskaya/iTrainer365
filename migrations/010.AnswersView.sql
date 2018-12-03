CREATE VIEW [dbo].[AnswersView]
	AS 
		SELECT
			[dbo].[Answers].[Id], 
			[dbo].[Users].[ChannelAccountName],
			[dbo].[Users].[ChannelId],
			[dbo].[Exercises].[ItemListTitle],
			[dbo].[Answers].[Answer],
			[dbo].[Answers].[IsCorrect],
			[dbo].[Answers].[CreatedAt]
		FROM 
			[dbo].[Answers]
				LEFT JOIN [dbo].[Users] ON [dbo].[Answers].[User] = [dbo].[Users].[Id]
				LEFT JOIN [dbo].[Exercises] ON [dbo].[Answers].[Exercise] = [dbo].[Exercises].[Id]