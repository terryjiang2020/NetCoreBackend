using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Utilities.Security.GitHub
{
    public class GitHubOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        public string AuthorizationEndpoint { get; set; } = "https://github.com/login/oauth/authorize";
        public string TokenEndpoint { get; set; } = "https://github.com/login/oauth/access_token";
        public string UserInformationEndpoint { get; set; } = "https://api.github.com/user";
        public string UserEmailEndpoint { get; set; } = "https://api.github.com/user/emails";
    }
}