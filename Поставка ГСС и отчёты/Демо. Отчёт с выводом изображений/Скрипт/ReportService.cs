namespace ImageOutputReport
{
    public class ReportService
    {
        private const string LINK_TYPE_NAME = "Состоит из ...";
        private const string NAME_ATTRIBUTE = "Наименование";
        private const string IMAGE_ATTRIBUTE = "Прикрепленное изображение";

        private readonly LoodsmanApiClient _apiClient;

        public ReportService(LoodsmanApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// Генерирует отчет
        /// </summary>
        public async Task<List<ReportRow>> GenerateReportAsync(int rootVersionId)
        {
            var reportRows = new List<ReportRow>();

            // Получаем информацию о корневом объекте
            ObjectInfo? rootObject = await _apiClient.GetObjectInfoAsync(rootVersionId);
            if (rootObject == null)
                throw new InvalidOperationException($"Корневой объект с идентификатором {rootVersionId} не найден");

            // Формируем первую строку отчёта
            ReportRow firstRow = await CreateReportRowAsync(rootObject);

            // Получаем информацию о дочерних объектах
            List<ObjectInfo> childObjects = await _apiClient.GetLinkedObjectsAsync(rootVersionId, LINK_TYPE_NAME);

            // Проходим по всем дочерним объектам и формируем данные для отчёта
            foreach (ObjectInfo childObject in childObjects)
            {
                ReportRow reportRow = await CreateReportRowAsync(childObject);
                reportRows.Add(reportRow);
            }

            // Сортируем данные отчёта по обозначению версии объекта
            reportRows = reportRows
                .OrderBy(x => x.Product)
                .ToList();

            // Добавляем первую строку в начало отчёта
            reportRows.Insert(0, firstRow);

            // Возвращаем данные отчёта
            return reportRows;
        }

        /// <summary>
        /// Собирает данные об атрибутах версии объекта и формирует строку отчёта
        /// </summary>
        private async Task<ReportRow> CreateReportRowAsync(ObjectInfo objectInfo)
        {
            ArgumentNullException.ThrowIfNull(objectInfo, nameof(objectInfo));

            var nameAttributeValue = await GetNameAttributeValueAsync(objectInfo.idVersion);
            var imageAttributeValue = await GetImageAttributeValueAsync(objectInfo.idVersion);

            var reportRow = new ReportRow()
            {
                TypeName = objectInfo.type,
                Product = objectInfo.product,
                VersionNumber = objectInfo.version,
                Name = nameAttributeValue,
                Image = imageAttributeValue
            };

            return reportRow;
        }

        /// <summary>
        /// Возвращает значение атрибута "Наименование" для версии объекта
        /// </summary>
        private async Task<string?> GetNameAttributeValueAsync(int versionId)
        {
            var attributes = await _apiClient.GetVersionAttributesAsync(versionId);
            foreach (var attribute in attributes)
            {
                if (attribute.name == NAME_ATTRIBUTE)
                {
                    return attribute.value;
                }
            }

            return null;
        }

        /// <summary>
        /// Возвращает значение атрибута "Прикрепленное изображение" для версии объекта
        /// </summary>
        private async Task<string?> GetImageAttributeValueAsync(int versionId)
        {
            var imageAttribute = await _apiClient.GetVersionImageAttributeAsync(versionId, IMAGE_ATTRIBUTE);
            if (imageAttribute == null)
            {
                return null;
            }

            return imageAttribute.image;
        }
    }
}
