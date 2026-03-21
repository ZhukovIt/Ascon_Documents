using Ascon.Plm.ServerApi;

namespace ExactDocumentFilesReport
{
    /// <summary>
    /// Клиент для взаимодействия с Server API сервера приложений ЛОЦМАН:PLM.
    /// </summary>
    public sealed class LoodsmanServerApiClient
    {
        /// <summary>
        /// Соединение с сервером приложений ЛОЦМАН:PLM.
        /// </summary>
        private readonly IConnection _connection;

        public LoodsmanServerApiClient(string serverAddress, Guid sessionId)
        {
            var uriBuilder = new UriBuilder(serverAddress);

            var connectionFactory = new ConnectionFactory(null, sessionId.ToString());
            _connection = connectionFactory.CreateConnection(uriBuilder.Host, uriBuilder.Port);
        }

        /// <summary>
        /// Возвращает информацию об указанном объекте.
        /// </summary>
        /// <param name="objectId">Идентификатор объекта.</param>
        /// <returns>Возвращает один элемент.</returns>
        /// <exception cref="InvalidOperationException"/>
        public ObjectInfo GetObjectInfo(int objectId)
        {
            object data = _connection
                .MainSystem
                .GetPropObjects(objectId.ToString(), 0, out object errorCode, out object errorMessage);
            if (!errorCode.Equals(0))
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении информации об объекте с идентификатором {objectId}:" +
                    $"\nКод ошибки: {errorCode}." +
                    $"\nСообщение об ошибке: {errorMessage}");
            }

            var objectInfo = Mapper.FirstOrDefault<ObjectInfo>(data);
            if (objectInfo == null)
                throw new InvalidOperationException($"Не удалось получить информацию об объекте с идентификатором {objectId}");

            return objectInfo;
        }

        /// <summary>
        /// Возвращает список файлов указанного документа.
        /// </summary>
        /// <param name="documentId">Идентификатор документа.</param>
        /// <returns>Возвращает набор данных.</returns>
        /// <exception cref="InvalidOperationException"/>
        public List<FileInfo> GetDocumentFiles(int documentId)
        {
            object data = _connection
                .MainSystem
                .GetInfoAboutVersion(null, null, null, documentId, 7, out object errorCode, out object errorMessage);
            if (!errorCode.Equals(0))
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении информации о файлах документа с идентификатором {documentId}:" +
                    $"\nКод ошибки: {errorCode}." +
                    $"\nСообщение об ошибке: {errorMessage}");
            }

            return Mapper.ToList<FileInfo>(data);
        }

        /// <summary>
        /// Возвращает значение атрибута "Наименование" для указанного объекта.
        /// </summary>
        /// <param name="objectId">Идентификатор объекта.</param>
        /// <returns>Возвращает наименование объекта или пустое значение.</returns>
        public string? GetObjectName(int objectId)
        {
            var objectAttributes = GetObjectAttributes(objectId);

            foreach (var objectAttribute in objectAttributes)
            {
                if (objectAttribute.Name == "Наименование")
                {
                    return objectAttribute.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Возвращает список атрибутов указанного объекта.
        /// </summary>
        /// <param name="versionId">Идентификатор объекта.</param>
        /// <returns>Возвращает набор данных.</returns>
        /// <exception cref="InvalidOperationException"/>
        private List<AttributeInfo> GetObjectAttributes(int versionId)
        {
            object data = _connection
                .MainSystem
                .GetInfoAboutVersion(null, null, null, versionId, 3, out object errorCode, out object errorMessage);
            if (!errorCode.Equals(0))
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении атрибутов для объекта с идентификатором {versionId}:" +
                    $"\nКод ошибки: {errorCode}." +
                    $"\nСообщение об ошибке: {errorMessage}");
            }

            return Mapper.ToList<AttributeInfo>(data);
        }

        /// <summary>
        /// Возвращает список дочерних объектов, связанных указанной связью.
        /// </summary>
        /// <param name="versionId">Идентификатор объекта.</param>
        /// <param name="linkType">Название типа связи между объектами.</param>
        /// <returns>Возвращает набор данных.</returns>
        /// <exception cref="InvalidOperationException"/>
        public List<ObjectInfo> GetLinkedObjects(int versionId, string linkType)
        {
            object data = _connection
                .MainSystem
                .GetLinkedFast(versionId, linkType, false, out object errorCode, out object errorMessage);
            if (!errorCode.Equals(0))
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении дочерних объектов для объекта с идентификатором {versionId} по связи {linkType}:" +
                    $"\nКод ошибки: {errorCode}." +
                    $"\nСообщение об ошибке: {errorMessage}");
            }

            return Mapper.ToList<ObjectInfo>(data);
        }
    }
}
