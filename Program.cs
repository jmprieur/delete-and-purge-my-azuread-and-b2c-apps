using Azure.Core;
using Microsoft.Identity.App.DeveloperCredentials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeleteAndPurgeMyApps
{
    class Program
    {
        static async Task Main(string tenantId, string username, bool deleteApps)
        {
            AppsManagement appsManagement = new AppsManagement();
            DeveloperCredentialsReader developerCredentialsReader = new DeveloperCredentialsReader();
            TokenCredential tokenCredential = developerCredentialsReader.GetDeveloperCredentials(username, tenantId);

            string tenant = await appsManagement.GetTenantName(tokenCredential);

            Console.WriteLine($"My apps in {tenant}");
            IEnumerable<AppInfo> myApps = (await appsManagement.ListMyApplications(tokenCredential));
            DisplayAppInfo(myApps);

            // Delete all my apps in tenant
            if (deleteApps)
            {
                await appsManagement.DeleteApplications(myApps, tokenCredential);
            }

            Console.WriteLine();
            Console.WriteLine($"My deleted apps  {tenant}");
            var deletedApps = (await appsManagement.ListDeletedApplications(tokenCredential))
                .OrderBy(a => a.deletedDateTime);

            DisplayAppInfo(deletedApps);

            await appsManagement.PurgeApplications(deletedApps, tokenCredential);
        }

        private static void DisplayAppInfo(IEnumerable<AppInfo> apps)
        {
            foreach (AppInfo appInfo in apps)
            {
                Console.WriteLine($"{appInfo.id}\t{appInfo.createdDateTime}\t{appInfo.deletedDateTime}\t{appInfo.displayName}");

            }
        }
    }
}
