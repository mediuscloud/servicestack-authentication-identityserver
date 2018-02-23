// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.Authentication.IdentityServer.Clients
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Interfaces;
    using Logging;
#if NETSTANDARD2_0
    using JsonClient = JsonHttpClient;
    using OpenIdConnectConfiguration = Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration;
#elif NETSTANDARD1_6
    using JsonClient = JsonServiceClient;
    using OpenIdConnectConfiguration = Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration;
#elif NET45
    using JsonClient = JsonServiceClient;
    using OpenIdConnectConfiguration = Microsoft.IdentityModel.Protocols.OpenIdConnectConfiguration;
#endif

    internal class DocumentDiscoveryClient : IDocumentDiscoveryClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DocumentDiscoveryClient));

        private readonly IIdentityServerAuthProviderSettings appSettings;

        public DocumentDiscoveryClient(IIdentityServerAuthProviderSettings settings)
        {
            this.appSettings = settings;
        }

        public async Task<DocumentDiscoveryResult> GetAsync(string endpoint)
        {
            var client = new JsonClient(appSettings.AuthRealm);
            string document;
            try
            {
                document = await client.GetAsync<string>(endpoint)
                                       .ConfigureAwait(false);
            }
            catch (AggregateException exception)
            {
                foreach (var ex in exception.InnerExceptions)
                {
                    Log.Error($"Error occurred requesting document data from {endpoint}", ex);
                }
                return null;
            }

            var configuration = new OpenIdConnectConfiguration(document);

            return new DocumentDiscoveryResult
            {
                AuthorizeUrl = configuration.AuthorizationEndpoint,
                IntrospectUrl = GetStringValue(document, "introspection_endpoint"),
                UserInfoUrl = configuration.UserInfoEndpoint,
                TokenUrl = configuration.TokenEndpoint,
                JwksUrl = configuration.JwksUri
            };
        }

        private static string GetStringValue(string document, string name)
        {
            var dictionary = document.FromJson<Dictionary<string, object>>();
            object obj;
            if (dictionary.TryGetValue(name, out obj))
            {
                return obj as string;
            }
            return null;
        }

        private static string GetStringValue(Dictionary<string, object> document, string name)
        {
            object obj;
            if (document.TryGetValue(name, out obj))
            {
                return obj as string;
            }
            return null;
        }
    }
}
