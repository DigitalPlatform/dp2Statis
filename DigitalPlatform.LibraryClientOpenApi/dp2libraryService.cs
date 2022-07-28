using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalPlatform.LibraryClientOpenApi
{
    public class dp2libraryService
    {
        private readonly HttpClient _httpClient;

        public string Url { get; set; }

        public dp2libraryService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri(Url);

            /*
            // using Microsoft.Net.Http.Headers;
            // The GitHub API requires two headers.
            _httpClient.DefaultRequestHeaders.Add(
                HeaderNames.Accept, "application/vnd.github.v3+json");
            _httpClient.DefaultRequestHeaders.Add(
                HeaderNames.UserAgent, "HttpRequestsSample");
            */
        }

        /*
        public async Task<IEnumerable<GitHubBranch>?> GetAspNetCoreDocsBranchesAsync() =>
            await _httpClient.GetFromJsonAsync<IEnumerable<GitHubBranch>>(
                "repos/dotnet/AspNetCore.Docs/branches");
        */

        public async Task<LoginResponse> Login(LoginRequest body)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            return await client.LoginAsync(body);
        }
    }
}
