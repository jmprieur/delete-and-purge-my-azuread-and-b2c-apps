using Azure.Core;
using Microsoft.Graph;
using Microsoft.Identity.App.DeveloperCredentials;
using Microsoft.Identity.App.MicrosoftIdentityPlatformApplication;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DeleteAndPurgeMyApps
{
    internal class AppInfo
    {
        public string id { get; set; }
        public string displayName { get; set; }

        public string deletedDateTime { get; set; }
        public string createdDateTime { get; set; }
    }

    internal class AppsManagement
    {
        GraphServiceClient? _graphServiceClient;


        internal async Task<string> GetTenantName(TokenCredential tokenCredential)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);
            Organization tenant = await GetTenant(graphServiceClient)!;
            return tenant.DisplayName;
        }

        internal async Task DeleteApplications(IEnumerable<AppInfo> appsToDelete, TokenCredential tokenCredential)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);
            foreach (AppInfo appToDelete in appsToDelete)
            {
                await graphServiceClient.Applications[appToDelete.id]
                    .Request()
                    .DeleteAsync();
            }
        }

        internal async Task<IEnumerable<AppInfo>> ListMyApplications(TokenCredential tokenCredential)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);
            var myApps = (await graphServiceClient.Me.OwnedObjects
                .Request()
                .GetAsync()).OfType<Application>();

            return myApps.Select(a => new AppInfo() { id = a.Id, displayName = a.DisplayName });
        }

        public async Task<List<AppInfo>> ListDeletedApplications(TokenCredential tokenCredential)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);
            User me = await graphServiceClient.Me.Request().GetAsync();

            List<AppInfo> deletedApplications = new List<AppInfo>();

            HttpClient httpClient = new HttpClient();
            AuthenticationResult authResult = await (tokenCredential as MsalTokenCredential).GetRawTokenAsync(
                new string[] { "https://graph.microsoft.com/.default" },
                CancellationToken.None);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {authResult.AccessToken}");
            // httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,
                "https://graph.microsoft.com/v1.0/directory/deletedItems/getUserOwnedObjects");
            httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(new { userId = me.Id, type = "Application" }),
                Encoding.UTF8, "application/json");

            var result = await httpClient.SendAsync(httpRequestMessage);


            string json = await result.Content.ReadAsStringAsync();
            JsonElement myDeletedApps = JsonSerializer.Deserialize<JsonElement>(json);

            JsonProperty collectionProperty = (JsonProperty)myDeletedApps.EnumerateObject().ToArray()[1];
            JsonElement collection = collectionProperty.Value;

            foreach (JsonElement app in collection.EnumerateArray())
            {
                AppInfo deletedApplication = new AppInfo
                {
                    id = GetProperty(app, "id"),
                    displayName = GetProperty(app, "displayName"),
                    deletedDateTime = GetProperty(app, "deletedDateTime"),
                    createdDateTime = GetProperty(app, "createdDateTime"),
                };
                deletedApplications.Add(deletedApplication);
            }
            return deletedApplications;
        }

        public async Task PurgeApplications(IEnumerable<AppInfo> deletedApps, TokenCredential tokenCredential)
        {
            foreach (AppInfo deletedApp in deletedApps)
            {
                await PurgeApplications(deletedApp, tokenCredential);
            }
        }

        public async Task PurgeApplications(AppInfo deletedApp, TokenCredential tokenCredential)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);
            await graphServiceClient.Directory.DeletedItems[deletedApp.id]
                .Request()
                .DeleteAsync();
        }

        private static string GetProperty(JsonElement app, string propertyName)
        {
            return app.EnumerateObject().FirstOrDefault(p => p.Name == propertyName).Value.GetString();
        }

        private GraphServiceClient GetGraphServiceClient(TokenCredential tokenCredential)
        {
            if (_graphServiceClient == null)
            {
                _graphServiceClient = new GraphServiceClient(new TokenCredentialAuthenticationProvider(tokenCredential));
            }
            return _graphServiceClient;

        }
        private static async Task<Organization?> GetTenant(GraphServiceClient graphServiceClient)
        {
            Organization? tenant = null;
            try
            {
                tenant = (await graphServiceClient.Organization
                    .Request()
                    .GetAsync()).FirstOrDefault();
            }
            catch (ServiceException ex)
            {
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                }
                else
                {
                    if (ex.Message.Contains("User was not found") || ex.Message.Contains("not found in tenant"))
                    {
                        Console.WriteLine("User was not found.\nUse both --tenant-id <tenant> --username <username@tenant>.\nAnd re-run the tool.");
                    }
                    else
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                Environment.Exit(1);
            }

            return tenant;
        }

    }
}
