using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExactDocumentFilesReport
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

        /// <summary>
        /// Возвращает идентификатор базового объекта.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public int GetMainObjectId()
        {
            if (ObjectIds == null || ObjectIds.Count == 0)
                throw new InvalidOperationException("Не удалось получить идентификатор базового объекта");

            return ObjectIds[0];
        }

        /// <summary>
        /// Возвращает значение параметра "Глубина разузловки".
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public int GetMaxDepth()
        {
            foreach (var parameter in Parameters)
            {
                if (parameter.Key == "Глубина разузловки")
                {
                    string? parameterValue = parameter.Value?.ToString();

                    if (int.TryParse(parameterValue, out int depth))
                    {
                        return depth;
                    }
                }
            }

            throw new InvalidOperationException("Не удалось получить значение параметра \"Глубина разузловки\"");
        }

        /// <summary>
        /// Возвращает значение параметра "Название типа связи для отбора объектов".
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public string GetSelectingObjectsLinkTypeName()
        {
            foreach (var parameter in Parameters)
            {
                if (parameter.Key == "Название типа связи для отбора объектов")
                {
                    string? parameterValue = parameter.Value?.ToString();

                    if (!string.IsNullOrWhiteSpace(parameterValue))
                    {
                        return parameterValue;
                    }
                }
            }

            throw new InvalidOperationException("Не удалось получить значение параметра \"Название типа связи для отбора объектов\"");
        }

        /// <summary>
        /// Возвращает значение параметра "Название типа связи для отбора документов".
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public string GetSelectingDocumentsLinkTypeName()
        {
            foreach (var parameter in Parameters)
            {
                if (parameter.Key == "Название типа связи для отбора документов")
                {
                    string? parameterValue = parameter.Value?.ToString();

                    if (!string.IsNullOrWhiteSpace(parameterValue))
                    {
                        return parameterValue;
                    }
                }
            }

            throw new InvalidOperationException("Не удалось получить значение параметра \"Название типа связи для отбора документов\"");
        }
    }
}
