namespace ExactProductStructureReport
{
    public class Program
    {
        async static Task Main(string[] args)
        {
            try
            {
                //получаем входные параметры, необходимые для дальнейшей работы
                var config = AppConfiguration.GetConfiguration(args, Console.In.ReadToEnd());
                //настраиваем клиент для общения с приложением
                var apiClient = new LoodsmanApiClient(config);
                //настраиваем сервис, который выдаст данные в нужном формате
                var reportService = new ReportService(apiClient, config);
                //получаем отчёт
                var report = await reportService.GenerateReport(config.ObjectIds.First());

                //выводим отчёт
                Console.WriteLine(report);

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