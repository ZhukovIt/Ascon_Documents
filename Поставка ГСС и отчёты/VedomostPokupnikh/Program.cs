namespace VedomostPokupnikh
{
    /// <summary>
    /// Точка входа консольного приложения для формирования отчёта.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Читает входные параметры, формирует отчёт и выводит его в консоль.
        /// </summary>
        /// <param name="args">Аргументы командной строки.</param>
        async static Task Main(string[] args)
        {
            try
            {
                //получаем входные параметры, необходимые для дальнейшей работы
                //var config = AppConfiguration.GetConfiguration(args, Console.In.ReadToEnd());
                var config = AppConfiguration.GetConfiguration(args,"{\"object_ids\":[4035],\"conf_rules\":{},\"params\":{}}");
                config.SessionId = "c82cb6d4-9bac-d30c-350f-4296382c0feb";
                //настраиваем клиент для общения с приложением
                var apiClient = new LoodsmanApiClient(config);
                //настраиваем сервис, который выдаст данные в нужном формате
                var reportService = new ReportService(apiClient, config);
                //Генерируем отчёт
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
