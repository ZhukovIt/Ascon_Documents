using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExactProductStructureReport
{
    public class AppConfiguration
    {
        public string ApiVersion { get; set; } = "3";
        public int RequestTimeoutSeconds { get; set; } = 60;
        public string AppServerHost { get; set; } = "http://localhost:8076";
        public string SessionId { get; set; }

        [JsonPropertyName("object_ids")]
        public List<int> ObjectIds { get; set; } = new();

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

        private static AppConfiguration DeserializeFromJson(string? userData)
        {
            if (string.IsNullOrWhiteSpace(userData))
                return new AppConfiguration();

            var json = userData.TrimStart('\ufeff').Trim();

            return JsonSerializer.Deserialize<AppConfiguration>(json)
                   ?? throw new InvalidOperationException("JSON конфигурации пустой или некорректный");
        }

        private static void ApplyCommandLineArguments(AppConfiguration config, string[] arguments)
        {
            if (TryGetArgumentValue(arguments, "-a", out var host))
                config.AppServerHost = host;

            if (TryGetArgumentValue(arguments, "--session", out var sessionId))
                config.SessionId = sessionId;
        }

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
        [JsonPropertyName("rule_id")]
        public int RuleId { get; set; }

        [JsonPropertyName("final_product_id")]
        public int FinalProductId { get; set; }

        [JsonPropertyName("path")]
        public List<int> Path { get; set; } = new();

        [JsonPropertyName("rule_params")]
        public List<RuleParam> RuleParams { get; set; } = new();

        [JsonPropertyName("fixed_context_id")]
        public int FixedContextId { get; set; }
    }

    public class RuleParam
    {
        [JsonPropertyName("param_name")]
        public string ParamName { get; set; } = string.Empty;

        [JsonPropertyName("param_type")]
        public int ParamType { get; set; }

        [JsonPropertyName("param_value")]
        public string ParamValue { get; set; } = string.Empty;

        [JsonPropertyName("is_any")]
        public bool IsAny { get; set; }
    }
}
