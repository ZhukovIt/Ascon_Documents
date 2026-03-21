namespace ExactProductStructureReport
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            try
            {
                var config = AppConfiguration.GetConfiguration(args, Console.In.ReadToEnd());
                var apiClient = new LoodsmanApiClient(config);
                var reportService = new ReportService(apiClient, config);
               
                var report = await reportService.GenerateReportAsync(config.ObjectIds.First(), 
                    int.Parse(config.GetStringParameterByName("Глубина разузловки")));

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