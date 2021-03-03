// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Identity;

namespace Microsoft.Identity.App.DeveloperCredentials
{
    internal class DeveloperCredentialsReader
    {
        public TokenCredential GetDeveloperCredentials(string? username, string? currentApplicationTenantId)
        {
#if AzureSDK
                        DefaultAzureCredentialOptions defaultAzureCredentialOptions = new DefaultAzureCredentialOptions()
                        {
                            SharedTokenCacheTenantId = currentApplicationTenantId,
                            SharedTokenCacheUsername = username,
                        };
                        defaultAzureCredentialOptions.ExcludeManagedIdentityCredential = true;
                        defaultAzureCredentialOptions.ExcludeInteractiveBrowserCredential = true;
                        defaultAzureCredentialOptions.ExcludeAzureCliCredential = true;
                        defaultAzureCredentialOptions.ExcludeEnvironmentCredential = true;



                        DefaultAzureCredential credential = new DefaultAzureCredential(defaultAzureCredentialOptions);
                        return credential;
#endif
            TokenCredential tokenCredential = new MsalTokenCredential(
                currentApplicationTenantId,
                username);
            return tokenCredential;
        }
    }
}
