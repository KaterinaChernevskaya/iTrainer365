using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security;
using Microsoft.Bot.Builder.Azure;
using Microsoft.SharePoint.Client;

[Serializable]
public class FeedbackAnswer
{
    [JsonProperty(PropertyName="id")]
    public int Id { get; set; }
    [JsonProperty(PropertyName="question")]
    public int Question { get; set; }
    [JsonProperty(PropertyName="answer")]
    public string Answer { get; set; }
    [JsonProperty(PropertyName="created_at")]
    public DateTime CreatedAt { get; set; }

    public async Task CreateFeedbackAnswerAsync()
    {
        await this.AddFeedbackAnswerAsync();
    }

    private async Task<bool> AddFeedbackAnswerAsync()
    {
        string AddFeedbackAnswerQuery = @"
            BEGIN
                BEGIN TRANSACTION;

                DECLARE @FeedbackQuestion INT;
                DECLARE @FeedbackAnswerInserted BIT;

                SET @FeedbackAnswerInserted = 0;
                SELECT @FeedbackQuestion = [Id] FROM [dbo].[FeedbackQuestions] WHERE [Id] = @FeedbackQuestionId;

                IF @FeedbackQuestion IS NOT NULL
                    BEGIN
                        INSERT INTO [dbo].[FeedbackAnswers] ( [FeedbackQuestion], [Answer] ) VALUES ( @FeedbackQuestion, @FeedbackAnswer );
                        SET @FeedbackAnswerInserted = 1;
                    END

                COMMIT; 

                SELECT 
                    @FeedbackAnswerInserted as [FeedbackAnswerInserted]
                ;
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand AddFeedbackAnswerCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = AddFeedbackAnswerQuery})
                {
                    AddFeedbackAnswerCommand.Parameters.Add("@FeedbackQuestionId", SqlDbType.Int).Value = this.Question;
                    AddFeedbackAnswerCommand.Parameters.Add("@FeedbackAnswer", SqlDbType.NVarChar, 255).Value = this.Answer;
                    bool result = (bool) await AddFeedbackAnswerCommand.ExecuteScalarAsync();
                    conn.Close();
                    return result;
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}