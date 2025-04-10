using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Core.Utilities.Security.GitHub
{
    public class GitHubAuthHelper : IGitHubAuthHelper
    {
        private readonly GitHubOptions _options;
        private readonly HttpClient _httpClient;

        public GitHubAuthHelper(IConfiguration configuration)
        {
            _options = configuration.GetSection("GitHubAuth").Get<GitHubOptions>();
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NorthwindBackend", "1.0"));
        }

        public string GetAuthorizationUrl(string state = null)
        {
            var queryParams = new Dictionary<string, string>
            {
                { "client_id", _options.ClientId },
                { "redirect_uri", _options.RedirectUri },
                { "scope", "user:email" }
            };

            if (!string.IsNullOrEmpty(state))
            {
                queryParams.Add("state", state);
            }

            var queryString = string.Join("&", Array.ConvertAll(queryParams.Keys.ToArray(),
                key => $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(queryParams[key])}"));

            return $"{_options.AuthorizationEndpoint}?{queryString}";
        }

        public async Task<GitHubUserInfo> GetUserInfoAsync(string code)
        {
            // Exchange code for access token
            var tokenResponse = await ExchangeCodeForTokenAsync(code);
            
            if (string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new Exception("Failed to get access token from GitHub");
            }

            // Get user profile using the access token
            var userProfile = await GetUserProfileAsync(tokenResponse.AccessToken);
            
            // Get user email if not provided in profile
            if (string.IsNullOrEmpty(userProfile.Email))
            {
                userProfile.Email = await GetUserEmailAsync(tokenResponse.AccessToken);
            }

            return userProfile;
        }

        private async Task<GitHubTokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", _options.ClientId },
                { "client_secret", _options.ClientSecret },
                { "code", code },
                { "redirect_uri", _options.RedirectUri }
            });

            var response = await _httpClient.PostAsync(_options.TokenEndpoint, requestContent);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to exchange code for token: {content}");
            }

            return JsonSerializer.Deserialize<GitHubTokenResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
        }

        private async Task<GitHubUserInfo> GetUserProfileAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await _httpClient.GetAsync(_options.UserInformationEndpoint);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get user profile: {content}");
            }

            return JsonSerializer.Deserialize<GitHubUserInfo>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
        }

        private async Task<string> GetUserEmailAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await _httpClient.GetAsync(_options.UserEmailEndpoint);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get user email: {content}");
            }

            var emails = JsonSerializer.Deserialize<List<GitHubEmail>>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            var primaryEmail = emails.FirstOrDefault(e => e.Primary) ?? emails.FirstOrDefault();
            return primaryEmail?.Email;
        }
        
        private class GitHubTokenResponse
        {
            public string AccessToken { get; set; }
            public string TokenType { get; set; }
            public string Scope { get; set; }
        }

        private class GitHubEmail
        {
            public string Email { get; set; }
            public bool Primary { get; set; }
            public bool Verified { get; set; }
        }
    }
}