using System.Text.Encodings.Web;
using System.Text.Json;

namespace ExactProductStructureReport
{
    /// <summary>
    /// Утилита для печати табличных данных в консоль
    /// </summary>
    public static class ReportPrinter
    {
        /// <summary>
        /// Настройки форматирования JSON. Не рекомендуется изменять без необходимости.
        /// </summary>
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Печать коллекции объектов в виде JSON
        /// </summary>
        public static void PrintJson<T>(IEnumerable<T> items)
        {
            if (items == null)
            {
                Console.WriteLine("Нет данных для отображения");
                return;
            }

            var itemsList = items.ToList();
            if (itemsList.Count == 0)
            {
                Console.WriteLine("Нет данных для отображения");
                return;
            }

            var prettyJson = JsonSerializer.Serialize(items, _options);

            Console.WriteLine(prettyJson);
        }
    }
}