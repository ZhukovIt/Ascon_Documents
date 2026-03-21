using System.Text.Encodings.Web;
using System.Text.Json;

namespace CSharpServerAPI
{
    /// <summary>
    /// Глобальные настройки приложения.
    /// </summary>
    public static class Global
    {
        /// <summary>
        /// Настройки форматирования JSON.
        /// <br/> Не рекомендуется изменять без необходимости.
        /// </summary>
        public static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            // Форматирует JSON с отступами и переносами строк для удобного чтения человеком.
            // Без этого JSON будет в одну строку.
            WriteIndented = true,

            // Делает десериализацию нечувствительной к регистру имён свойств.
            // Например, можно десериализовать {"Name": "..."} в свойство name.
            PropertyNameCaseInsensitive = true,

            // Автоматически преобразует имена свойств C# в camelCase при сериализации.
            // Например, свойство FirstName станет firstName.
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

            // Ослабляет экранирование символов в JSON:
            // - Не экранирует ASCII символы (кроме обязательных:  "  \  \b  \f  \n  \r  \t  )
            // - Сохраняет кириллицу и Unicode символы как есть, без \uXXXX
            // - Полезно для читаемости и сокращения размера JSON
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}
