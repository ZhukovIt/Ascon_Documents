namespace CSharp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var config = AppConfiguration.GetConfiguration(args, Console.In.ReadToEnd());
                using var apiClient = new LoodsmanApiClient(config);

                var report = new List<string>();

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