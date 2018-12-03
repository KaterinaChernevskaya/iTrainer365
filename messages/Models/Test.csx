using System.Resources;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Connector;
using Microsoft.SharePoint.Client;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

[Serializable]
public class TestQuestionAnswerValue
{
    [JsonProperty(PropertyName="t")]
    public string TestTitle { get; set; }
    [JsonProperty(PropertyName="q")]
    public int Question { get; set; }
    [JsonProperty(PropertyName="a")]
    public int Answer { get; set; }
    [JsonProperty(PropertyName="n")]
    public int QuestionNumber { get; set; }
    [JsonProperty(PropertyName="c")]
    public int QuestionCorrectCounter { get; set; }
}

[Serializable]
public class TestQuestionAnswer
{
    [JsonProperty(PropertyName="id")]
    public int Id { get; set; }
    [JsonProperty(PropertyName="text")]
    public string AnswerText { get; set; }
    [JsonProperty(PropertyName="button")]
    public CardAction Button { get; set; }
    [JsonProperty(PropertyName="correct")]
    public bool Correct { get; set; }
}

[Serializable]
public class TestQuestion
{
    [JsonProperty(PropertyName="id")]
    public int Id { get; set; }
    [JsonProperty(PropertyName="title")]
    public string Title { get; set; }
    [JsonProperty(PropertyName="subtitle")]
    public string Subtitle { get; set; }
    [JsonProperty(PropertyName="language")]
    public string Language { get; set; }
    [JsonProperty(PropertyName="text")]
    public string TaskText { get; set; }
    [JsonProperty(PropertyName="answers")]
    public List<TestQuestionAnswer> Answers { get; set; }
    [JsonProperty(PropertyName="date")]
    public DateTime Date { get; set; }
    [JsonProperty(PropertyName="active")]
    public bool Active { get; set; }
}

[Serializable]
public class Test
{
    [JsonProperty(PropertyName="title")]
    public string Title { get; set; }
    [JsonProperty(PropertyName="questions")]
    public SortedList<int, TestQuestion> Questions { get; set; }
    private string SharePointSiteUrl { get; set; }
    private string SharePointUserName { get; set; }
    private string SharePointUserPassword { get; set; }
    private string SharePointExerciseListTitle { get; set; }
    private string SharePointCompanyListTitle { get; set; }
    private ResourceManager LocalizationLanguage { get; set; }

    public Test(string functionDirectory)
    {
        this.SharePointSiteUrl = Utils.GetAppSetting("SharePointSiteUrl");
        this.SharePointUserName = Utils.GetAppSetting("SharePointUserName");
        this.SharePointUserPassword = Utils.GetAppSetting("SharePointUserPassword");
        this.SharePointExerciseListTitle = Utils.GetAppSetting("SharePointExerciseListTitle");
        this.SharePointCompanyListTitle = Utils.GetAppSetting("SharePointCompanyListTitle");
        this.LocalizationLanguage = ResourceManager.CreateFileBasedResourceManager("Language", Path.Combine(functionDirectory, "Resources\\Localization"), null);
    }

    public async Task GetTestByTitleAsync(string TestTitle, int QuestionNumber = 0, int QuestionCorrectCounter = 0)
    {
        this.Questions = new SortedList<int, TestQuestion>();
        Folder TestFolder;

        SecureString securePwd = new SecureString();
        foreach (char ch in this.SharePointUserPassword)
            securePwd.AppendChar(ch);

        using (ClientContext cli = new ClientContext(this.SharePointSiteUrl))
        {
            cli.Credentials = new SharePointOnlineCredentials(this.SharePointUserName, securePwd);
            Web web = cli.Web;

            List oList = web.Lists.GetByTitle(this.SharePointExerciseListTitle);
            cli.Load(oList.RootFolder);
            cli.ExecuteQuery();

            try
            {
                TestFolder = web.GetFolderByServerRelativeUrl(oList.RootFolder.ServerRelativeUrl + "/" + Utils.GetAppSetting("SharePointExerciseTestFolder") + "/" + TestTitle);   
                cli.Load(TestFolder);
                cli.ExecuteQuery();
            }
            catch
            {
                return;
            }
            
            if(TestFolder.Exists)
            {
                TestQuestion CurrentTestQuestion;
                this.Title = TestFolder.Name.Trim().First().ToString().ToUpper() + TestFolder.Name.Trim().Substring(1);
                
                CamlQuery camlQuery = new CamlQuery();
                camlQuery.ViewXml = $@"
                <View>
                    <Query>
                        <ViewFields>
                            <FieldRef Name='{Utils.GetAppSetting("SharePointExerciseFieldID")}' />
                        </ViewFields>
                        <Where>
                            <And>
                                <Eq>
                                    <FieldRef Name='{Utils.GetAppSetting("SharePointExerciseFieldLanguage")}'/>
                                    <Value Type='Choice'>{this.LocalizationLanguage.GetString("LanguageTitle")}</Value>
                                </Eq>
                                <Eq>
                                    <FieldRef Name='{Utils.GetAppSetting("SharePointExerciseFieldActive")}'/>
                                    <Value Type='Boolean'>1</Value>
                                </Eq>
                            </And>
                        </Where>
                    </Query>
                    <RowLimit>
                        100
                    </RowLimit>
                </view>";

                camlQuery.FolderServerRelativeUrl = TestFolder.ServerRelativeUrl;

                ListItemCollection collListItem = oList.GetItems(camlQuery);
                cli.Load(collListItem);
                cli.ExecuteQuery();

                foreach (ListItem listItem in collListItem)
                {
                    Match match = Regex.Match(
                        Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldCorrectAnswer")]), 
                        @"\d+"
                    );

                    int CorrectAnswer = match.Success ? Int32.Parse(match.Value) : 0;
                    
                    CurrentTestQuestion = new TestQuestion();
                    CurrentTestQuestion.Id = Int32.Parse(Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldID")]));
                    CurrentTestQuestion.Title = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldTitle")]);
                    CurrentTestQuestion.Subtitle = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldSubtitle")]);
                    CurrentTestQuestion.Language = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldLanguage")]);
                    CurrentTestQuestion.TaskText = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldTaskText")]);
                    CurrentTestQuestion.Answers = new List<TestQuestionAnswer>{
                        new TestQuestionAnswer(){
                            Id = 1,
                            AnswerText = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldButton1Answer")]),
                            Button = new CardAction(){
                                Title = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldButton1")]),
                                DisplayText = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldButton1")]),
                                Type = ActionTypes.PostBack,
                                Value = JsonConvert.SerializeObject((new TestQuestionAnswerValue(){ TestTitle = this.Title, Question = CurrentTestQuestion.Id, Answer = 1, QuestionNumber = QuestionNumber, QuestionCorrectCounter = QuestionCorrectCounter }))
                            },
                            Correct = CorrectAnswer == 1 ? true : false
                        },
                        new TestQuestionAnswer(){
                            Id = 2,
                            AnswerText = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldButton2Answer")]),
                            Button = new CardAction(){
                                Title = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldButton2")]),
                                DisplayText = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldButton2")]),
                                Type = ActionTypes.PostBack,
                                Value = JsonConvert.SerializeObject((new TestQuestionAnswerValue(){ TestTitle = this.Title, Question = CurrentTestQuestion.Id, Answer = 2, QuestionNumber = QuestionNumber, QuestionCorrectCounter = QuestionCorrectCounter }))
                            },
                            Correct = CorrectAnswer == 2 ? true : false
                        },
                        new TestQuestionAnswer(){
                            Id = 3,
                            AnswerText = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldButton3Answer")]),
                            Button = new CardAction(){
                                Title = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldButton3")]),
                                DisplayText = Convert.ToString(listItem[Utils.GetAppSetting("SharePointExerciseFieldButton3")]),
                                Type = ActionTypes.PostBack,
                                Value = JsonConvert.SerializeObject((new TestQuestionAnswerValue(){ TestTitle = this.Title, Question = CurrentTestQuestion.Id, Answer = 3, QuestionNumber = QuestionNumber, QuestionCorrectCounter = QuestionCorrectCounter }))
                            },
                            Correct = CorrectAnswer == 3 ? true : false
                        }
                    };
                    CurrentTestQuestion.Date = (DateTime) listItem[Utils.GetAppSetting("SharePointExerciseFieldDate")];
                    CurrentTestQuestion.Active = (bool) listItem[Utils.GetAppSetting("SharePointExerciseFieldActive")];

                    this.Questions.Add(CurrentTestQuestion.Id, CurrentTestQuestion);
                    CurrentTestQuestion = null;
                }
            }
        }
        await Task.FromResult(this);
    }

    public async Task AddTestAnswerAsync(int TestQuestion, int TestAnswer, User user)
    {
        bool CorrectAnswer = false;

        if(TestQuestion == 0 || TestAnswer == 0)
        {
            return;
        }

        if(this.Questions[TestQuestion].Answers[TestAnswer-1].Correct)
        {
            CorrectAnswer = true;
        }

        string AddTestAnswerQuery = $@"
            BEGIN
                BEGIN TRANSACTION;

                DECLARE @Test INT;
				DECLARE @TestQuestion INT;

                SELECT @Test = [Id] FROM [dbo].[Tests] WHERE [Title] = @TestTitle;
				SELECT @TestQuestion = [Id] FROM [dbo].[Exercises] WHERE [ItemListID] = @TestQuestionItemListID;
                
                IF @Test IS NULL
                    BEGIN
                        DECLARE @TestTable table (Id INT)
                        INSERT INTO [dbo].[Tests] ( [Title] ) OUTPUT Inserted.Id into @TestTable VALUES ( @TestTitle );
                        SELECT @Test = [Id] from @TestTable
                    END
				
				IF @TestQuestion IS NULL
                    BEGIN
                        DECLARE @TestQuestionTable table (Id INT)
                        INSERT INTO [dbo].[Exercises] ( [ItemListID], [ItemListTitle] ) OUTPUT Inserted.Id into @TestQuestionTable VALUES ( @TestQuestionItemListID, @TestQuestionItemListTitle );
                        SELECT @TestQuestion = [Id] from @TestQuestionTable
                    END

                INSERT INTO [dbo].[TestAnswers] ( [User], [Test], [Exercise], [Answer], [IsCorrect] ) VALUES ( @User, @Test, @TestQuestion, @TestAnswer, @IsCorrect );

                COMMIT; 
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand AddTestAnswerCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = AddTestAnswerQuery})
                {
                    AddTestAnswerCommand.Parameters.Add("@TestTitle", SqlDbType.NVarChar, 50).Value = this.Title;
                    AddTestAnswerCommand.Parameters.Add("@TestQuestionItemListID", SqlDbType.Int).Value = this.Questions[TestQuestion].Id;
                    AddTestAnswerCommand.Parameters.Add("@TestQuestionItemListTitle", SqlDbType.NVarChar, 50).Value = this.Questions[TestQuestion].Title;
                    AddTestAnswerCommand.Parameters.Add("@User", SqlDbType.Int).Value = user.Id;
                    AddTestAnswerCommand.Parameters.Add("@TestAnswer", SqlDbType.SmallInt).Value = TestAnswer;
                    AddTestAnswerCommand.Parameters.Add("@IsCorrect", SqlDbType.Bit).Value = CorrectAnswer;
                    await AddTestAnswerCommand.ExecuteScalarAsync();
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            conn.Close();
        }
    }

    public async Task AddTestResultAsync(decimal TestResult, User user)
    {
        string AddTestResultQuery = $@"
            BEGIN
                BEGIN TRANSACTION;

                DECLARE @Test INT;

                SELECT @Test = [Id] FROM [dbo].[Tests] WHERE [Title] = @TestTitle;
                
                INSERT INTO [dbo].[TestResults] ( [User], [Test], [Result] ) VALUES ( @User, @Test, @TestResult );

                COMMIT; 
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand AddTestResultCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = AddTestResultQuery})
                {
                    AddTestResultCommand.Parameters.Add("@TestTitle", SqlDbType.NVarChar, 50).Value = this.Title;
                    AddTestResultCommand.Parameters.Add("@User", SqlDbType.Int).Value = user.Id;
                    AddTestResultCommand.Parameters.Add("@TestResult", SqlDbType.Decimal, 5).Value = TestResult;
                    await AddTestResultCommand.ExecuteScalarAsync();
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            conn.Close();
        }
    }

    public async Task<decimal> GetBestTestResultAsync(User user)
    {
        string GetBestTestResultQuery = $@"
            BEGIN
                BEGIN TRANSACTION;

                DECLARE @Test INT;
				DECLARE @BestTestResult DECIMAL(5,4);

                SELECT @Test = [Id] FROM [dbo].[Tests] WHERE [Title] = @TestTitle;

                SELECT @BestTestResult = MAX([Result]) FROM [dbo].[TestResults] WHERE [dbo].[TestResults].[User] = @User AND [dbo].[TestResults].[Test] = @Test;

                IF @BestTestResult IS NULL
                    SET @BestTestResult = 0;

                SELECT 
                    @BestTestResult as [BestTestResult]
                ;

                COMMIT; 
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand GetBestTestResultCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = GetBestTestResultQuery})
                {
                    GetBestTestResultCommand.Parameters.Add("@TestTitle", SqlDbType.NVarChar, 50).Value = this.Title;
                    GetBestTestResultCommand.Parameters.Add("@User", SqlDbType.Int).Value = user.Id;
                    decimal result = (decimal) await GetBestTestResultCommand.ExecuteScalarAsync();
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