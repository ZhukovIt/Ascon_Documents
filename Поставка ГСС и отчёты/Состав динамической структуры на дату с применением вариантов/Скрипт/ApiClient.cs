using Ascon.Plm.ServerApi;
using System.Data.Common;
using System.Text.Json;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DynamicStructureReport
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
        public MainObjectInfo GetObjectInfo(int objectId)
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

            var objectInfo = Mapper.FirstOrDefault<MainObjectInfo>(data);

            if (objectInfo == null)
                throw new InvalidOperationException($"Не удалось получить информацию об объекте с идентификатором {objectId}");

            return objectInfo;
        }

        /// <summary>
        /// Возвращает информацию о конкретном атрибуте.
        /// </summary>
        /// <param name="attrId">Id атрибута</param>
        public GetAttributeListDto GetInfoAbouAttribute(int attrId)
        {
            object data = _connection.MainSystem.GetAttributeList2(1/*без учёта режима*/, out object errorCode, out object errorMessage);

            if (!errorCode.Equals(0))
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении информации об атрибутах:" +
                    $"\nКод ошибки: {errorCode}." +
                    $"\nСообщение об ошибке: {errorMessage}");
            }

            var objectsInfo = Mapper.ToList<GetAttributeListDto>(data);
            var objectInfo = objectsInfo.FirstOrDefault(x => x.Id == attrId);

            if (objectInfo == null)
                throw new InvalidOperationException($"Не удалось получить информацию об атрибуте");

            return objectInfo;
        }

        /// <summary>
        /// Проверяет информацию о версии с помощью id указанного правила.
        /// </summary>
        /// <param name="objectId">Идентификатор объекта.</param>
        /// <returns>Возвращает один элемент.</returns>
        /// <exception cref="InvalidOperationException"/>
        public CheckObjectByEffRuleWithRootVersionOutputDto CheckObjectByEffRuleWithRootVersion(MainObjectInfo mainObjectInfo, ConfRules rules, string attrName)
        {
            var quickAttrs = rules.QuickAttrs.First();

            object quickParamsValues = new object[] { new object[] { !quickAttrs.IsState, attrName, quickAttrs.ParamValue, quickAttrs.AnyValue } };

            object data = _connection
                .MainSystem
                .CheckObjectByEffRuleWithRootVersion(mainObjectInfo.TypeName, mainObjectInfo.Product, rules.RuleId, rules.ContextId, quickParamsValues, mainObjectInfo.VersionId, out object errorCode, out object errorMessage);

            if (!errorCode.Equals(0))
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении информации об объекте:" +
                    $"\nКод ошибки: {errorCode}." +
                    $"\nСообщение об ошибке: {errorMessage}");
            }

            var objectInfo = Mapper.FirstOrDefault<CheckObjectByEffRuleWithRootVersionOutputDto>(data);

            if (objectInfo == null)
                throw new InvalidOperationException($"Не удалось получить информацию об объекте с идентификатором {mainObjectInfo.VersionId}");

            return objectInfo;
        }

        /// <summary>
        /// Строит динамическую структуру по указанным параметрам.
        /// Построение производится только по связям нисходящих вертикальных типов связей.
        /// </summary>
        /// <param name="mainObjectId">Идентификатор головного объекта.</param>
        /// <param name="rules">Правила версионного конфигурирования, выбранные пользователем.</param>
        /// <param name="attrName">Имя атрибута для версионного конфигурирования.</param>
        /// <param name="linkTypeName">Имя нисходящих вертикальных типов связей, по которой строится динамическая структура.</param>
        /// <param name="variantConfig">Режим учёта вариатного конфигурирования: 0 -- не учитывать, 1 -- учитывать, 2 -- учитывать из текущего сеанса.</param>
        /// <returns>Возвращает элемент на основе которого строится динамическая структура.</returns>
        /// <exception cref="InvalidOperationException"/>
        public List<GetDynamicTreeOutputDto> GetDynamicTree(int mainObjectId, ConfRules rules, string attrName, string linkTypeName, int variantConfig)
        {
            //Пример версионного конфигурирования по параметру "Дата".
            //var quickParameters = new List<object>() { new object() { RuleCondition = 1, Name = "Дата", Value = "2016-11-01T00:00:00", IsAnyValue = false}  };

            var quickAttrs = rules.QuickAttrs.First();

            //Параметры версионного конфигурирования, выбранные пользователем.
            object quickParamsValues = new object[] { new object[] { !quickAttrs.IsState, attrName, quickAttrs.ParamValue, quickAttrs.AnyValue } };

            object data = _connection
                .MainSystem
                .GetDynamicTree(mainObjectId, rules.RuleId, rules.EndVersionid, null, quickParamsValues, rules.ContextId, linkTypeName, "", "", false, false, true, variantConfig, 0, out object errorCode, out object errorMessage);


            if (!errorCode.Equals(0))
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении информации об объекте:" +
                    $"\nКод ошибки: {errorCode}." +
                    $"\nСообщение об ошибке: {errorMessage}");
            }

            var objectInfo = Mapper.ToList<GetDynamicTreeOutputDto>(data);

            if (objectInfo == null)
                throw new InvalidOperationException($"Не удалось получить информацию об объекте с идентификатором {mainObjectId}");

            return objectInfo;
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
        public List<AttributeInfo> GetObjectAttributes(int versionId)
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

        /// <summary>
        /// Возвращает список id дочерних объектов, связанных указанной связью.
        /// </summary>
        /// <param name="versionId">Идентификатор объекта.</param>
        /// <param name="linkType">Название типа связи между объектами.</param>
        /// <returns>Возвращает набор данных.</returns>
        /// <exception cref="InvalidOperationException"/>
        public List<int> GetLinkedObjectsIds(int versionId, string linkType)
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

            return Mapper.ToList<ObjectInfo>(data).Select(x=> x.VersionId).ToList();
        }

        /// <summary>
        /// Возвращает значения атрибутов связи для данного экземпляра связи, включая служебные.
        /// </summary>
        /// <param name="idLink">Идентификатор экземпляра связи.</param>
        public List<AttributeInfo> GetLinkedAttributes(int idLink)
        {
            var data = _connection.MainSystem.GetLinkAttributes2(idLink, 0, out object errorCode, out object errorMessage);

            if (!errorCode.Equals(0))
            {
                throw new InvalidOperationException(
                    $"Ошибка при получении атрибутов связи для объекта по id связи {idLink}:" +
                    $"\nКод ошибки: {errorCode}." +
                    $"\nСообщение об ошибке: {errorMessage}");
            }

            return Mapper.ToList<AttributeInfo>(data);
        }
    }
}
