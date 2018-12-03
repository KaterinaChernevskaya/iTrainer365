#load "..\Models\Settings.csx"

using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams.Models;
using Newtonsoft.Json;

[Serializable]
public class User
{
    [JsonProperty(PropertyName="id")]
    public int Id { get; set; }
    [JsonProperty(PropertyName="channel_id")]
    public string ChannelId { get; set; }
    [JsonProperty(PropertyName="channel_account_id")]
    public string ChannelAccountId { get; set; }
    [JsonProperty(PropertyName="channel_account_name")]
    public string ChannelAccountName { get; set; }
    [JsonProperty(PropertyName="custom_name")]
    public string CustomName { get; set; }
    [JsonProperty(PropertyName="channel_data")]
    public Dictionary<string, string> ChannelData = new Dictionary<string, string>();
    [JsonProperty(PropertyName="company")]
    public Company Company { get; set; }
    [JsonProperty(PropertyName="timezone")]
    public string Timezone { get; set; }
    [JsonProperty(PropertyName="created_at")]
    public DateTime CreatedAt { get; set; }

    public async Task GetOrCreateUserByActivityAsync(Activity activity)
    {
        this.ChannelId = activity.ChannelId;
        this.ChannelAccountId = activity.From.Id;
        this.ChannelAccountName = activity.From.Name;
 
        switch (activity.ChannelId)
        {
            case "msteams":
                TeamsChannelData teamsChannelData = activity.GetChannelData<TeamsChannelData>();
                this.ChannelData.Add("TenantId", teamsChannelData.Tenant.Id);
                break;
            default:
                break;
        }

        await this.GetOrAddAdditionalUserDataAsync();
    }

    public async Task GetUserByIdAsync(int Id)
    {
        this.Id = Id;
        await this.GetUserDataAsync();
    }

    public async Task SetUserSettingsAsync(Settings settings)
    {
        this.Timezone = settings.Timezone;
        await SetUserSettingsAsync();
    }

    public async Task SetUserCustomNameAsync(string CustomName)
    {
        this.CustomName = CustomName;
        await SetUserCustomNameAsync();
    }

    public async Task SetUserCompanyAsync(Company company)
    {
        this.Company = company;
        await CreateCompanyConnectionAsync(company);
    }

    public async Task SubscribeAsync(string Conversation, string ServiceUrl)
    {
        await CreateSubscriptionAsync(Conversation, ServiceUrl);
    }

    public async Task UnsubscribeAsync(string Conversation, string ServiceUrl)
    {
        await RemoveSubscriptionAsync(Conversation, ServiceUrl);
    }

    public async Task SetSubscriptionTimeAsync(string Conversation, string ServiceUrl, string Hour)
    {
        await UpdateSubscriptionTimeAsync(Conversation, ServiceUrl, Hour);
    }

    private async Task GetOrAddAdditionalUserDataAsync()
    {
        string ChannelDataQuery = "";
        
        foreach( KeyValuePair<string, string> ChannelDataItem in this.ChannelData )
        {
            ChannelDataQuery += $@"
                IF NOT EXISTS (SELECT * FROM [dbo].[ChannelData] WHERE [ChannelDataKey] = '{ChannelDataItem.Key}' AND [User] = @Id)
                    BEGIN
                        INSERT INTO [dbo].[ChannelData] ( [User], [ChannelDataKey], [ChannelDataValue] ) VALUES ( @Id, '{ChannelDataItem.Key}', @{ChannelDataItem.Key} );
                    END
            ";
        }
        
        string AdditionalUserDataConnectionQuery = $@"
            BEGIN
                BEGIN TRANSACTION;
                    IF NOT EXISTS (SELECT [Id] FROM [dbo].[Users] WHERE [ChannelId] = @ChannelId AND [ChannelAccountId] = @ChannelAccountId)
                        BEGIN
                            DECLARE @Id INT;
                            DECLARE @UserTable table (Id INT);
                            INSERT INTO [dbo].[Users] ( [ChannelId], [ChannelAccountId], [ChannelAccountName] ) OUTPUT Inserted.Id INTO @UserTable VALUES ( @ChannelId, @ChannelAccountId, @ChannelAccountName );
                            SELECT @Id = [Id] FROM @UserTable;
                        END
                    ELSE
                        BEGIN
                            SELECT @Id = [Id] FROM [dbo].[Users] WHERE [ChannelId] = @ChannelId AND [ChannelAccountId] = @ChannelAccountId;
                        END
                    {ChannelDataQuery}
                COMMIT;
                SELECT * FROM [dbo].[Users] WHERE [Id] = @Id;
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand AdditionalUserDataConnectionCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = AdditionalUserDataConnectionQuery})
                {
                    AdditionalUserDataConnectionCommand.Parameters.Add("@ChannelId", SqlDbType.NVarChar, 50).Value = this.ChannelId;
                    AdditionalUserDataConnectionCommand.Parameters.Add("@ChannelAccountId", SqlDbType.NVarChar, 255).Value = this.ChannelAccountId;
                    AdditionalUserDataConnectionCommand.Parameters.Add("@ChannelAccountName", SqlDbType.NVarChar, 50).Value = (object)this.ChannelAccountName ?? DBNull.Value;
                    foreach( KeyValuePair<string, string> ChannelDataItem in this.ChannelData )
                    {
                        AdditionalUserDataConnectionCommand.Parameters.Add("@" + ChannelDataItem.Key, SqlDbType.NVarChar, 255).Value = ChannelDataItem.Value;
                    }
                    using (SqlDataReader AdditionalUserDataConnectionReader = await AdditionalUserDataConnectionCommand.ExecuteReaderAsync())
                    {
                        if(AdditionalUserDataConnectionReader.Read())
                        {
                            this.Id = AdditionalUserDataConnectionReader.GetInt32(AdditionalUserDataConnectionReader.GetOrdinal("Id"));
                            int Company = !AdditionalUserDataConnectionReader.IsDBNull(AdditionalUserDataConnectionReader.GetOrdinal("Company")) ? AdditionalUserDataConnectionReader.GetInt32(AdditionalUserDataConnectionReader.GetOrdinal("Company")) : 0;
                            if(Company > 0)
                            {
                                Company UserCompany = new Company();
                                await UserCompany.GetCompanyByIdAsync(Company);
                                this.Company = UserCompany;
                            }
                            this.CustomName = !AdditionalUserDataConnectionReader.IsDBNull(AdditionalUserDataConnectionReader.GetOrdinal("CustomName")) ? AdditionalUserDataConnectionReader.GetString(AdditionalUserDataConnectionReader.GetOrdinal("CustomName")) : "";
                            this.Timezone = !AdditionalUserDataConnectionReader.IsDBNull(AdditionalUserDataConnectionReader.GetOrdinal("Timezone")) ? AdditionalUserDataConnectionReader.GetString(AdditionalUserDataConnectionReader.GetOrdinal("Timezone")) : Utils.GetAppSetting("DefaultTimezone");
                            this.CreatedAt = !AdditionalUserDataConnectionReader.IsDBNull(AdditionalUserDataConnectionReader.GetOrdinal("CreatedAt")) ? AdditionalUserDataConnectionReader.GetDateTime(AdditionalUserDataConnectionReader.GetOrdinal("CreatedAt")) : DateTime.Now;                      
                        }
                    }
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            conn.Close();
        }
    }

    private async Task GetUserDataAsync()
    {
        string UserDataConnectionQuery = $@"
            BEGIN
                SELECT * FROM [dbo].[Users] WHERE [Id] = @Id;
            END
        ";

        string ChannelDataConnectionQuery = $@"
            BEGIN
                SELECT * FROM [dbo].[ChannelData] WHERE [User] = @User;
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand UserDataConnectionCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = UserDataConnectionQuery})
                {
                    UserDataConnectionCommand.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
                    using (SqlDataReader UserDataConnectionReader = await UserDataConnectionCommand.ExecuteReaderAsync())
                    {
                        if(UserDataConnectionReader.Read())
                        {
                            this.Id = UserDataConnectionReader.GetInt32(UserDataConnectionReader.GetOrdinal("Id"));
                            this.ChannelId = !UserDataConnectionReader.IsDBNull(UserDataConnectionReader.GetOrdinal("ChannelId")) ? UserDataConnectionReader.GetString(UserDataConnectionReader.GetOrdinal("ChannelId")) : "";
                            this.ChannelAccountId = !UserDataConnectionReader.IsDBNull(UserDataConnectionReader.GetOrdinal("ChannelAccountId")) ? UserDataConnectionReader.GetString(UserDataConnectionReader.GetOrdinal("ChannelAccountId")) : "";
                            this.ChannelAccountName = !UserDataConnectionReader.IsDBNull(UserDataConnectionReader.GetOrdinal("ChannelAccountName")) ? UserDataConnectionReader.GetString(UserDataConnectionReader.GetOrdinal("ChannelAccountName")) : "";
                            this.CustomName = !UserDataConnectionReader.IsDBNull(UserDataConnectionReader.GetOrdinal("CustomName")) ? UserDataConnectionReader.GetString(UserDataConnectionReader.GetOrdinal("CustomName")) : "";
                            int Company = !UserDataConnectionReader.IsDBNull(UserDataConnectionReader.GetOrdinal("Company")) ? UserDataConnectionReader.GetInt32(UserDataConnectionReader.GetOrdinal("Company")) : 0;
                            if(Company > 0)
                            {
                                Company UserCompany = new Company();
                                await UserCompany.GetCompanyByIdAsync(Company);
                                this.Company = UserCompany;
                            }
                            this.Timezone = !UserDataConnectionReader.IsDBNull(UserDataConnectionReader.GetOrdinal("Timezone")) ? UserDataConnectionReader.GetString(UserDataConnectionReader.GetOrdinal("Timezone")) : Utils.GetAppSetting("DefaultTimezone");
                            this.CreatedAt = !UserDataConnectionReader.IsDBNull(UserDataConnectionReader.GetOrdinal("CreatedAt")) ? UserDataConnectionReader.GetDateTime(UserDataConnectionReader.GetOrdinal("CreatedAt")) : DateTime.Now;                      
                        }
                    }
                }
                using (SqlCommand ChannelDataConnectionCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = ChannelDataConnectionQuery})
                {
                    ChannelDataConnectionCommand.Parameters.Add("@User", SqlDbType.Int).Value = this.Id;
                    using (SqlDataReader ChannelDataConnectionReader = await ChannelDataConnectionCommand.ExecuteReaderAsync())
                    {
                        while(ChannelDataConnectionReader.Read())
                        {
                            this.ChannelData.Add(ChannelDataConnectionReader.GetString(ChannelDataConnectionReader.GetOrdinal("ChannelDataKey")), ChannelDataConnectionReader.GetString(ChannelDataConnectionReader.GetOrdinal("ChannelDataValue")));                      
                        }
                    }
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            conn.Close();
        }
    }

    private async Task CreateCompanyConnectionAsync(Company company)
    {
        string CreateCompanyConnectionQuery = $@"
            BEGIN
                BEGIN TRANSACTION;
                    IF @Company IS NOT NULL
                        BEGIN
                            UPDATE [dbo].[Users] SET [Company] = @Company WHERE [Id] = @Id;
                        END
                COMMIT; 
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand CreateCompanyConnectionCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = CreateCompanyConnectionQuery})
                {
                    CreateCompanyConnectionCommand.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
                    CreateCompanyConnectionCommand.Parameters.Add("@Company", SqlDbType.Int).Value = company.Id;
                    await CreateCompanyConnectionCommand.ExecuteScalarAsync();
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            conn.Close();
        }
    }

    private async Task SetUserCustomNameAsync()
    {
        string SetUserCustomNameConnectionQuery = $@"
            BEGIN
                BEGIN TRANSACTION;
                    IF @CustomName IS NOT NULL
                        BEGIN
                            UPDATE [dbo].[Users] SET [CustomName] = @CustomName WHERE [Id] = @Id;
                        END
                COMMIT; 
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand SetUserCustomNameConnectionCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = SetUserCustomNameConnectionQuery})
                {
                    SetUserCustomNameConnectionCommand.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
                    SetUserCustomNameConnectionCommand.Parameters.Add("@CustomName", SqlDbType.NVarChar, 50).Value = (object)this.CustomName ?? DBNull.Value;
                    await SetUserCustomNameConnectionCommand.ExecuteScalarAsync();
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            conn.Close();
        }
    }

    private async Task SetUserSettingsAsync()
    {
        string SetUserSettingsConnectionQuery = $@"
            BEGIN
                BEGIN TRANSACTION;
                    IF @Timezone IS NOT NULL
                        BEGIN
                            UPDATE [dbo].[Users] SET [Timezone] = @Timezone WHERE [Id] = @Id;
                        END
                COMMIT; 
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand SetUserSettingsConnectionCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = SetUserSettingsConnectionQuery})
                {
                    SetUserSettingsConnectionCommand.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
                    SetUserSettingsConnectionCommand.Parameters.Add("@Timezone", SqlDbType.Int).Value = (object)this.Timezone ?? DBNull.Value;
                    await SetUserSettingsConnectionCommand.ExecuteScalarAsync();
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            conn.Close();
        }
    }

    private async Task CreateSubscriptionAsync(string Conversation, string ServiceUrl)
    {
        string CreateSubscriptionConnectionQuery = $@"
            BEGIN
                BEGIN TRANSACTION;
                    IF NOT EXISTS (SELECT [Id] FROM [dbo].[Subscription] WHERE [User] = @Id AND [Conversation] = @Conversation AND [ServiceUrl] = @ServiceUrl AND [Locale] = @Locale)
                        BEGIN
                            INSERT INTO [dbo].[Subscription] ( [User], [Conversation], [ServiceUrl], [Locale] ) VALUES ( @Id, @Conversation, @ServiceUrl, @Locale );
                        END
                COMMIT;
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand CreateSubscriptionConnectionCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = CreateSubscriptionConnectionQuery})
                {
                    CreateSubscriptionConnectionCommand.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
                    CreateSubscriptionConnectionCommand.Parameters.Add("@Conversation", SqlDbType.NVarChar, 255).Value = (object)Conversation ?? DBNull.Value;
                    CreateSubscriptionConnectionCommand.Parameters.Add("@ServiceUrl", SqlDbType.NVarChar, 255).Value = (object)ServiceUrl ?? DBNull.Value;
                    CreateSubscriptionConnectionCommand.Parameters.Add("@Locale", SqlDbType.NVarChar, 255).Value = (object)Utils.GetAppSetting("Locale") ?? DBNull.Value;
                    await CreateSubscriptionConnectionCommand.ExecuteScalarAsync();
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            conn.Close();
        }
    }

    private async Task RemoveSubscriptionAsync(string Conversation, string ServiceUrl)
    {
        string RemoveSubscriptionConnectionQuery = $@"
            BEGIN
                IF EXISTS (SELECT [Id] FROM [dbo].[Subscription] WHERE [User] = @Id AND [Conversation] = @Conversation AND [ServiceUrl] = @ServiceUrl AND [Locale] = @Locale)
                    BEGIN
                        BEGIN TRANSACTION;
                            DELETE FROM [dbo].[Subscription] WHERE [User] = @Id AND [Conversation] = @Conversation AND [ServiceUrl] = @ServiceUrl AND [Locale] = @Locale;
                        COMMIT;
                    END
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand RemoveSubscriptionConnectionCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = RemoveSubscriptionConnectionQuery})
                {
                    RemoveSubscriptionConnectionCommand.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
                    RemoveSubscriptionConnectionCommand.Parameters.Add("@Conversation", SqlDbType.NVarChar, 255).Value = (object)Conversation ?? DBNull.Value;
                    RemoveSubscriptionConnectionCommand.Parameters.Add("@ServiceUrl", SqlDbType.NVarChar, 255).Value = (object)ServiceUrl ?? DBNull.Value;
                    RemoveSubscriptionConnectionCommand.Parameters.Add("@Locale", SqlDbType.NVarChar, 255).Value = (object)Utils.GetAppSetting("Locale") ?? DBNull.Value;
                    await RemoveSubscriptionConnectionCommand.ExecuteScalarAsync();
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            conn.Close();
        }
    }

    private async Task UpdateSubscriptionTimeAsync(string Conversation, string ServiceUrl, string Hour)
    {
        string UpdateSubscriptionTimeConnectionQuery = $@"
            BEGIN
                IF EXISTS (SELECT [Id] FROM [dbo].[Subscription] WHERE [User] = @Id AND [Conversation] = @Conversation AND [ServiceUrl] = @ServiceUrl AND [Locale] = @Locale)
                    BEGIN
                        BEGIN TRANSACTION;
                            UPDATE [dbo].[Subscription] SET [Hour] = @Hour WHERE [User] = @Id AND [Conversation] = @Conversation AND [ServiceUrl] = @ServiceUrl AND [Locale] = @Locale;
                        COMMIT;
                    END
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand UpdateSubscriptionTimeConnectionCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = UpdateSubscriptionTimeConnectionQuery})
                {
                    UpdateSubscriptionTimeConnectionCommand.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
                    UpdateSubscriptionTimeConnectionCommand.Parameters.Add("@Conversation", SqlDbType.NVarChar, 255).Value = (object)Conversation ?? DBNull.Value;
                    UpdateSubscriptionTimeConnectionCommand.Parameters.Add("@ServiceUrl", SqlDbType.NVarChar, 255).Value = (object)ServiceUrl ?? DBNull.Value;
                    UpdateSubscriptionTimeConnectionCommand.Parameters.Add("@Locale", SqlDbType.NVarChar, 255).Value = (object)Utils.GetAppSetting("Locale") ?? DBNull.Value;
                    UpdateSubscriptionTimeConnectionCommand.Parameters.Add("@Hour", SqlDbType.Int).Value = (object)Hour ?? DBNull.Value;
                    await UpdateSubscriptionTimeConnectionCommand.ExecuteScalarAsync();
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                throw new Exception(ex.Message);
            }
            conn.Close();
        }
    }

    public async Task<Tuple<bool, int, int, int, int>> GetProgressAsync()
    {
        string CurrentProgressQuery = @"
            BEGIN
                DECLARE @DaysInEducation INT;
                DECLARE @AllAnswers INT;
                DECLARE @CorrectAnswers INT;
                DECLARE @SuccessRateAnswers INT;
                
                SELECT @DaysInEducation = DATEDIFF(DAY, [dbo].[Users].[CreatedAt], getdate()) FROM [dbo].[Users] WHERE [dbo].[Users].[Id] = @User;
                SELECT @AllAnswers = COUNT(*) FROM [dbo].[Answers] WHERE [dbo].[Answers].[User] = @User;
                SELECT @CorrectAnswers = COUNT(*) FROM [dbo].[Answers] WHERE [dbo].[Answers].[User] = @User AND [dbo].[Answers].[IsCorrect] = @IsCorrect;
                
                IF @DaysInEducation IS NULL
                    SET @DaysInEducation = 0;
                IF @AllAnswers > 0
                    SET @SuccessRateAnswers = @CorrectAnswers*100 / @AllAnswers;
                ELSE
                    SET @SuccessRateAnswers = 0;

                SELECT 
                    @DaysInEducation as [DaysInEducation], 
                    @AllAnswers as [AllAnswers], 
                    @CorrectAnswers as [CorrectAnswers], 
                    @SuccessRateAnswers as [SuccessRateAnswers]
                ;
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand CurrentProgressCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = CurrentProgressQuery})
                {
                    CurrentProgressCommand.Parameters.Add("@User", SqlDbType.Int).Value = this.Id;
                    CurrentProgressCommand.Parameters.Add("@IsCorrect", SqlDbType.Bit).Value = true;

                    using (SqlDataReader CurrentProgressReader = await CurrentProgressCommand.ExecuteReaderAsync())
                    {
                        if(CurrentProgressReader.Read())
                        {
                            int DaysInEducation = CurrentProgressReader.GetInt32(CurrentProgressReader.GetOrdinal("DaysInEducation"));
                            int AllAnswers = CurrentProgressReader.GetInt32(CurrentProgressReader.GetOrdinal("AllAnswers"));
                            int CorrectAnswers = CurrentProgressReader.GetInt32(CurrentProgressReader.GetOrdinal("CorrectAnswers"));
                            int SuccessRateAnswers = CurrentProgressReader.GetInt32(CurrentProgressReader.GetOrdinal("SuccessRateAnswers"));
                            return new Tuple<bool, int, int, int, int>(true, DaysInEducation, AllAnswers, CorrectAnswers, SuccessRateAnswers);                        
                        }
                        else
                        {
                            return new Tuple<bool, int, int, int, int>(false, 0, 0, 0, 0);
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
}