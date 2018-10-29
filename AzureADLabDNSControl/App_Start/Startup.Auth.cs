﻿using System;
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.Owin.Security.Notifications;
using Microsoft.IdentityModel.Protocols;
using System.Web.Mvc;
using System.Configuration;
using System.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens;
using System.Web;
using System.Linq;
using System.Security.Claims;
using Graph;
using Lab.Common;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AzureADLabDNSControl
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            var authProvider = new CookieAuthenticationProvider
            {
                OnResponseSignIn = ctx =>
                {
                    var task = Task.Run(async () => {
                        await AuthInit(ctx);
                    });
                    task.Wait();
                },
                OnValidateIdentity = ctx =>
                {
                    //good spot to troubleshoot nonces, etc...
                    return Task.FromResult(0);
                }
            };

            var cookieOptions = new CookieAuthenticationOptions
            {
                Provider = authProvider,
                CookieManager = new Microsoft.Owin.Host.SystemWeb.SystemWebChunkingCookieManager()
            };

            app.UseCookieAuthentication(cookieOptions);

            OpenIdConnectAuthenticationOptions LabAdminOptions = new OpenIdConnectAuthenticationOptions
            {
                ClientId = Settings.LabAdminClientId,
                Authority = Settings.AdminAuthority,
                PostLogoutRedirectUri = "/",
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    RedirectToIdentityProvider = (context) =>
                    {
                        string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;
                        context.ProtocolMessage.RedirectUri = appBaseUrl + "/";
                        context.ProtocolMessage.PostLogoutRedirectUri = appBaseUrl;
                        return Task.FromResult(0);
                    },
                },
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                },
                AuthenticationType = CustomAuthType.LabAdmin
            };
            app.UseOpenIdConnectAuthentication(LabAdminOptions);

            OpenIdConnectAuthenticationOptions LabUserOptions = new OpenIdConnectAuthenticationOptions
            {
                ClientId = Settings.LabUserClientId,
                Authority = Settings.UserAuthority,
                PostLogoutRedirectUri = "/",
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    RedirectToIdentityProvider = (context) =>
                    {
                        string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;
                        context.ProtocolMessage.RedirectUri = appBaseUrl + "/";
                        context.ProtocolMessage.PostLogoutRedirectUri = appBaseUrl;
                        context.ProtocolMessage.Prompt = "login";
                        return Task.FromResult(0);
                    },
                },
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                },
                AuthenticationType = CustomAuthType.LabUser
            };
            app.UseOpenIdConnectAuthentication(LabUserOptions);

        }

        private static string EnsureTrailingSlash(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }
    }
}
