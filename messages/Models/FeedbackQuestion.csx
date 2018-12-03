using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security;
using Microsoft.Bot.Builder.Azure;
using Microsoft.SharePoint.Client;

[Serializable]
public class FeedbackQuestion
{
    [JsonProperty(PropertyName="id")]
    public int Id { get; set; }
    [JsonProperty(PropertyName="user")]
    public User User { get; set; }
    [JsonProperty(PropertyName="channel_id")]
    public string ChannelId { get; set; }
    [JsonProperty(PropertyName="conversation")]
    public string Conversation { get; set; }
    [JsonProperty(PropertyName="question")]
    public string Question { get; set; }
    [JsonProperty(PropertyName="created_at")]
    public DateTime CreatedAt { get; set; }

    public async Task GetQuestionByIdAsync(int FeedbackQuestionId)
    {
        string QuestionQuery = @"
            BEGIN
                DECLARE @FeedbackQuestionUser INT;
                
                SELECT @FeedbackQuestionUser = [User] FROM [dbo].[FeedbackQuestions] WHERE [Id] = @FeedbackQuestionId;
                
                IF @FeedbackQuestionUser IS NOT NULL
                    BEGIN
                        SELECT 
							[dbo].[FeedbackQuestions].[Id],
                            [dbo].[FeedbackQuestions].[User],
                            [dbo].[FeedbackQuestions].[Conversation],
                            [dbo].[FeedbackQuestions].[Question],
                            [dbo].[FeedbackQuestions].[CreatedAt],
                            [dbo].[Users].[ChannelId]
						FROM 
							[dbo].[FeedbackQuestions] 
						LEFT JOIN
							[dbo].[Users] ON [dbo].[Users].[Id] = [FeedbackQuestions].[User]
						WHERE 
							[dbo].[FeedbackQuestions].[Id] = @FeedbackQuestionId;
                    END
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand QuestionCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = QuestionQuery})
                {
                    QuestionCommand.Parameters.Add("@FeedbackQuestionId", SqlDbType.Int).Value = FeedbackQuestionId;

                    using (SqlDataReader QuestionReader = await QuestionCommand.ExecuteReaderAsync())
                    {
                        if(QuestionReader.Read())
                        {
                            this.Id = QuestionReader.GetInt32(QuestionReader.GetOrdinal("Id"));
                            int UserId = !QuestionReader.IsDBNull(QuestionReader.GetOrdinal("User")) ? QuestionReader.GetInt32(QuestionReader.GetOrdinal("User")) : 0;
                            if(UserId > 0)
                            {
                                User user = new User();
                                await user.GetUserByIdAsync(UserId);
                                this.User = user;
                            }
                            this.ChannelId = QuestionReader.GetString(QuestionReader.GetOrdinal("ChannelId"));
                            this.Conversation = QuestionReader.GetString(QuestionReader.GetOrdinal("Conversation"));
                            this.Question = QuestionReader.GetString(QuestionReader.GetOrdinal("Question"));
                            this.CreatedAt = QuestionReader.GetDateTime(QuestionReader.GetOrdinal("CreatedAt"));                      
                        }
                    }
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }

    public async Task CreateFeedbackQuestionAsync(User user)
    {
        await this.AddFeedbackQuestionAsync(user);
        await this.GetQuestionByIdAsync(this.Id);
    }

    private async Task AddFeedbackQuestionAsync(User user)
    {
        string AddFeedbackQuestionQuery = $@"
            BEGIN
                BEGIN TRANSACTION;

                DECLARE @QuestionId INT;
				DECLARE @FeedbackQuestionTable table (Id INT)

                INSERT INTO [dbo].[FeedbackQuestions] ( [User], [Conversation], [Question] ) OUTPUT Inserted.Id into @FeedbackQuestionTable VALUES ( @User, @Conversation, @Question );
                SELECT @QuestionId = [Id] from @FeedbackQuestionTable

                COMMIT; 

                SELECT 
                    @QuestionId as [QuestionId]
                ;
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand AddFeedbackQuestionCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = AddFeedbackQuestionQuery})
                {
                    AddFeedbackQuestionCommand.Parameters.Add("@User", SqlDbType.Int).Value = user.Id;
                    AddFeedbackQuestionCommand.Parameters.Add("@Conversation", SqlDbType.NVarChar, 255).Value = this.Conversation;
                    AddFeedbackQuestionCommand.Parameters.Add("@Question", SqlDbType.NVarChar, 255).Value = this.Question;
                    this.Id = (int) await AddFeedbackQuestionCommand.ExecuteScalarAsync();
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            conn.Close();
        }
    }
}