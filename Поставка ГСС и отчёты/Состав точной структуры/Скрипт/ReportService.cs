using System.Globalization;

namespace ExactProductStructureReport
{
    /// <summary>
    /// Сервис для генерации отчетов о структуре изделия
    /// </summary>
    public class ReportService
    {
        private readonly LoodsmanApiClient _apiClient;
        private readonly AppConfiguration _config;
        private List<ReportRow> _reportTable;

        public ReportService(LoodsmanApiClient apiClient, AppConfiguration config)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Сгенерировать отчет о структуре изделия
        /// </summary>
        public async Task<List<ReportRow>> GenerateReportAsync(int rootIdVersion, int maxDepth)
        {
            _reportTable = new List<ReportRow>();

            // Получаем информацию о корневом объекте
            var rootObjects = await _apiClient.GetObjectInfoAsync(rootIdVersion);

            if (rootObjects == null || !rootObjects.Any())
            {
                throw new InvalidOperationException($"Не удалось получить информацию об объекте с ID {rootIdVersion}");
            }

            var rootObject = rootObjects.First();

            // Обходим дерево объектов
            await TraverseTreeAsync(rootObject, 1, maxDepth);

            // Заполняем дополнительные атрибуты
            await EnrichReportDataAsync();
            var firstRow = _reportTable.First();
            var sortedTable = _reportTable.Skip(1).OrderBy(x => x.Product).ToList();
            sortedTable.Insert(0, firstRow);
            return sortedTable;
        }

        /// <summary>
        /// Рекурсивный обход дерева объектов с ограничением по глубине
        /// </summary>
        private async Task TraverseTreeAsync(ObjectInfo currentObject, int currentLevel, int maxDepth)
        {
            // Добавляем текущий объект в отчет
            _reportTable.Add(new ReportRow
            {
                IdLink = currentObject.idLink,
                IdVersion = currentObject.idVersion,
                Type = currentObject.type ?? string.Empty,
                Product = currentObject.product ?? string.Empty,
                VersionNumber = currentObject.version ?? string.Empty,
                Quantity = currentObject.maxCalc
            });

            // Если достигли максимальной глубины, не продолжаем обход
            if (currentLevel >= maxDepth)
                return;

            // Получаем дочерние объекты
            List<ObjectInfo> children;
            try
            {
                children = await _apiClient.GetLinkedObjectsAsync(currentObject.idVersion, _config.GetStringParameterByName("Тип связи"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Предупреждение: не удалось получить дочерние объекты для версии {currentObject.idVersion}: {ex.Message}");
                return;
            }

            if (children == null || children.Count == 0)
                return;

            // Рекурсивно обрабатываем дочерние объекты
            foreach (var child in children)
            {
                await TraverseTreeAsync(child, currentLevel + 1, maxDepth);
            }
        }

        /// <summary>
        /// Обогащение данных отчета дополнительными атрибутами
        /// </summary>
        private async Task EnrichReportDataAsync()
        {
            foreach (var row in _reportTable)
            {
                // Получаем атрибуты версии
                if (row.IdVersion != 0)
                {
                    try
                    {
                        if (row.IdVersion == 965)
                        {

                        }
                        var attributes = await _apiClient.GetVersionAttributesAsync(row.IdVersion);
                        row.Name = attributes
                            .FirstOrDefault(x => x.name == "Наименование")?.value ?? string.Empty;

                        var weightStr = attributes
                            .FirstOrDefault(x => x.name == "Масса")?.value;

                        if (!string.IsNullOrWhiteSpace(weightStr))
                        {
                            row.Weight = CalculateTotalWeight(weightStr, row.Quantity);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Предупреждение: не удалось получить атрибуты версии {row.IdVersion}: {ex.Message}");
                    }
                }

                // Получаем атрибуты связи
                if (row.IdLink != 0)
                {
                    try
                    {
                        var linkAttributes = await _apiClient.GetLinkAttributesAsync(row.IdLink);

                        row.Position = linkAttributes
                            .FirstOrDefault(x => x.name == "Позиция")?.value ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Предупреждение: не удалось получить атрибуты связи {row.IdLink}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Рассчитать общий вес с учетом количества
        /// </summary>
        private string CalculateTotalWeight(string weightStr, double quantity)
        {
            if (string.IsNullOrWhiteSpace(weightStr))
                return string.Empty;

            // Замена точки на запятую для корректного парсинга
            //weightStr = weightStr.Replace(".", ",");

            if (!double.TryParse(weightStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double weight))
                return string.Empty;

            double totalWeight = weight * quantity;

            if (totalWeight == 0)
                return string.Empty;

            return Math.Round(totalWeight, int.Parse(_config.GetStringParameterByName("Количество знаков после запятой")))
                .ToString(CultureInfo.CurrentCulture);
        }
    }
}