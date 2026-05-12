using System.Globalization;

namespace DynamicStructureReport
{
    /// <summary>
    /// Сервис для формирования данных отчёта "Состав точной структуры".
    /// </summary>
    public class ReportService
    {
        /// <summary>
        /// Клиент для взаимодействия с Server API сервера приложений ЛОЦМАН:PLM.
        /// </summary>
        private readonly LoodsmanServerApiClient _apiClient;

        /// <summary>
        /// Данные для вывода в отчёт.
        /// </summary>
        private List<ReportRow> ReportRows { get; } = new();

        /// <summary>
        /// Правила версионного конфигурирования.
        /// </summary>
        private ConfRules? ConfRules { get; set; }

        public ReportService(LoodsmanServerApiClient apiClient, ConfRules? confRules)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

            if (confRules is null)
            {
                throw new ArgumentNullException("Версионная конфигурация не задана.");
            }

            ConfRules = confRules;
        }

        /// <summary>
        /// Формирует данные отчёта.
        /// </summary>
        /// <param name="mainObjectId">Идентификатор базового объекта.</param>
        /// <param name="maxDepth">Глубина разузловки.</param>
        /// <param name="linkTypeName">Название типа связи для отбора объектов.</param>
        /// <param name="signsAfterDot">Количество знаков после запятой для посчёта сумм.</param>
        /// <returns>Возвращает набор данных.</returns>
        public List<ReportRow> GenerateReportData(int mainObjectId, int maxDepth, string linkTypeName, int signsAfterDot, int variantConfig)
        {
            if(maxDepth <= 0)
            {
                return ReportRows;
            }

            var tree = new List<GetDynamicTreeOutputDto>();
            CheckObjectByEffRuleWithRootVersionOutputDto checkInfoAboutVersion;

            ReportRows.Clear();

            //Получаем информацию о головном объекте.
            var mainObjectInfo = _apiClient.GetObjectInfo(mainObjectId);

            //Получаем информацию об атрибуте для версионного конфигурирования.
            var attribute = _apiClient.GetInfoAbouAttribute(ConfRules!.QuickAttrs.First().Typeid);

            //Проверяем возможность построить динамическую структуру/доступность объекта.
            checkInfoAboutVersion = _apiClient.CheckObjectByEffRuleWithRootVersion(mainObjectInfo!, ConfRules, attribute.Name);

            if (checkInfoAboutVersion is not null)
            {
                //Получаем динамическую структуру по заданным параметрам.
                tree = _apiClient.GetDynamicTree(checkInfoAboutVersion.VersionId, ConfRules, attribute.Name, linkTypeName, variantConfig);

                //Получаем информацию о разрешённом объекте.
                var permitObjectInfo = _apiClient.GetObjectInfo(checkInfoAboutVersion.VersionId);

                //Заполняем отчёт.
                FillTree(tree, ConvertToGetDynamicTreeOutputDto(permitObjectInfo), 1, maxDepth, linkTypeName);

            }

            // Заполняем дополнительные атрибуты
            EnrichReportDataAsync(signsAfterDot);

            var firstRow = ReportRows.FirstOrDefault() ?? throw new AggregateException("Нет данных для формирования отчёта.");
            var sortedTable = ReportRows.Skip(1).OrderBy(x => x.Product).ToList();
            sortedTable.Insert(0, firstRow);
            return sortedTable;
        }

        /// <summary>
        /// Сконвертировать головной объект в обычный.
        /// </summary>
        public GetDynamicTreeOutputDto ConvertToGetDynamicTreeOutputDto(MainObjectInfo mainObject)
        {
            return new GetDynamicTreeOutputDto
            {
                LinkId = 0,
                Product = mainObject.Product,
                TypeName = mainObject.TypeName,
                MaxQuantity = 0,
                Version = mainObject.Version ?? string.Empty,
                VersionId = mainObject.VersionId
            };
        }

        /// <summary>
        /// Заполняем таблицу с данными на заданную параметром "глубина разузловки" глубину
        /// </summary>
        private void FillTree(List<GetDynamicTreeOutputDto> dto, GetDynamicTreeOutputDto dtoItem, int currentLevel, int maxDepth, string linkTypeName)
        {
            // Если достигли максимальной глубины, не продолжаем обход
            if (currentLevel > maxDepth)
                return;

            string? objectName = _apiClient.GetObjectName(dtoItem.VersionId);

            ReportRows.Add(new()
            {
                IdLink = dtoItem.LinkId,
                IdVersion = dtoItem.VersionId,
                Type = dtoItem.TypeName,
                Product = dtoItem.Product,
                Name = objectName,
                VersionNumber = dtoItem.Version,
                Quantity = dtoItem.MaxQuantity ?? 0.0d
            });

            var childObjectsCurrent = _apiClient.GetLinkedObjects(dtoItem.VersionId, linkTypeName);

            //отфильтровываем те, которые нам доступны на данной глубине разузловки
            var childObjects = dto.Where(x => childObjectsCurrent.Any(y => x.VersionId == y.VersionId)).ToList();

            currentLevel++;
            foreach (var childObject in childObjects)
            {
                FillTree(dto, childObject, currentLevel, maxDepth, linkTypeName);
            }
        }

        /// <summary>
        /// Обогащение данных отчета дополнительными атрибутами.
        /// </summary>
        private void EnrichReportDataAsync(int signsAfterDot)
        {
            foreach (var row in ReportRows)
            {
                // Получаем атрибуты версии
                if (row.IdVersion != 0)
                {
                    try
                    {
                        var attributes = _apiClient.GetObjectAttributes(row.IdVersion);
                        row.Name = attributes
                            .FirstOrDefault(x => x.Name == "Наименование")?.Value ?? string.Empty;

                        var weightStr = attributes
                            .FirstOrDefault(x => x.Name == "Масса")?.Value;

                        if (!string.IsNullOrWhiteSpace(weightStr))
                        {
                            row.Weight = CalculateTotalWeight(weightStr, row.Quantity, signsAfterDot);
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
                        var linkAttributes = _apiClient.GetLinkedAttributes(row.IdLink);

                        row.Position = linkAttributes
                            .FirstOrDefault(x => x.Name == "Позиция")?.Value ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Предупреждение: не удалось получить атрибуты связи {row.IdLink}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Рассчитать общий вес с учетом количества.
        /// </summary>
        private static string CalculateTotalWeight(string weightStr, double quantity, int signsAfterDot)
        {
            if (string.IsNullOrWhiteSpace(weightStr))
                return string.Empty;

            // Замена точки на запятую для корректного парсинга
            //weightStr = weightStr.Replace(".", ",");

            if (!double.TryParse(weightStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double weight))
                return string.Empty;

            double totalWeight = weight * quantity;

            if (Convert.ToInt32(totalWeight) == 0)
                return string.Empty;

            return Math.Round(totalWeight, signsAfterDot)
                .ToString(CultureInfo.CurrentCulture);
        }
    }
}
