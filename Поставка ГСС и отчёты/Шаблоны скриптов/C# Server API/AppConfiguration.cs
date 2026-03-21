using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSharpServerAPI
{
    /// <summary>
    /// Конфигурация приложения.
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// Список идентификаторов базового объекта.
        /// <br/> Будет использован только первый указанный.
        /// </summary>
        [JsonPropertyName("object_ids")]
        public List<int> ObjectIds { get; set; } = new();

        /// <summary>
        /// Список произвольных параметров.
        /// </summary>
        [JsonPropertyName("params")]
        public Dictionary<string, object?> Parameters { get; set; } = new();

        /// <summary>
        /// Создаёт объект конфигурации приложения на основе сериализованных настроек.
        /// </summary>
        /// <param name="rawData">Сериализованные настройки приложения.</param>
        /// <returns>Объект конфигурации приложения.</returns>
        /// <exception cref="ArgumentException"/>
        public static AppConfiguration Create(string rawData)
        {
            var appConfiguration = JsonSerializer.Deserialize<AppConfiguration>(rawData, Global.JsonSerializerOptions);
            if (appConfiguration == null)
                throw new ArgumentException("Не удалось десериализовать конфигурацию приложения из JSON", nameof(rawData));

            return appConfiguration;
        }
    }
}
