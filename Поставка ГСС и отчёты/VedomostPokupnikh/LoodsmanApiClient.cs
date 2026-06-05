using System.Text.Json;

namespace VedomostPokupnikh
{
    /// <summary>
    /// Клиент для работы с API системы Loodsman
    /// </summary>
    public class LoodsmanApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly AppConfiguration _config;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Создаёт клиент для работы с Loodsman Web API.
        /// </summary>
        /// <param name="config">Конфигурация подключения к API.</param>
        public LoodsmanApiClient(AppConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri($"{_config.AppServerHost}/api/v{_config.ApiVersion}/"),
                Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds)
            };

            _httpClient.DefaultRequestHeaders.Add("web-loodsman-session", _config.SessionId);
            _httpClient.DefaultRequestHeaders.Add("x-loodsman-db-name", _config.DatabaseName);
        }

        /// <summary>
        /// Получить информацию о версии объекта.
        /// </summary>
        /// <param name="idVersion">Идентификатор версии объекта.</param>
        /// <returns>Список сведений о версии объекта.</returns>
        public Task<List<LoodsmanVersionInfo>> GetVersionInfo(int idVersion)
            => GetJson<List<LoodsmanVersionInfo>>($"ObjectInfo/get-info-about-version?idVersion={idVersion}&mode=15");

        /// <summary>
        /// Получить связанные объекты по типу связи.
        /// </summary>
        /// <param name="idVersion">Идентификатор версии исходного объекта.</param>
        /// <param name="linkType">Название типа связи.</param>
        /// <param name="inverse">Признак поиска по обратному направлению связи.</param>
        /// <returns>Список связанных объектов.</returns>
        public Task<List<LoodsmanLinkedObject>> GetLinkedObjects(int idVersion, string linkType, bool inverse = false)
            => GetJson<List<LoodsmanLinkedObject>>(
                $"ObjectInfo/get-linked-fast?idVersion={idVersion}&linkType={UrlEncode(linkType)}&inverse={inverse.ToString().ToLowerInvariant()}");

        /// <summary>
        /// Получить значения атрибутов объектов.
        /// </summary>
        /// <param name="ids">Идентификаторы версий объектов.</param>
        /// <returns>Список значений атрибутов.</returns>
        public Task<List<LoodsmanAttributeValue>> GetAttributes(IEnumerable<int> ids)
            => GetJson<List<LoodsmanAttributeValue>>(
                $"ObjectInfo/get-attributes-values-2?ids={UrlEncode(string.Join(",", ids.Distinct()))}&attrIds=");

        /// <summary>
        /// Получить значения атрибутов связи.
        /// </summary>
        /// <param name="idLink">Идентификатор связи.</param>
        /// <returns>Список значений атрибутов связи.</returns>
        public Task<List<LoodsmanLinkAttributeValue>> GetLinkAttributes(int idLink)
            => GetJson<List<LoodsmanLinkAttributeValue>>($"ObjectInfo/get-link-attributes-2?idLink={idLink}&mode=2");

        /// <summary>
        /// Получить список типов объектов.
        /// </summary>
        /// <returns>Список типов объектов.</returns>
        public Task<List<LoodsmanTypeInfo>> GetTypes()
            => GetJson<List<LoodsmanTypeInfo>>("ConfMetaData/get-types");

        /// <summary>
        /// Получить список типов связей.
        /// </summary>
        /// <returns>Список типов связей.</returns>
        public Task<List<LoodsmanLinkTypeInfo>> GetLinkTypes()
            => GetJson<List<LoodsmanLinkTypeInfo>>("ConfMetaData/get-links");

        /// <summary>
        /// Выполняет GET-запрос к API и десериализует JSON-ответ в указанный тип.
        /// </summary>
        /// <typeparam name="T">Тип результата, в который нужно преобразовать ответ API.</typeparam>
        /// <param name="relativeUrl">Относительный URL метода API.</param>
        /// <returns>Десериализованный ответ API.</returns>
        private async Task<T> GetJson<T>(string relativeUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(relativeUrl);
                string content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Ошибка получения данных из API: {content}");

                return JsonSerializer.Deserialize<T>(content, _jsonOptions)
                       ?? throw new InvalidOperationException($"API вернул пустой ответ для {relativeUrl}");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении данных из API: {relativeUrl}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException(
                    $"Превышено время ожидания при получении данных из API: {relativeUrl}", ex);
            }
        }

        /// <summary>
        /// Кодирует значение для безопасной передачи в query-параметре URL.
        /// </summary>
        /// <param name="value">Исходное значение.</param>
        /// <returns>Закодированное значение.</returns>
        private static string UrlEncode(string value)
            => Uri.EscapeDataString(value);

        /// <summary>
        /// Освобождает HTTP-клиент.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
