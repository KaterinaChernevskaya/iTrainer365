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
public class ExerciseAnswerValue
{
    [JsonProperty(PropertyName="excercise")]
    public int Excercise { get; set; }
    [JsonProperty(PropertyName="answer")]
    public int Answer { get; set; }
}

[Serializable]
public class ExerciseAnswer
{
    [JsonProperty(PropertyName="id")]
    public int Id { get; set; }
    [JsonProperty(PropertyName="text")]
    public string AnswerText { get; set; }
    [NonSerialized]
    [JsonProperty(PropertyName="button")]
    public CardAction Button;
    [JsonProperty(PropertyName="correct")]
    public bool Correct { get; set; }
}

[Serializable]
public class Exercise
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
    public List<ExerciseAnswer> Answers { get; set; }
    [JsonProperty(PropertyName="date")]
    public DateTime Date { get; set; }
    [JsonProperty(PropertyName="active")]
    public bool Active { get; set; }
    private string SharePointSiteUrl { get; set; }
    private string SharePointUserName { get; set; }
    private string SharePointUserPassword { get; set; }
    private string SharePointExerciseListTitle { get; set; }
    private string SharePointCompanyListTitle { get; set; }
    private ResourceManager LocalizationLanguage { get; set; }

    public Exercise(string functionDirectory)
    {
        this.SharePointSiteUrl = Utils.GetAppSetting("SharePointSiteUrl");
        this.SharePointUserName = Utils.GetAppSetting("SharePointUserName");
        this.SharePointUserPassword = Utils.GetAppSetting("SharePointUserPassword");
        this.SharePointExerciseListTitle = Utils.GetAppSetting("SharePointExerciseListTitle");
        this.SharePointCompanyListTitle = Utils.GetAppSetting("SharePointCompanyListTitle");
        this.LocalizationLanguage = ResourceManager.CreateFileBasedResourceManager("Language", Path.Combine(functionDirectory, "Resources\\Localization"), null);
    }

    public async Task GetTodayExcerciseAsync()
    {
        Folder RegularFolder;

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
                RegularFolder = web.GetFolderByServerRelativeUrl(oList.RootFolder.ServerRelativeUrl + "/" + Utils.GetAppSetting("SharePointExerciseRegularFolder"));   
                cli.Load(RegularFolder);
                cli.ExecuteQuery();
            }
            catch
            {
                return;
            }

            if(RegularFolder.Exists)
            {
                string Date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");

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
                                    <FieldRef Name='{Utils.GetAppSetting("SharePointExerciseFieldDate")}'/>
                                    <Value Type='DateTime' IncludeTimeValue='false'>{Date}</Value>
                                </Eq>
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
                            </And>
                        </Where>
                    </Query>
                    <RowLimit>
                        1
                    </RowLimit>
                </view>";

                camlQuery.FolderServerRelativeUrl = RegularFolder.ServerRelativeUrl;

                ListItemCollection collListItem = oList.GetItems(camlQuery);
                    
                cli.Load(collListItem);
                cli.ExecuteQuery();

                if(collListItem.Count > 0)
                {
                    Match match = Regex.Match(
                        Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldCorrectAnswer")]), 
                        @"\d+"
                    );

                    int CorrectAnswer = match.Success ? Int32.Parse(match.Value) : 0;
                    
                    this.Id = Int32.Parse(Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldID")]));
                    this.Title = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldTitle")]);
                    this.Subtitle = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldSubtitle")]);
                    this.Language = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldLanguage")]);
                    this.TaskText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldTaskText")]);
                    this.Answers = new List<ExerciseAnswer>{
                        new ExerciseAnswer(){
                            Id = 1,
                            AnswerText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton1Answer")]),
                            Button = new CardAction(){
                                Title = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton1")]),
                                DisplayText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton1")]),
                                Type = ActionTypes.PostBack,
                                Value = JsonConvert.SerializeObject((new ExerciseAnswerValue(){ Excercise = this.Id, Answer = 1 }))
                            },
                            Correct = CorrectAnswer == 1 ? true : false
                        },
                        new ExerciseAnswer(){
                            Id = 2,
                            AnswerText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton2Answer")]),
                            Button = new CardAction(){
                                Title = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton2")]),
                                DisplayText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton2")]),
                                Type = ActionTypes.PostBack,
                                Value = JsonConvert.SerializeObject((new ExerciseAnswerValue(){ Excercise = this.Id, Answer = 2 }))
                            },
                            Correct = CorrectAnswer == 2 ? true : false
                        },
                        new ExerciseAnswer(){
                            Id = 3,
                            AnswerText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton3Answer")]),
                            Button = new CardAction(){
                                Title = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton3")]),
                                DisplayText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton3")]),
                                Type = ActionTypes.PostBack,
                                Value = JsonConvert.SerializeObject((new ExerciseAnswerValue(){ Excercise = this.Id, Answer = 3 }))
                            },
                            Correct = CorrectAnswer == 3 ? true : false
                        }
                    };
                    this.Date = (DateTime) collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldDate")];
                    this.Active = (bool) collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldActive")];  
                }
            }
        }
        await Task.FromResult(this);
    }

    public async Task GetExcerciseByIdAsync(int Id)
    {
        Folder RegularFolder;

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
                RegularFolder = web.GetFolderByServerRelativeUrl(oList.RootFolder.ServerRelativeUrl + "/" + Utils.GetAppSetting("SharePointExerciseRegularFolder"));   
                cli.Load(RegularFolder);
                cli.ExecuteQuery();
            }
            catch
            {
                return;
            }

            if(RegularFolder.Exists)
            {
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
                                    <FieldRef Name='{Utils.GetAppSetting("SharePointExerciseFieldID")}'/>
                                    <Value Type='Integer'>{Id}</Value>
                                </Eq>
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
                            </And>
                        </Where>
                    </Query>
                    <RowLimit>
                        1
                    </RowLimit>
                </view>";

                camlQuery.FolderServerRelativeUrl = RegularFolder.ServerRelativeUrl;

                ListItemCollection collListItem = oList.GetItems(camlQuery);
                    
                cli.Load(collListItem);
                cli.ExecuteQuery();

                if(collListItem.Count > 0)
                {
                    Match match = Regex.Match(
                        Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldCorrectAnswer")]), 
                        @"\d+"
                    );

                    int CorrectAnswer = match.Success ? Int32.Parse(match.Value) : 0;
                    
                    this.Id = Int32.Parse(Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldID")]));
                    this.Title = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldTitle")]);
                    this.Subtitle = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldSubtitle")]);
                    this.Language = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldLanguage")]);
                    this.TaskText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldTaskText")]);
                    this.Answers = new List<ExerciseAnswer>{
                        new ExerciseAnswer(){
                            Id = 1,
                            AnswerText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton1Answer")]),
                            Button = new CardAction(){
                                Title = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton1")]),
                                DisplayText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton1")]),
                                Type = ActionTypes.PostBack,
                                Value = new ExerciseAnswerValue(){
                                    Excercise = this.Id,
                                    Answer = 1
                                }
                            },
                            Correct = CorrectAnswer == 1 ? true : false
                        },
                        new ExerciseAnswer(){
                            Id = 2,
                            AnswerText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton2Answer")]),
                            Button = new CardAction(){
                                Title = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton2")]),
                                DisplayText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton2")]),
                                Type = ActionTypes.PostBack,
                                Value = new ExerciseAnswerValue(){
                                    Excercise = this.Id,
                                    Answer = 2
                                }
                            },
                            Correct = CorrectAnswer == 2 ? true : false
                        },
                        new ExerciseAnswer(){
                            Id = 3,
                            AnswerText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton3Answer")]),
                            Button = new CardAction(){
                                Title = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton3")]),
                                DisplayText = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldButton3")]),
                                Type = ActionTypes.PostBack,
                                Value = new ExerciseAnswerValue(){
                                    Excercise = this.Id,
                                    Answer = 3
                                }
                            },
                            Correct = CorrectAnswer == 3 ? true : false
                        }
                    };
                    this.Date = (DateTime) collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldDate")];
                    this.Active = (bool) collListItem[0][Utils.GetAppSetting("SharePointExerciseFieldActive")];  
                }
            }
        }
        await Task.FromResult(this);
    }

    public async Task<bool> AddAnswerAsync(int answer, User user)
    {
        bool CorrectAnswer = false;

        if(answer == 0)
        {
            return false;
        }

        if(this.Answers[answer-1].Correct)
        {
            CorrectAnswer = true;
        }
        
        string AddAnswerQuery = $@"
            BEGIN
                BEGIN TRANSACTION;

                DECLARE @Exercise INT;
                DECLARE @AnswerInserted BIT;

                SET @AnswerInserted = 0;
                SELECT @Exercise = [Id] FROM [dbo].[Exercises] WHERE [ItemListID] = @ExerciseItemListID;
                
                IF @Exercise IS NULL
                    BEGIN
                        DECLARE @ExerciseTable table (Id INT)
                        INSERT INTO [dbo].[Exercises] ( [ItemListID], [ItemListTitle] ) OUTPUT Inserted.Id into @ExerciseTable VALUES ( @ExerciseItemListID, @ExerciseItemListTitle );
                        SELECT @Exercise = [Id] from @ExerciseTable
                    END

                IF NOT EXISTS (SELECT * FROM [dbo].[Answers] WHERE [Exercise] = @Exercise AND [User] = @User)
                    BEGIN
                        INSERT INTO [dbo].[Answers] ( [User], [Exercise], [Answer], [IsCorrect] ) VALUES ( @User, @Exercise, @Answer, @IsCorrect );
                        SET @AnswerInserted = 1;
                    END

                COMMIT; 

                SELECT 
                    @AnswerInserted as [AnswerInserted]
                ;
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand AddAnswerCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = AddAnswerQuery})
                {
                    AddAnswerCommand.Parameters.Add("@ExerciseItemListID", SqlDbType.Int).Value = this.Id;
                    AddAnswerCommand.Parameters.Add("@ExerciseItemListTitle", SqlDbType.NVarChar, 50).Value = this.Title;
                    AddAnswerCommand.Parameters.Add("@User", SqlDbType.Int).Value = user.Id;
                    AddAnswerCommand.Parameters.Add("@Answer", SqlDbType.SmallInt).Value = answer;
                    AddAnswerCommand.Parameters.Add("@IsCorrect", SqlDbType.Bit).Value = CorrectAnswer;
                    bool result = (bool) await AddAnswerCommand.ExecuteScalarAsync();
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

