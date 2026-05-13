namespace ExactProductStructureReport
{
    /// <summary>
    /// Сервис для генерации отчетов о структуре изделия
    /// </summary>
    public class ReportService
    {
        private readonly LoodsmanApiClient _apiClient;
        public ReportService(LoodsmanApiClient apiClient, AppConfiguration config)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// Получить готовый отчёт из БД.
        /// </summary>
        public async Task<string> GenerateReport(int versionId)
        {
            var report = await _apiClient.GetReport(versionId);

            return report;
        }
    }
}