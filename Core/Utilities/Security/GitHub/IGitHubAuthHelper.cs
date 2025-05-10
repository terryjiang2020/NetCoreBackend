using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Entities.Concrete;

namespace Core.Utilities.Security.GitHub
{
    public interface IGitHubAuthHelper
    {
        string GetAuthorizationUrl(string state = null);
        Task<GitHubUserInfo> GetUserInfoAsync(string code);
    }
}