using System.Text.Encodings.Web;
using System.Text.Json;

namespace VedomostPokupnikh
{
    /// <summary>
    /// Сервис для генерации отчетов о структуре изделия
    /// </summary>
    public class ReportService
    {
        private readonly PurchaseReportBuilder _reportBuilder;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        /// <summary>
        /// Создаёт сервис генерации отчёта.
        /// </summary>
        /// <param name="apiClient">Клиент для чтения данных из Loodsman Web API.</param>
        /// <param name="config">Конфигурация приложения.</param>
        public ReportService(LoodsmanApiClient apiClient, AppConfiguration config)
        {
            _reportBuilder = new PurchaseReportBuilder(apiClient ?? throw new ArgumentNullException(nameof(apiClient)));
        }

        /// <summary>
        /// Сформировать отчёт через Loodsman Web API v4.
        /// </summary>
        /// <param name="versionId">Идентификатор версии корневого изделия.</param>
        /// <returns>JSON отчёта в формате шаблона ведомости покупных изделий.</returns>
        public async Task<string> GenerateReport(int versionId)
        {
            var report = await _reportBuilder.Build(versionId);
            return JsonSerializer.Serialize(report, _jsonOptions);
        }
    }
}
