namespace ExactProductStructureReport
{
    /// <summary>
    /// Клиент для работы с API системы Loodsman
    /// </summary>
    public class LoodsmanApiClient : IDisposable
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
        /// Получить готовый отчёт из API приложения.
        /// </summary>
        public async Task<string> GetReport(int idVersion)
        {
            try
            {
                var response = await _httpClient.GetAsync($"Report/rep_VEDOMOST_POKUPNYH?objectIds={idVersion}");
                string content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Ошибка получения данных отчёта: {content}");

                return content;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении информации об объекте {idVersion}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException(
                    $"Превышено время ожидания при получении информации об объекте {idVersion}", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}