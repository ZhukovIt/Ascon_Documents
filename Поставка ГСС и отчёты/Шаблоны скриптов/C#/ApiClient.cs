namespace CSharp
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

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}