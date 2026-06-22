namespace ImageOutputReport
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var config = AppConfiguration.GetConfiguration(args, Console.In.ReadToEnd());
                var apiClient = new LoodsmanApiClient(config);
                var reportService = new ReportService(apiClient);

                var report = await reportService.GenerateReportAsync(config.ObjectIds.First());

                ReportPrinter.PrintJson(report);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Ошибка при генерации отчета: {ex.Message}");
                Console.ResetColor();

                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Детали: {ex.InnerException.Message}");
                }
            }
        }
    }
}
