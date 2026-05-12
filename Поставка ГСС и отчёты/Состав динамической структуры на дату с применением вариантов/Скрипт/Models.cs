using Ascon.Plm.Mapping.Attributes;
namespace DynamicStructureReport
{
    /// <summary>
    /// Данные версии документа и его файла для вывода в отчёт.
    /// </summary>
    public class ReportRow
    {
        /// <summary>
        /// Служебный идентификатор версии (не выводится в отчет)
        /// </summary>
        public int IdVersion { get; set; }

        /// <summary>
        /// Служебный идентификатор связи (не выводится в отчет)
        /// </summary>
        public int IdLink { get; set; }

        /// <summary>
        /// Тип версии
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Обозначение версии
        /// </summary>
        public string? Product { get; set; }


        /// <summary>
        /// Наименование версии
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Номер версии (версии с разными номерами в разных строках)
        /// </summary>
        public string? VersionNumber { get; set; }

        /// <summary>
        /// Суммарное количество значений атрибута связи
        /// </summary>
        public double Quantity { get; set; }

        /// <summary>
        /// Суммарная масса с учетом рассчитанного количества
        /// </summary>
        public string? Weight { get; set; }

        /// <summary>
        /// Позиция (через запятую если разные позиции в разных вхождениях)
        /// </summary>
        public string? Position { get; set; }
    }

    /// <summary>
    /// Информация об объекте.
    /// </summary>
    public class ObjectInfo
    {
        [Column("_ID_LINK")]
        public int IdLink { get; set; }

        /// <summary>
        /// Идентификатор объекта.
        /// </summary>
        [Column("_ID_VERSION")]
        public int VersionId { get; set; }

        /// <summary>
        /// Название типа объекта.
        /// </summary>
        [Column("_TYPE")]
        public required string TypeName { get; set; }

        /// <summary>
        /// Обозначение объекта.
        /// </summary>
        [Column("_PRODUCT")]
        public required string Product { get; set; }

        /// <summary>
        /// Номер версии объекта.
        /// </summary>
        [Column("_VERSION")]
        public string? Version { get; set; }


        [Column("_MAX_QUANTITY")]
        public double MaxCalc { get; set; }
    }

    /// <summary>
    /// Информация о головном объекте.
    /// </summary>
    public class MainObjectInfo
    {
        /// <summary>
        /// Идентификатор объекта.
        /// </summary>
        [Column("_ID_VERSION")]
        public int VersionId { get; set; }

        /// <summary>
        /// Название типа объекта.
        /// </summary>
        [Column("_TYPE")]
        public required string TypeName { get; set; }

        /// <summary>
        /// Обозначение объекта.
        /// </summary>
        [Column("_PRODUCT")]
        public required string Product { get; set; }

        /// <summary>
        /// Номер версии объекта.
        /// </summary>
        [Column("_VERSION")]
        public string? Version { get; set; }
    }

    /// <summary>
    /// Информация об атрибуте объекта.
    /// </summary>
    public class AttributeInfo
    {
        /// <summary>
        /// Идентификатор атрибута.
        /// </summary>
        [Column("_ID")]
        public int Id { get; set; }

        /// <summary>
        /// Название атрибута.
        /// </summary>
        [Column("_NAME")]
        public required string Name { get; set; }

        /// <summary>
        /// Значение атрибута.
        /// </summary>
        [Column("_VALUE")]
        public required string Value { get; set; }
    }

    public class CheckObjectByEffRuleWithRootVersionOutputDto
    {
        [Column("_VERSION_ID")] public int VersionId { get; set; }
        [Column("_RULE_STRING_ID")] public int RuleStringId { get; set; }
    }

    public class GetDynamicTreeOutputDto
    {
        [Column("_LINK_ID")] public int LinkId { get; set; }
        [Column("_MAIN_ID")] public int MainId { get; set; }
        [Column("_PRODUCT")] public string Product { get; set; }
        [Column("_TYPE_ID")] public int TypeId { get; set; }
        [Column("_TYPE_NAME")] public string TypeName { get; set; }
        [Column("_IS_DOCUMENT")] public int IsDocument { get; set; }
        [Column("_VERSION_ID")] public int VersionId { get; set; }
        [Column("_ID_STATE")] public int IdState { get; set; }
        [Column("_STATE_NAME")] public string StateName { get; set; }
        [Column("_VERSION")] public string Version { get; set; }
        [Column("_DATEOFCREATE")] public DateTime DateOfCreate { get; set; }
        [Column("_MODIFIED")] public DateTime Modified { get; set; }
        [Column("_OWNER_ID")] public int OwnerId { get; set; }
        [Column("_LINK_TYPE_ID")] public int LinkTypeId { get; set; }
        [Column("_LINKED_VERSION_ID")] public int LinkedVersionId { get; set; }
        [Column("_ID_LOCK")] public int IdLock { get; set; }
        [Column("_ACCESSLEVEL")] public int AccessLevel { get; set; }
        [Column("_LINK_DIRECTION")] public int LinkDirection { get; set; }
        [Column("_MIN_QUANTITY")] public double? MinQuantity { get; set; }
        [Column("_MAX_QUANTITY")] public double? MaxQuantity { get; set; }
        [Column("_ID_MEASURE")] public string IdMeasure { get; set; }
        [Column("_ID_UNIT")] public string IdUnit { get; set; }
        [Column("_UNIT")] public string Unit { get; set; }
        [Column("_MEASURE")] public string Measure { get; set; }
        [Column("_RULE_STRING_ID")] public int RuleStringId { get; set; }
        [Column("_FIXED_BY")] public int FixedBy { get; set; }
        [Column("_PASSED")] public bool Passed { get; set; }
        [Column("_PASSED_VERSIONS")] public int PassedVersion { get; set; }
    }

    public class GetAttributeListDto
    {
        [Column("_ID")]
        public int Id { get; set; }
        [Column("_NAME")]
        public string Name { get; set; }
/*        [Column]
        public byte _ATTRTYPE { get; set; }
        [Column]
        public string _DEFAULT { get; set; }
        [ColumnNullable]
        public string _LIST { get; set; }
        [Column]
        public bool _SYSTEM { get; set; }
        [Column]
        public int _ONLYLISTITEMS { get; set; }
        [Column]
        public byte _ACCESSLEVEL { get; set; }
        [ColumnNullable]
        public string _ID_NATURE { get; set; }
        [Column]
        public string _ALIAS { get; set; }*/
    }
}
