using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Utilities.Security.GitHub
{
    public class GitHubUserInfo
    {
        public long Id { get; set; }
        public string Login { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
    }
}