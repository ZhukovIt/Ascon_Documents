using System.Text;
using System.Text.Json;
using ExactDocumentFilesReport;

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
    ///             "Название типа связи для отбора объектов": "Состоит из ...",
    ///             "Название типа связи для отбора документов": "Документы"
    ///         }
    ///     }
    ///     
    /// </param>
    /// <returns>Возвращает данные отчёта в сериализованном виде.<br/>
    /// Например:
    /// 
    ///     [
    ///       {
    ///         "type": "Спецификация",
    ///         "product": "ПРМР.309001.001",
    ///         "name": "Спецификация",
    ///         "version": "1.0",
    ///         "fileName": "ПРМР.309001.001_Космический аппарат.spw",
    ///         "fileSize": "192 КБайт"
    ///       },
    ///       
    ///       {
    ///         "type": "Спецификация",
    ///         "product": "ПРМР.309001.001",
    ///         "name": "Спецификация",
    ///         "version": "1.0",
    ///         "fileName": "ПРМР.309001.002_Панель в сборе.spw",
    ///         "fileSize": "171 КБайт"
    ///       },
    ///       
    ///       {
    ///         "type": "Спецификация",
    ///         "product": "ПРМР.309001.001",
    ///         "name": "Спецификация",
    ///         "version": "1.0",
    ///         "fileName": "ПРМР.309001.003_Двойная аккумуляторная батарея.spw",
    ///         "fileSize": "169 КБайт"
    ///       }
    ///     ]
    ///     
    /// </returns>
    public string Execute(string serverAddress, Guid sessionId, string configRawData)
    {
        // Создаём конфигурацию приложения
        var appConfiguration = AppConfiguration.Create(configRawData);

        // Получаем идентификатор базового объекта из конфигурации
        int mainObjectId = appConfiguration.GetMainObjectId();

        // Получаем глубину разузловки из конфигурации
        int maxDepth = appConfiguration.GetMaxDepth();

        // Получаем название типа связи для отбора объектов из конфигурации
        string selectingObjectsLinkTypeName = appConfiguration.GetSelectingObjectsLinkTypeName();

        // Получаем название типа связи для отбора документов из конфигурации
        string selectingDocumentsLinkTypeName = appConfiguration.GetSelectingDocumentsLinkTypeName();

        // Создаём клиент для взаимодействия с ServerAPI сервера приложений ЛОЦМАН:PLM
        var apiClient = new LoodsmanServerApiClient(serverAddress, sessionId);

        // Создаём сервис для сбора данных отчёта
        var reportService = new ReportService(apiClient);

        // Формируем данные отчёта
        var reportRows = reportService.GenerateReportData(mainObjectId, maxDepth, selectingObjectsLinkTypeName, selectingDocumentsLinkTypeName);

        // Отправляем данные отчёта в качестве результата выполнения метода
        return JsonSerializer.Serialize(reportRows, Global.JsonSerializerOptions);
    }
}
