using System.Net.Http.Json;

namespace StructureComposition
{
    /// <summary>
    /// Клиент для работы с API системы Loodsman
    /// </summary>
    public sealed class LoodsmanApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly AppConfiguration _config;

        public LoodsmanApiClient(AppConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri($"{_config.AppServerHost}/api/v{_config.ApiVersion}/"),
                Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds)
            };

            _httpClient.DefaultRequestHeaders.Add("web-loodsman-session", _config.SessionId);
        }

        /// <summary>
        /// Возвращает список подразделений и должностей, в которых текущий пользователь назначен на должность.
        /// </summary>
        public async Task<List<GetAddressBook>> GetUserAddressBookAsync(int userId)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<GetAddressBook>>(
                    $"OrgStructure/get-address-book-tree-mode-6?parent={userId}");

                return result ?? new List<GetAddressBook>();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении информации подразделениях пользователя {userId}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException(
                    $"Превышено время ожидания при получении информации о подразделениях пользователя {userId}", ex);
            }
        }

        /// <summary>
        /// Возвращает список всех ролей для должности.
        /// </summary>
        public async Task<List<RoleFromPost>> GetPostUserRoles(int postId)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<RoleFromPost>>(
                    $"OrgStructure/posts/{postId}/roles");

                return result ?? new List<RoleFromPost>();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении информации о ролях для должности {postId}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException(
                    $"Превышено время ожидания при получении информации о ролях для должности {postId}", ex);
            }
        }

        /// <summary>
        /// Возвращает список подразделений и должностей, в которых текущий пользователь назначен на должность.
        /// </summary>
        public async Task<CurrentUser> GetCurrentUserAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<CurrentUser>(
                    $"Auth/current-user");

                return result ?? new CurrentUser();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении информации о текущем пользователе", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException(
                    $"Превышено время ожидания при получении информации о текущем пользователе", ex);
            }
        }

        /// <summary>
        /// Возвращает список пользователей, которым назначены какие либо должности в указанном подразделении.
        /// </summary>
        public async Task<List<UserFromUnit>> GetInfoAboutUsersAsync(int deputyId)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<UserFromUnit>>(
                    $"OrgStructure/units/{deputyId}/users");

                return result ?? new List<UserFromUnit>();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении информации о пользователях в подразделении {deputyId}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException(
                    $"Превышено время ожидания при получении информации о пользователях в подразделении {deputyId}", ex);
            }
        }

        /// <summary>
        /// Возвращает список всех должностей
        /// </summary>
        public async Task<List<Post>> GetAllPostsAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<Post>>(
                    "OrgStructure/posts");

                return result ?? new List<Post>();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    "Ошибка при получении информации о всех должностях", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException(
                    "Превышено время ожидания при получении информации о всех должностях", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
