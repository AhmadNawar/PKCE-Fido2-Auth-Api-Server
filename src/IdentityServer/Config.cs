// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServer
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                    new IdentityResources.Profile(),
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new List<ApiScope>
            {
                new ApiScope("api1", "My API")
            };

        public static IEnumerable<Client> Clients =>
            new List<Client>
            {
                new Client
                {
                    ClientId = "client",

                    // no interactive user, use the clientid/secret for authentication
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    // secret for authentication
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    // scopes that client has access to
                    AllowedScopes = { "api1" }
                },
                new Client
                {
                    ClientId="pkce_client",
                    AllowedGrantTypes=GrantTypes.Code,
                    RequireClientSecret = false,
                    RedirectUris={"https://localhost:5001/callback"},
                    PostLogoutRedirectUris={"https://localhost:5002/signout-callback-oidc"},
                    AllowedScopes=new List<string>
                    {
                        "api1"
                    }
                },
                new Client
                {
                    ClientId="react_native_client",
                    AllowedGrantTypes=GrantTypes.Code,
                    RequireClientSecret = false,
                    RedirectUris={"https://auth.expo.io/@ahmaddroubi/pkce-test"},
                    PostLogoutRedirectUris={"https://auth.expo.io/@ahmaddroubi/pkce-test"},
                    AllowedScopes=new List<string>
                    {
                        "api1"
                    }
                },
                // OpenID Connect implicit flow client (MVC)
                new Client
                {
                    ClientId = "mvc",
                    ClientName = "MVC Client",
                    AllowedGrantTypes = GrantTypes.Implicit,

                    // where to redirect to after login
                    RedirectUris = { "https://localhost:44328/signin-oidc" },

                    // where to redirect to after logout
                    PostLogoutRedirectUris = { "http://localhost:44328/signout-callback-oidc" },

                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile
                    },

                    RequireConsent = false
                }
            };

        public static List<TestUser> Users => new List<TestUser> {
            new TestUser
            {
                Username = "alice",
                Claims = new Claim[] {
                    new Claim(JwtClaimTypes.Name, "Alice Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Alice"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                    new Claim(JwtClaimTypes.Email, "AliceSmith@email.com")
                },
                Password = "Abcd@1234",
                IsActive = true,
                SubjectId = Guid.NewGuid().ToString(),
            }
        };
    }
}