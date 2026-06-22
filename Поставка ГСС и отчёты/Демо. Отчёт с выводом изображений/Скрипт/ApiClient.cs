using System.Net.Http.Json;

namespace ImageOutputReport
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
        /// Получить информацию о версии объекта
        /// </summary>
        public async Task<ObjectInfo?> GetObjectInfoAsync(int versionId)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<ObjectInfo>>(
                    $"ObjectInfo/get-prop-objects?objectList={versionId}");

                return result?.FirstOrDefault(x => x.idVersion == versionId);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении информации об объекте {versionId}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException(
                    $"Превышено время ожидания при получении информации об объекте {versionId}", ex);
            }
        }

        /// <summary>
        /// Получить связанные объекты
        /// </summary>
        public async Task<List<ObjectInfo>> GetLinkedObjectsAsync(int versionId, string linkTypeName)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<ObjectInfo>>(
                    $"ObjectInfo/get-linked-fast?idVersion={versionId}&linkType={linkTypeName}");

                return result ?? new List<ObjectInfo>();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении связанных объектов для версии {versionId}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException(
                    $"Превышено время ожидания при получении связанных объектов для версии {versionId}", ex);
            }
        }

        /// <summary>
        /// Получить атрибуты версии объекта
        /// </summary>
        public async Task<List<Attribute>> GetVersionAttributesAsync(int versionId)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<Attribute>>(
                    $"ObjectInfo/get-info-about-version-mode-3?idVersion={versionId}");

                return result ?? new List<Attribute>();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении атрибутов версии {versionId}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException(
                    $"Превышено время ожидания при получении атрибутов версии {versionId}", ex);
            }
        }

        /// <summary>
        /// Получить атрибут типа "Изображение" для версии объекта
        /// </summary>
        public async Task<ImageAttribute?> GetVersionImageAttributeAsync(int versionId, string attributeName)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<ImageAttribute>>(
                    $"ObjectInfo/get-attr-image-value-by-id?idVersion={versionId}&attrName={attributeName}");

                return result?.FirstOrDefault(x => x.name == attributeName);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении атрибута типа \"Изображение\" для версии объекта {versionId}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException(
                    $"Превышено время ожидания при получении атрибута типа \"Изображение\" для версии объекта {versionId}", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
