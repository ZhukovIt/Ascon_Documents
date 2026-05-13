using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExactProductStructureReport
{
    public class AppConfiguration
    {
        /// <summary>
        /// Версия API приложения.
        /// </summary>
        public string ApiVersion { get; set; } = "4";

        /// <summary>
        /// Ожидание выполнения запроса в секундах.
        /// </summary>
        public int RequestTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Стандартный хост сервера, если не указан иной при запуске приложения.
        /// </summary>
        public string AppServerHost { get; set; } = "http://localhost:8076";

        /// <summary>
        /// Сессия пользователя.
        /// </summary>
        public string SessionId { get; set; }

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
        public Dictionary<string, object?> Params { get; set; } = new();

        [JsonPropertyName("conf_rules")]
        public ConfRules? ConfRules { get; set; }

        public string GetStringParameterByName(string parameterName)
        {
            if (Params.TryGetValue(parameterName, out object? parameterValue))
                return parameterValue?.ToString() ?? "";

            return "";
        }

        /// <summary>
        /// Получить конфигурацию из входных параметров.
        /// </summary>
        /// <param name="arguments">Данные, полученные при запуске приложения.</param>
        /// <param name="userData">Входные данные, введённые пользователем в качестве входных параметров.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static AppConfiguration GetConfiguration(string[] arguments, string? userData)
        {
            try
            {
                var config = DeserializeFromJson(userData);
                ApplyConfigParameters(config);
                ApplyCommandLineArguments(config, arguments);
                return config;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Ошибка при разборе конфигурации приложения",
                    ex
                );
            }
        }

        /// <summary>
        /// Применяет конфигурацию приложения.
        /// </summary>
        /// <param name="config">Входные данные приложения.</param>
        private static void ApplyConfigParameters(AppConfiguration config)
        {
            if (config.Params.TryGetValue("ApiVersion", out var apiVersionValue) &&
                double.TryParse(apiVersionValue?.ToString(), out double apiVersion))
            {
                config.ApiVersion = apiVersion.ToString();
            }

            if (config.Params.TryGetValue("RequestTimeoutSeconds", out var requestTimeoutSecondsValue) &&
                int.TryParse(requestTimeoutSecondsValue?.ToString(), out int requestTimeoutSeconds))
            {
                config.RequestTimeoutSeconds = requestTimeoutSeconds;
            }
        }

        /// <summary>
        /// Преобразует данные в удобный для программы формат.
        /// </summary>
        /// <param name="userData">Пользовательские данные.</param>
        /// <returns>Возвращает конфигурацию приложения.</returns>
        private static AppConfiguration DeserializeFromJson(string? userData)
        {
            if (string.IsNullOrWhiteSpace(userData))
                return new AppConfiguration();

            var json = userData.TrimStart('\ufeff').Trim();

            return JsonSerializer.Deserialize<AppConfiguration>(json)
                   ?? throw new InvalidOperationException("JSON конфигурации пустой или некорректный");
        }

        /// <summary>
        /// Применяем конфигурацию для имени хоста и сессии пользователя, полученную при его запуске.
        /// </summary>
        /// <param name="config">Конфигурация приложения.</param>
        /// <param name="arguments">Данные, полученные при запуске приложения.</param>
        private static void ApplyCommandLineArguments(AppConfiguration config, string[] arguments)
        {
            if (TryGetArgumentValue(arguments, "-a", out var host))
                config.AppServerHost = host;

            if (TryGetArgumentValue(arguments, "--session", out var sessionId))
                config.SessionId = sessionId;
        }

        /// <summary>
        /// Получить значение аргумента.
        /// </summary>
        /// <param name="arguments">Данные, полученные при запуске приложения.</param>
        /// <param name="argumentKey">Имя агрумента.</param>
        /// <param name="value">Значение аргумента.</param>
        /// <returns>Возращает результат операции: true -- агрумент найден, false -- агрумент не найден.</returns>
        private static bool TryGetArgumentValue(
            string[] arguments,
            string argumentKey,
            out string value)
        {
            value = string.Empty;

            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], argumentKey, StringComparison.OrdinalIgnoreCase))
                {
                    value = arguments[i + 1];
                    return true;
                }
            }

            return false;
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
