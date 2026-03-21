namespace ExactDocumentFilesReport
{
    /// <summary>
    /// Сервис для формирования данных отчёта "Состав документов и файлов".
    /// </summary>
    /// <remarks>
    /// <para>
    /// Отчёт включает информацию о файлах, прикреплённых к документам. Для каждого файла
    /// создаётся отдельная строка с информацией о родительском документе.
    /// </para>
    /// <para>
    /// <b>Особенность логики:</b> Если документ не имеет файлов, он не создаёт записей в отчёте,
    /// но обход дерева для его дочерних документов и объектов продолжается. Это позволяет
    /// получить файлы из глубоких уровней вложенности, даже если промежуточные документы
    /// не содержат собственных файлов.
    /// </para>
    /// </remarks>
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

        public ReportService(LoodsmanServerApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// Формирует данные отчёта.
        /// </summary>
        /// <param name="mainObjectId">Идентификатор базового объекта.</param>
        /// <param name="maxDepth">Глубина разузловки.</param>
        /// <param name="selectingObjectsLinkTypeName">Название типа связи для отбора объектов.</param>
        /// <param name="selectingDocumentsLinkTypeName">Название типа связи для отбора документов.</param>
        /// <returns>Возвращает набор данных.</returns>
        public List<ReportRow> GenerateReportData(int mainObjectId, int maxDepth, string selectingObjectsLinkTypeName, 
            string selectingDocumentsLinkTypeName)
        {
            ReportRows.Clear();

            var mainObjectInfo = _apiClient.GetObjectInfo(mainObjectId);

            TraverseTree(mainObjectInfo, 1, maxDepth, selectingObjectsLinkTypeName, selectingDocumentsLinkTypeName);

            return ReportRows;
        }

        /// <summary>
        /// Рекурсивно обходит дерево дочерних объектов и документов для формирования данных отчёта.
        /// </summary>
        /// <param name="objectInfo">Информация о текущем объекте.</param>
        /// <param name="currentLevel">Текущий уровень разузловки.</param>
        /// <param name="maxDepth">Глубина разузловки.</param>
        /// <param name="selectingObjectsLinkTypeName">Название типа связи для отбора объектов.</param>
        /// <param name="selectingDocumentsLinkTypeName">Название типа связи для отбора документов.</param>
        private void TraverseTree(ObjectInfo objectInfo, int currentLevel, int maxDepth, string selectingObjectsLinkTypeName, 
            string selectingDocumentsLinkTypeName)
        {
            if (currentLevel > maxDepth)
                return;

            string? objectName = _apiClient.GetObjectName(objectInfo.VersionId);

            if (objectInfo.IsDocument == 1)
            {
                var documentFiles = _apiClient.GetDocumentFiles(objectInfo.VersionId);
                foreach (var documentFile in documentFiles)
                {
                    ReportRows.Add(new()
                    {
                        Type = objectInfo.TypeName,
                        Product = objectInfo.Product,
                        Name = objectName,
                        Version = objectInfo.Version,
                        FileName = documentFile.Name,
                        FileSize = FormatFileSize(documentFile.Size)
                    });
                }
            }

            var documents = _apiClient.GetLinkedObjects(objectInfo.VersionId, selectingDocumentsLinkTypeName);
            foreach (var document in documents)
            {
                TraverseTree(document, currentLevel + 1, maxDepth, selectingObjectsLinkTypeName, selectingDocumentsLinkTypeName);
            }

            var childObjects = _apiClient.GetLinkedObjects(objectInfo.VersionId, selectingObjectsLinkTypeName);
            foreach (var childObject in childObjects)
            {
                TraverseTree(childObject, currentLevel + 1, maxDepth, selectingObjectsLinkTypeName, selectingDocumentsLinkTypeName);
            }
        }

        /// <summary>
        /// Форматирует размер файла.
        /// </summary>
        /// <param name="fileBytes">Размер файла в байтах.</param>
        /// <returns>Возвращает форматированное значение размера файла.</returns>
        private static string FormatFileSize(long fileBytes)
        {
            string[] units = { "Байт", "КБайт", "МБайт", "ГБайт", "ТБайт", "ПБайт", "ЭБайт" };

            int order = 0;

            while (fileBytes >= 1024 && order < (units.Length - 1))
            {
                order++;
                fileBytes /= 1024;
            }

            return $"{fileBytes:0} {units[order]}";
        }
    }
}
