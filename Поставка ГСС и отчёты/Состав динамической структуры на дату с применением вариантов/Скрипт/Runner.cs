using System.Text;
using System.Text.Json;
using DynamicStructureReport;
/// <summary>
/// Главный класс приложения.
/// </summary>
public class Runner
{
    static Runner()
    {
        // Регистрирует провайдер кодировок для поддержки Windows-специфичных кодировок (как Windows-1251).
        // Без этой настройки методы ServerAPI могут выбросить исключение.
        // Не рекомендуется изменять без необходимости.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Главный метод приложения.
    /// </summary>
    /// <param name="serverAddress">Адрес сервера приложений.
    /// <br/> Например, http://localhost:8076</param>
    /// <param name="sessionId">Уникальный глобальный идентификатор сессии с сервером приложений.
    /// <br/> Например, 901a4b51-e8d4-457d-9fb1-06e6c27dcb93</param>
    /// <param name="configRawData">Сериализованные настройки приложения.<br/>
    /// Например:
    ///
    ///     {
    ///         "object_ids": [2904, 2, 3],
    ///         "params":
    ///         {
    ///             "Глубина разузловки": 3,
    ///             "Тип связи": "Состоит из ...",
    ///             "Количество знаков после запятой": "2"
    ///         }
    ///     }
    ///     
    /// </param>
    /// <returns>Возвращает данные отчёта в сериализованном виде.<br/>
    /// Например:
    /// 
    ///     [
    ///         {
    ///           "idVersion": 311,
    ///           "idLink": 0,
    ///           "type": "Сборочная единица",
    ///           "product": "078.505.9.0100.00",
    ///           "name": "Редуктор",
    ///           "versionNumber": "1",
    ///           "quantity": 0,
    ///           "weight": "",
    ///           "position": null
    ///         },
    ///         {
    ///           "idVersion": 334,
    ///           "idLink": 1237,
    ///           "type": "Деталь",
    ///           "product": "078.505.0.0102.00",
    ///           "name": "Шестерня",
    ///           "versionNumber": "1",
    ///           "quantity": 1,
    ///           "weight": "0,54",
    ///           "position": "1"
    ///         }
    ///    ]
    ///     
    /// </returns>
    public string Execute(string serverAddress, Guid sessionId, string configRawData)
    {
        List<ReportRow> reportRows;

        // Создаём конфигурацию приложения
        var appConfiguration = AppConfiguration.Create(configRawData);

        // Получаем идентификатор базового объекта из конфигурации
        int mainObjectId = appConfiguration.GetMainObjectId();

        // Получаем глубину разузловки из конфигурации
        int maxDepth = appConfiguration.GetMaxDepth();

        //Получаем значение типа связи для отбора объектов
        string LinkTypeName = appConfiguration.GetTypeLink();

        //Получаем количество знаков после запятой
        int signsAfterDot = appConfiguration.GetSingsAfterDot();

        //Получаем режим учёта вариантного конфигурирования
        int variantConfig = appConfiguration.GetVariantConfig();

        //Получаем правила версионного конфигурирования. Если они есть, то ставятся в приоритет. если их нет, то динамическая структура строится по дате.
        var confRules = appConfiguration.GetConfigRules();

        // Создаём клиент для взаимодействия с ServerAPI сервера приложений ЛОЦМАН:PLM
        var apiClient = new LoodsmanServerApiClient(serverAddress, sessionId);

        // Создаём сервис для сбора данных отчёта
        var reportService = new ReportService(apiClient, confRules);

        // Формируем данные отчёта
        reportRows = reportService.GenerateReportData(mainObjectId, maxDepth, LinkTypeName, signsAfterDot, variantConfig);

        // Отправляем данные отчёта в качестве результата выполнения метода
        return JsonSerializer.Serialize(reportRows, Global.JsonSerializerOptions);
    }
}
