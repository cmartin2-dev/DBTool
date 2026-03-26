using DBTool.Commons;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2016.Excel;
using Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Chat;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBTool.Connect
{

    public class RequestQuery
    {

        private readonly int POLL_DELAY = 5;
        private readonly int MAX_POLL_RETRY = 10;
        public Region SelectedRegion { get; set; }

        public HeaderEnvironment headerEnvironment { get; set; }
        public string selectedTenant { get; set; }
        public CancellationTokenSource sourceToken { get; set; }
        public CancellationToken token { get; set; }


        private RestClientOptions _restClientOptions = null;
        private RestClient _restClient = null;

        public string Query { get; set; }
        string jsonObj = string.Empty;

        public void SetDetails(Region region, HeaderEnvironment tenant)
        {
            this.SelectedRegion = region;
            this.headerEnvironment = tenant;
            _restClientOptions = new RestClientOptions();
            _restClientOptions.MaxTimeout = 300000; // 5 minutes

            string username = this.headerEnvironment.ClientId;
            string password = this.headerEnvironment.ClientSecret;

            _restClientOptions.Authenticator = OAuth1Authenticator.ForRequestToken(username, password);

            _restClient = new RestClient(_restClientOptions);
        }

        public void SetDetails(RegionTenant regionTenant)
        {
            this.SelectedRegion = regionTenant.Region;
            this.headerEnvironment = StaticFunctions.GetHeaderEnvironment(this.SelectedRegion.HeaderEnvironmentId);
            this.selectedTenant = regionTenant.tenantId;

            _restClientOptions = new RestClientOptions();
            _restClientOptions.MaxTimeout = 300000; // 5 minutes

            string username = this.headerEnvironment.ClientId;
            string password = this.headerEnvironment.ClientSecret;

            _restClientOptions.Authenticator = OAuth1Authenticator.ForRequestToken(username, password);

            _restClient = new RestClient(_restClientOptions);
        }

        public async Task<RequestResponse> GetRequestEnvironment()
        {

            RequestResponse requestResponse = new RequestResponse();

            List<RegionTenant> regionTenants = null;
            try
            {
                if (SelectedRegion != null)
                {

                    var request = new RestRequest(SelectedRegion.RegionEndPoint);
                    request.Method = Method.Get;

                    if (this.headerEnvironment.TenantName.ToLower() == "local")
                    {
                        foreach (var item in this.headerEnvironment.Headers)
                        {
                            request.AddHeader(item.Key, item.Value);
                        }
                    }
                    //old
                    //client.Authenticator = OAuth1Authenticator.ForRequestToken(headerEnvironment.ClientId, headerEnvironment.ClientSecret);
                    //Task<IRestResponse> t = client.ExecuteAsync(request, token);

                    Task<RestResponse> t = _restClient.ExecuteAsync(request, token);

                    var response = await t;

                    if (response.IsSuccessful)
                    {

                        regionTenants = JsonConvert.DeserializeObject<List<RegionTenant>>(response.Content);

                        requestResponse.TenantList = regionTenants;
                        requestResponse.isSuccess = true;
                    }
                    else
                    {
                        requestResponse.isSuccess = false;
                        requestResponse.ErrorMessage = response.Content;
                    }

                }
                else
                {
                    requestResponse.isSuccess = false;
                    requestResponse.ErrorMessage = "No region selected";
                }

                return requestResponse;
            }
            catch (Exception ex)
            {

                requestResponse.isSuccess = false;
                requestResponse.ErrorMessage = ex.Message;
                return requestResponse;
            }
        }

        public async Task<RequestResponse> GetRequestQuery()
        {

            RequestResponse requestResponse = new RequestResponse();

            try
            {
                if (SelectedRegion != null)
                {
                    // await Oauth2Settings();

                    var request = new RestRequest(this.headerEnvironment.EndPoint);
                    request.Method = Method.Post;

                    //if (this.headerEnvironment.TenantName.ToLower() == "local")
                    //{
                    //    foreach (var item in this.headerEnvironment.Headers)
                    //    {
                    //        request.AddHeader(item.Key, item.Value);
                    //    }
                    //}



                    foreach (var item in this.headerEnvironment.Headers)
                    {
                        if (item.Key == "X-Infor-TenantId")
                        {
                            if (this.headerEnvironment.isOAuth1 != null && this.headerEnvironment.isOAuth1)
                            {
                                request.AddHeader(item.Key, selectedTenant);
                            }
                            else
                            {
                                HeaderEnvironment newHeader = StaticFunctions.GetHeaderEnvironment(selectedTenant);
                                if (this.headerEnvironment.TenantName.ToLower() == "local")
                                {
                                    request.AddHeader(item.Key, selectedTenant);
                                }
                                else
                                    request.AddHeader(item.Key, item.Value);
                            }
                        }

                        else
                            request.AddHeader(item.Key, item.Value);
                    }



                    string scriptSeperator = "GO";
                    var commands = new List<string>();

                    Regex regex = new Regex("^\\s*" + scriptSeperator + "\\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                    if (regex.IsMatch(Query))
                    {
                        string[] lines = regex.Split(Query);
                        foreach (string line in lines)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                commands.Add(line);
                            }
                        }
                    }


                    if (!StaticFunctions.AppConnection.settingsObject.CheckAccess)
                    {
                        Query = $"BEGIN TRAN {Query} ROLLBACK TRAN";
                    }

                    var aaa = Encoding.UTF8.GetBytes(Query);
                    var bbb = Convert.ToBase64String(aaa);

                    string newString = @"{IsProvision: " + false.ToString().ToLower() + ", ActionType:'SELECT',Query:'" + bbb + "'}";
                    var values = JsonConvert.DeserializeObject<RequestCustomQuery>(newString);
                    request.AddJsonBody(values);


                    Task<RestResponse> t = _restClient.ExecuteAsync(request, token);

                    var response = await t;

                    if (response.IsSuccessful)
                    {

                        string responseResult = string.Empty;
                        if (headerEnvironment.EndPoint.ToLower().Contains("compressed"))
                        {

                            /// new
                            /// 
                            string repStr = response.Content.Replace("\"", "");
                            byte[] convertedToByte = Convert.FromBase64String(repStr);

                            responseResult = Utilities.Unzip(convertedToByte);

                            var custom = JsonConvert.DeserializeObject<CustObj>(responseResult);

                            //   AddMetaData(custom);
                            requestResponse.CustObj = custom;
                            requestResponse.isSuccess = true;


                        }
                        else
                        {


                            var custom = JsonConvert.DeserializeObject<CustObj>(response.Content);


                            requestResponse.CustObj = custom;
                            requestResponse.isSuccess = true;

                        }

                        //SendEmail();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(response.ErrorMessage) && response.ErrorMessage.ToLower().Contains("cancel"))
                            requestResponse.ErrorMessage = "Cancelled request";
                        else
                            requestResponse.ErrorMessage = response.Content.Replace(this.headerEnvironment.TenantName, selectedTenant);

                        requestResponse.isSuccess = false;

                        return requestResponse;

                    }

                }
                else
                {
                    requestResponse.isSuccess = false;
                    requestResponse.ErrorMessage = "No region selected";
                }

                return requestResponse;
            }
            catch (Exception ex)
            {

                requestResponse.isSuccess = false;
                requestResponse.ErrorMessage = ex.Message;
                return requestResponse;
            }
            finally
            {
                ExecutionLog.LogQuery(Query, selectedTenant, requestResponse.isSuccess ? "Success" : "Failed", requestResponse.ErrorMessage);
            }
        }

        private void AddMetaData(CustObj custObj)
        {
            foreach (var item in custObj.Objects)
            {
                string metadata = string.Empty;
                foreach (var val in item.Object)
                {
                    if (string.IsNullOrEmpty(metadata))
                        metadata = val.Value.ToString();
                    else
                    {
                        if (val.Value != null)
                            metadata += "&con&" + val.Value.ToString();
                    }
                }

                item.Object.Add("metadata", metadata);
            }
        }

        private void SetDataLake()
        {
            _restClientOptions = new RestClientOptions();

            string username = this.headerEnvironment.ClientId;
            string password = this.headerEnvironment.ClientSecret;

            _restClientOptions.Authenticator = OAuth1Authenticator.ForRequestToken(username, password);

            _restClient = new RestClient(_restClientOptions);
        }

        private string GenerateAuthorizationHeader(string method, string url, Dictionary<string, string>? queryParams, string _consumerKey, string _consumerSecret)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var nonce = Guid.NewGuid().ToString("N");

            var parameters = new SortedDictionary<string, string>
        {
            { "oauth_consumer_key", _consumerKey },
            { "oauth_nonce", nonce },
            { "oauth_signature_method", "HMAC-SHA1" },
            { "oauth_timestamp", timestamp },
            { "oauth_version", "1.0" }
        };

            if (queryParams != null)
            {
                foreach (var kvp in queryParams)
                    parameters[kvp.Key] = kvp.Value;
            }

            // Signature base string
            var paramString = string.Join("&", parameters.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var signatureBase = $"{method.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(paramString)}";

            // Sign with HMAC-SHA1
            var signingKey = $"{Uri.EscapeDataString(_consumerSecret)}&"; // no token secret
            string signature;
            using (var hasher = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey)))
            {
                signature = Convert.ToBase64String(hasher.ComputeHash(Encoding.ASCII.GetBytes(signatureBase)));
            }

            // Add signature
            parameters["oauth_signature"] = signature;

            // Build header
            var headerParams = parameters
                .Where(kvp => kvp.Key.StartsWith("oauth_"))
                .Select(kvp => $"{kvp.Key}=\"{Uri.EscapeDataString(kvp.Value)}\"");

            return "OAuth " + string.Join(", ", headerParams);
        }

        private async Task Oauth2Settings(RegionTenant regionTenant)
        {
            this.SelectedRegion = regionTenant.Region;
            this.selectedTenant = regionTenant.tenantId;
            this.headerEnvironment = StaticFunctions.GetHeaderEnvironment(this.selectedTenant);

            string tokenUrl = headerEnvironment.TokenUrl;
            var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = headerEnvironment.ClientId,
                ["client_secret"] = headerEnvironment.ClientSecret,
                ["resource"] = headerEnvironment.Resource,
                ["username"] = headerEnvironment.Username,
                ["password"] = headerEnvironment.Password
            });

            using (var client1 = new HttpClient())
            {
                var res = await client1.SendAsync(req);

                string json = await res.Content.ReadAsStringAsync();


                var tokenObject = JsonConvert.DeserializeObject<TokenAuth>(json);

                headerEnvironment.Token = tokenObject.access_token;

            }

            if (!headerEnvironment.Headers.ContainsKey("Authorization"))
            {
                headerEnvironment.Headers.Add("Authorization", $"Bearer {headerEnvironment.Token}");
            }
            else
            {
                headerEnvironment.Headers.Remove("Authorization");
                headerEnvironment.Headers.Add("Authorization", $"Bearer {headerEnvironment.Token}");
            }
        }

        public async Task<string> TestDataLake(RegionTenant regionTenant)
        {
            string returnString = string.Empty;
            string endpoint1 = $"https://mingle-cqa-ionapi.cqa.inforcloudsuite.com/{regionTenant.tenantId}/DATAFABRIC/compass/v2/jobs";
            try
            {
                await Oauth2Settings(regionTenant);

                var request = new RestRequest(endpoint1);
                request.Method = Method.Post;

                foreach (var item in this.headerEnvironment.Headers)
                {
                    if (item.Key == "X-Infor-TenantId")
                    {
                        if (this.headerEnvironment.isOAuth1 != null && this.headerEnvironment.isOAuth1)
                        {
                            request.AddHeader(item.Key, selectedTenant);
                        }
                        else
                        {
                            if (this.headerEnvironment.TenantName.ToLower() == "local")
                            {
                                request.AddHeader(item.Key, selectedTenant);
                            }
                            else
                                request.AddHeader(item.Key, item.Value);
                        }
                    }

                    else
                    {

                        if (item.Key.ToLower() == "content-type")
                        {
                            request.AddHeader(item.Key, "text/plain");
                        }
                        else
                            request.AddHeader(item.Key, item.Value);
                    }
                }

                var compassJob = await CreateCompassJob(request);
                var compassStatus = await PollCompassStatus(request, compassJob.QueryId, regionTenant.tenantId);
                var compassResult = await CompassResult(request, compassJob.QueryId, regionTenant.tenantId, 0, 1000);

                returnString = compassResult.ToString();

          //      var aaa = JArray.Parse(returnString);

          //      var grouped = aaa
          //.Cast<JObject>()
          //.GroupBy(r => (int)r["STYLEID"])
          //.ToDictionary(g => g.Key, g => g.ToList());


          //      var maxParallel = 1;
          //      var semaphore = new SemaphoreSlim(maxParallel);
          //      var styleTasks = new List<Task>();

          //      foreach (var kvp in grouped)
          //      {
          //          int styleId = kvp.Key;
          //          var styleData = kvp.Value;

          //          await semaphore.WaitAsync();

          //          var task = Task.Run(async () =>
          //          {
          //              try
          //              {
          //                 // await AnalyzeBatch(request, styleId, styleData);
          //              }
          //              catch (Exception ex)
          //              {
          //                  throw;
          //              }
          //              finally
          //              {
          //                  semaphore.Release();
          //              }
          //          });

          //          styleTasks.Add(task);
          //      }

          //      await Task.WhenAll(styleTasks);

            }
            catch (Exception ex)
            {

            }
            return returnString;
        }

        private async Task<CompassJobResponse> CreateCompassJob(RestRequest request)
        {
            RestClient restClient = new RestClient();
            var result = new CompassJobResponse();
            request.AddBody(Query);
            var task = await restClient.ExecuteAsync(request);

            if (task != null && task.IsSuccessful)
            {
                string responseCode = task.Content;

                result = JsonConvert.DeserializeObject<CompassJobResponse>(responseCode);
            }

            return result;
        }

        private async Task<CompassJobResponse> PollCompassStatus(RestRequest request, string queryId, string tenantId)
        {
            request.Method = Method.Get;

            string enpoint = $"https://mingle-cqa-ionapi.cqa.inforcloudsuite.com/{tenantId}/DATAFABRIC/compass/v2/jobs/{queryId}/status";
            request.Resource = enpoint;

            RestClient restClient = new RestClient();

            for (int i = 0; i < MAX_POLL_RETRY; i++)
            {
                try
                {
                    var result = await restClient.ExecuteAsync(request);

                    if (result != null && result.IsSuccessful)
                    {
                        var deserializedResponse = JsonConvert.DeserializeObject<CompassJobResponse>(result.Content);

                        if (deserializedResponse.Status == "FINISHED")
                        {
                            return deserializedResponse;
                        }
                    }

                    await Task.Delay(POLL_DELAY * 1000);
                }
                catch (Exception ex) { throw new Exception(ex.Message); }
            }

            return new CompassJobResponse
            {
                Status = "FAILED"
            };

        }

        private async Task<string> CompassResult(RestRequest request, string queryId, string tenantId, int offset = 0, int batchSize = 1000)
        {
            string result = string.Empty;

            RestClient restClient = new RestClient();
            request.Method = Method.Get;

            string enpoint = $"https://mingle-cqa-ionapi.cqa.inforcloudsuite.com/{tenantId}/DATAFABRIC/compass/v2/jobs/{queryId}/result?offset={offset}&limit={batchSize}";
            request.Resource = enpoint;

            var task = await restClient.ExecuteAsync(request);

            if (task != null && task.IsSuccessful)
            {
                result = task.Content;

            }

            return result;
        }

        public async Task<RequestResponse> GetRequestQueryCustomCS(string connectionString)
        {

            RequestResponse requestResponse = new RequestResponse();

            try
            {
                if (SelectedRegion != null)
                {
                    // await Oauth2Settings();

                    string customConnStrEndPoint = this.headerEnvironment.EndPoint.ToLower().Replace("getdatacompressed", "getcustomdata");

                    var request = new RestRequest(customConnStrEndPoint);
                    request.Method = Method.Post;

                    //if (this.headerEnvironment.TenantName.ToLower() == "local")
                    //{
                    //    foreach (var item in this.headerEnvironment.Headers)
                    //    {
                    //        request.AddHeader(item.Key, item.Value);
                    //    }
                    //}



                    foreach (var item in this.headerEnvironment.Headers)
                    {
                        if (item.Key == "X-Infor-TenantId")
                        {
                            if (this.headerEnvironment.isOAuth1 != null && this.headerEnvironment.isOAuth1)
                            {
                                request.AddHeader(item.Key, selectedTenant);
                            }
                            else
                            {
                                HeaderEnvironment newHeader = StaticFunctions.GetHeaderEnvironment(selectedTenant);
                                if (this.headerEnvironment.TenantName.ToLower() == "local")
                                {
                                    request.AddHeader(item.Key, selectedTenant);
                                }
                                else
                                    request.AddHeader(item.Key, item.Value);
                            }
                        }

                        else
                            request.AddHeader(item.Key, item.Value);
                    }



                    string scriptSeperator = "GO";
                    var commands = new List<string>();

                    Regex regex = new Regex("^\\s*" + scriptSeperator + "\\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                    if (regex.IsMatch(Query))
                    {
                        string[] lines = regex.Split(Query);
                        foreach (string line in lines)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                commands.Add(line);
                            }
                        }
                    }


                    if (!StaticFunctions.AppConnection.settingsObject.CheckAccess)
                    {
                        Query = $"BEGIN TRAN {Query} ROLLBACK TRAN";
                    }

                    var aaa = Encoding.UTF8.GetBytes(Query);
                    var bbb = Convert.ToBase64String(aaa);

                    string newString = @"{IsProvision: " + false.ToString().ToLower() + ", ActionType:'SELECT',Query:'" + bbb + "'}";
                    var values = JsonConvert.DeserializeObject<RequestCustomQuery>(newString);
                    values.ConStr = connectionString;
                    values.DBType = "MSSQL";
                    request.AddJsonBody(values);


                    Task<RestResponse> t = _restClient.ExecuteAsync(request, token);

                    var response = await t;

                    if (response.IsSuccessful)
                    {

                        string responseResult = string.Empty;
                        if (headerEnvironment.EndPoint.ToLower().Contains("compressed"))
                        {

                            /// new
                            /// 
                            string repStr = response.Content.Replace("\"", "");
                            byte[] convertedToByte = Convert.FromBase64String(repStr);

                            responseResult = Utilities.Unzip(convertedToByte);

                            var custom = JsonConvert.DeserializeObject<CustObj>(responseResult);

                            //   AddMetaData(custom);
                            requestResponse.CustObj = custom;
                            requestResponse.isSuccess = true;


                        }
                        else
                        {


                            var custom = JsonConvert.DeserializeObject<CustObj>(response.Content);


                            requestResponse.CustObj = custom;
                            requestResponse.isSuccess = true;

                        }

                        //SendEmail();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(response.ErrorMessage) && response.ErrorMessage.ToLower().Contains("cancel"))
                            requestResponse.ErrorMessage = "Cancelled request";
                        else
                            requestResponse.ErrorMessage = response.Content.Replace(this.headerEnvironment.TenantName, selectedTenant);

                        requestResponse.isSuccess = false;

                        return requestResponse;

                    }

                }
                else
                {
                    requestResponse.isSuccess = false;
                    requestResponse.ErrorMessage = "No region selected";
                }

                return requestResponse;
            }
            catch (Exception ex)
            {

                requestResponse.isSuccess = false;
                requestResponse.ErrorMessage = ex.Message;
                return requestResponse;
            }
        }

        public async Task<RequestResponse> GetRequestQueryDataset()
        {

            RequestResponse requestResponse = new RequestResponse();

            try
            {
                if (SelectedRegion != null)
                {

                    //  await Oauth2Settings();

                    var request = new RestRequest(this.headerEnvironment.EndPoint);
                    request.Method = Method.Post;

                    if (this.headerEnvironment.TenantName.ToLower() == "local")
                    {
                        foreach (var item in this.headerEnvironment.Headers)
                        {
                            request.AddHeader(item.Key, item.Value);
                        }
                    }


                    foreach (var item in this.headerEnvironment.Headers)
                    {
                        if (item.Key == "X-Infor-TenantId")
                        {
                            if (this.headerEnvironment.isOAuth1 != null && this.headerEnvironment.isOAuth1)
                            {
                                request.AddHeader(item.Key, selectedTenant);
                            }
                            else
                            {
                                if (this.headerEnvironment.TenantName.ToLower() == "local")
                                {
                                    request.AddHeader(item.Key, selectedTenant);
                                }
                                else
                                    request.AddHeader(item.Key, item.Value);
                            }
                        }

                        else
                            request.AddHeader(item.Key, item.Value);
                    }



                    string scriptSeperator = "GO";
                    var commands = new List<string>();

                    Regex regex = new Regex("^\\s*" + scriptSeperator + "\\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                    if (regex.IsMatch(Query))
                    {
                        string[] lines = regex.Split(Query);
                        foreach (string line in lines)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                commands.Add(line);
                            }
                        }
                    }

                    if (!StaticFunctions.AppConnection.settingsObject.CheckAccess)
                    {
                        Query = $"BEGIN TRAN {Query} ROLLBACK TRAN";
                    }

                    var aaa = Encoding.UTF8.GetBytes(Query);
                    var bbb = Convert.ToBase64String(aaa);

                    string newString = @"{IsProvision: " + false.ToString().ToLower() + ", ActionType:'SELECT',Query:'" + bbb + "'}";
                    var values = JsonConvert.DeserializeObject<RequestCustomQuery>(newString);
                    request.AddJsonBody(values);


                    Task<RestResponse> t = _restClient.ExecuteAsync(request, token);

                    var response = await t;

                    if (response.IsSuccessful)
                    {

                        string responseResult = string.Empty;
                        // dynamic data = JsonConvert.DeserializeObject(response.Content);
                        if (headerEnvironment.EndPoint.ToLower().Contains("compressed"))
                        {

                            string repStr = response.Content.Replace("\"", "");
                            byte[] convertedToByte = Convert.FromBase64String(repStr);

                            responseResult = Utilities.Unzip(convertedToByte);

                            var custom = JsonConvert.DeserializeObject<CustObj>(responseResult);

                            requestResponse.CustObj = custom;
                            requestResponse.isSuccess = true;

                        }
                        else
                        {
                            responseResult = response.Content;
                        }

                        jsonObj = responseResult;


                        var custom1 = JsonConvert.DeserializeObject<CustObj>(jsonObj);

                        string str = JsonConvert.SerializeObject(custom1.Objects.Select(x => x.Object));

                        System.Data.DataTable dt = JsonConvert.DeserializeObject<System.Data.DataTable>(str);

                        DataSet ds = new DataSet();

                        ds.Tables.Add(dt);


                        requestResponse.isSuccess = true;
                        requestResponse.DataSet = ds;



                        //  return requestResponse;
                    }
                    else
                    {

                        if (!string.IsNullOrEmpty(response.ErrorMessage) && response.ErrorMessage.ToLower().Contains("cancel"))
                            requestResponse.ErrorMessage = "Cancelled request";
                        else
                            requestResponse.ErrorMessage = response.Content;

                        requestResponse.isSuccess = false;

                        return requestResponse;

                    }

                }
                else
                {
                    requestResponse.isSuccess = false;
                    requestResponse.ErrorMessage = "No region selected";
                }

                return requestResponse;
            }
            catch (Exception ex)
            {

                requestResponse.isSuccess = false;
                requestResponse.ErrorMessage = ex.Message;
                return requestResponse;
            }
        }

        //public async Task<RequestResponse> GetLocale()
        //{

        //}

     
    }

    public class CompleteChatRequest
    {
        public string Model { get; set; } = string.Empty;

        public ChatMessage[] Messages { get; set; }

        public ChatCompletionOptions Options { get; set; } = new ChatCompletionOptions();
    }

   

}
