using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DynamicStructureReport
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
        /// Версионное конфигурирование.
        /// </summary>
        [JsonPropertyName("conf_rules")]
        public ConfRules? ConfRules { get; set; }

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
        /// Возвращает значение параметра "Тип связи".
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public string GetTypeLink()
        {
            foreach (var parameter in Parameters)
            {
                if (parameter.Key == "Тип связи")
                {
                    string? parameterValue = parameter.Value?.ToString();

                    if (!string.IsNullOrWhiteSpace(parameterValue))
                    {
                        return parameterValue;
                    }
                }
            }

            throw new InvalidOperationException("Не удалось получить значение параметра \"Тип связи\"");
        }

        /// <summary>
        /// Возвращает значение параметра "Количество знаков после запятой".
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public int GetSingsAfterDot()
        {
            foreach (var parameter in Parameters)
            {
                if (parameter.Key == "Количество знаков после запятой")
                {
                    string? parameterValue = parameter.Value?.ToString();

                    if (int.TryParse(parameterValue, out int signs))
                    {
                        return signs;
                    }
                }
            }

            throw new InvalidOperationException("Не удалось получить значение параметра \"Количество знаков после запятой\"");
        }

        /// <summary>
        /// Возвращает режим учёта вариантного конфигурирования.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public int GetVariantConfig()
        {
            foreach (var parameter in Parameters)
            {
                if (parameter.Key == "Режим учёта вариатного конфигурирования")
                {
                    string? parameterValue = parameter.Value?.ToString();

                    if (int.TryParse(parameterValue, out int config))
                    {
                        return config;
                    }
                }
            }

            throw new InvalidOperationException("Не удалось получить значение параметра \"Дата\"");
        }

        /// <summary>
        /// Получить версионное конфигурирование.
        /// </summary>
        public ConfRules GetConfigRules()
        {
            if (ConfRules is not null)
            {
                return ConfRules;
            }

            throw new InvalidOperationException("Не удалось получить значения версионного конфигурирования.");
        }
    }
    
    public class ConfRules
    {

        [JsonPropertyName("contextId")]
        public int ContextId { get; set; }

        [JsonPropertyName("ruleId")]
        public int RuleId { get; set; }

        [JsonPropertyName("endVersionId")]
        public int EndVersionid { get; set; }

        [JsonPropertyName("quickAttrs")]
        public List<QuickAttrs> QuickAttrs { get; set; } = new();
    }

    public class QuickAttrs
    {

        [JsonPropertyName("isState")]
        public bool IsState { get; set; }

        [JsonPropertyName("attrTypeId")]
        public int Typeid { get; set; }

        [JsonPropertyName("attrValue")]
        public string ParamValue { get; set; } = string.Empty;

        [JsonPropertyName("anyValue")]
        public bool AnyValue { get; set; }
    }
}
