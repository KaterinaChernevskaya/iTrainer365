using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security;
using Microsoft.Bot.Builder.Azure;
using Microsoft.SharePoint.Client;

[Serializable]
public class Company
{
    [JsonProperty(PropertyName="id")]
    public int Id { get; set; }
    [JsonProperty(PropertyName="extid")]
    public int ExtId { get; set; }
    [JsonProperty(PropertyName="title")]
    public string Title { get; set; }
    [JsonProperty(PropertyName="code")]
    public string Code { get; set; }
    [JsonProperty(PropertyName="tenant_id")]
    public string TenantId { get; set; }
    [JsonProperty(PropertyName="active")]
    public bool Active { get; set; }
    [JsonProperty(PropertyName="created_at")]
    public DateTime CreatedAt { get; set; }
    private string SharePointSiteUrl { get; set; }
    private string SharePointUserName { get; set; }
    private string SharePointUserPassword { get; set; }
    private string SharePointExerciseListTitle { get; set; }
    private string SharePointCompanyListTitle { get; set; }

    public Company()
    {
        this.SharePointSiteUrl = Utils.GetAppSetting("SharePointSiteUrl");
        this.SharePointUserName = Utils.GetAppSetting("SharePointUserName");
        this.SharePointUserPassword = Utils.GetAppSetting("SharePointUserPassword");
        this.SharePointExerciseListTitle = Utils.GetAppSetting("SharePointExerciseListTitle");
        this.SharePointCompanyListTitle = Utils.GetAppSetting("SharePointCompanyListTitle");
    }

    public async Task GetOrCreateCompanyByCodeAsync(string CompanyCode)
    {
        this.Code = CompanyCode;
        await this.GetCompanyByCodeAsync();
        if(this.ExtId > 0)
        {
            await this.GetOrAddAdditionalCompanyDataByListIdAsync();
        }
    }

    public async Task GetCompanyByIdAsync(int Id)
    {
        this.Id = Id;
        await this.GetCompanyDataByIdAsync();
    }

    private async Task GetCompanyByCodeAsync()
    {
        SecureString securePwd = new SecureString();
        foreach (char ch in this.SharePointUserPassword)
            securePwd.AppendChar(ch);

        using (ClientContext cli = new ClientContext(this.SharePointSiteUrl))
        {
            cli.Credentials = new SharePointOnlineCredentials(this.SharePointUserName, securePwd);
            Web web = cli.Web;

            List oList = web.Lists.GetByTitle(this.SharePointCompanyListTitle);

            CamlQuery camlQuery = new CamlQuery();
            camlQuery.ViewXml = $@"
            <View>
                <Query>
                    <ViewFields>
                        <FieldRef Name='{Utils.GetAppSetting("SharePointCompanyFieldID")}' />
                    </ViewFields>
                    <Where>
                        <And>
                            <Eq>
                                <FieldRef Name='{Utils.GetAppSetting("SharePointCompanyFieldCode")}'/>
                                <Value Type='Text'>{this.Code}</Value>
                            </Eq>
                            <Eq>
                                <FieldRef Name='{Utils.GetAppSetting("SharePointCompanyFieldActive")}'/>
                                <Value Type='Boolean'>1</Value>
                            </Eq>
                        </And>
                    </Where>
                </Query>
                <RowLimit>
                    1
                </RowLimit>
            </view>";

            ListItemCollection collListItem = oList.GetItems(camlQuery);
                    
            cli.Load(collListItem);
            cli.ExecuteQuery();

            if(collListItem.Count > 0)
            {
                this.ExtId = Int32.Parse(Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointCompanyFieldID")]));
                this.Title = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointCompanyFieldTitle")]);
                this.Code = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointCompanyFieldCode")]);
                this.TenantId = Convert.ToString(collListItem[0][Utils.GetAppSetting("SharePointCompanyFieldTenantId")]);
                this.Active = (bool) collListItem[0][Utils.GetAppSetting("SharePointCompanyFieldActive")];
            }
        }
        await Task.FromResult(this);
    }

    private async Task GetOrAddAdditionalCompanyDataByListIdAsync()
    {
        string GetOrAddAdditionalCompanyDataByListIdQuery = $@"
            BEGIN
                BEGIN TRANSACTION;

                DECLARE @Company INT;

                SELECT @Company = [Id] FROM [dbo].[Companies] WHERE [ItemListID] = @CompanyItemListID;

                IF @Company IS NULL
                    BEGIN
                        DECLARE @CompanyTable table (Id INT);
                        INSERT INTO [dbo].[Companies] ( [ItemListID], [ItemListTitle], [ItemListTenantId] ) OUTPUT Inserted.Id into @CompanyTable VALUES ( @CompanyItemListID, @CompanyItemListTitle, @CompanyItemListTenantId );
                        SELECT @Company = [Id] FROM @CompanyTable
                    END
                ELSE
                    BEGIN
                        UPDATE [dbo].[Companies] SET [ItemListTitle] = @CompanyItemListTitle, [ItemListTenantId] = @CompanyItemListTenantId WHERE [Id] = @Company;
                    END

                COMMIT;

                SELECT * FROM [dbo].[Companies] WHERE [Id] = @Company;
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand GetOrAddAdditionalCompanyDataByListIdCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = GetOrAddAdditionalCompanyDataByListIdQuery})
                {
                    GetOrAddAdditionalCompanyDataByListIdCommand.Parameters.Add("@CompanyItemListID", SqlDbType.Int).Value = this.ExtId;
                    GetOrAddAdditionalCompanyDataByListIdCommand.Parameters.Add("@CompanyItemListTitle", SqlDbType.NVarChar, 50).Value = (object)this.Title ?? DBNull.Value;
                    GetOrAddAdditionalCompanyDataByListIdCommand.Parameters.Add("@CompanyItemListTenantId", SqlDbType.NVarChar, 50).Value = (object)this.TenantId ?? DBNull.Value;
                    using (SqlDataReader GetOrAddAdditionalCompanyDataByListIdConnectionReader = await GetOrAddAdditionalCompanyDataByListIdCommand.ExecuteReaderAsync())
                    {
                        if(GetOrAddAdditionalCompanyDataByListIdConnectionReader.Read())
                        {
                            this.Id = GetOrAddAdditionalCompanyDataByListIdConnectionReader.GetInt32(GetOrAddAdditionalCompanyDataByListIdConnectionReader.GetOrdinal("Id"));
                            this.CreatedAt = GetOrAddAdditionalCompanyDataByListIdConnectionReader.GetDateTime(GetOrAddAdditionalCompanyDataByListIdConnectionReader.GetOrdinal("CreatedAt"));                  
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

    private async Task GetCompanyDataByIdAsync()
    {
        string GetCompanyDataAsyncQuery = $@"
            BEGIN
                SELECT * FROM [dbo].[Companies] WHERE [Id] = @Id;
            END
        ";

        string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(SQLConnectionString))
        {
            conn.Open();
            try
            {
                using (SqlCommand GetCompanyDataAsyncCommand = new SqlCommand{
                    CommandType = CommandType.Text,
                    Connection = conn,
                    CommandTimeout = 300,
                    CommandText = GetCompanyDataAsyncQuery})
                {
                    GetCompanyDataAsyncCommand.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
                    using (SqlDataReader GetCompanyDataAsyncConnectionReader = await GetCompanyDataAsyncCommand.ExecuteReaderAsync())
                    {
                        if(GetCompanyDataAsyncConnectionReader.Read())
                        {
                            this.Id = !GetCompanyDataAsyncConnectionReader.IsDBNull(GetCompanyDataAsyncConnectionReader.GetOrdinal("Id")) ? GetCompanyDataAsyncConnectionReader.GetInt32(GetCompanyDataAsyncConnectionReader.GetOrdinal("Id")) : 0;
                            this.ExtId = !GetCompanyDataAsyncConnectionReader.IsDBNull(GetCompanyDataAsyncConnectionReader.GetOrdinal("ItemListID")) ? GetCompanyDataAsyncConnectionReader.GetInt32(GetCompanyDataAsyncConnectionReader.GetOrdinal("ItemListID")) : 0;
                            this.Title = !GetCompanyDataAsyncConnectionReader.IsDBNull(GetCompanyDataAsyncConnectionReader.GetOrdinal("ItemListTitle")) ? GetCompanyDataAsyncConnectionReader.GetString(GetCompanyDataAsyncConnectionReader.GetOrdinal("ItemListTitle")) : "";
                            this.TenantId = !GetCompanyDataAsyncConnectionReader.IsDBNull(GetCompanyDataAsyncConnectionReader.GetOrdinal("ItemListTenantId")) ? GetCompanyDataAsyncConnectionReader.GetString(GetCompanyDataAsyncConnectionReader.GetOrdinal("ItemListTenantId")) : "";
                            this.CreatedAt = !GetCompanyDataAsyncConnectionReader.IsDBNull(GetCompanyDataAsyncConnectionReader.GetOrdinal("CreatedAt")) ? GetCompanyDataAsyncConnectionReader.GetDateTime(GetCompanyDataAsyncConnectionReader.GetOrdinal("CreatedAt")) : DateTime.Now;                        
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
}