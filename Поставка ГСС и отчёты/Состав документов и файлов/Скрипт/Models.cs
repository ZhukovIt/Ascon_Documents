using Ascon.Plm.Mapping.Attributes;

namespace ExactDocumentFilesReport
{
    /// <summary>
    /// Данные версии документа и его файла для вывода в отчёт.
    /// </summary>
    public class ReportRow
    {
        /// <summary>
        /// Тип документа.
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Обозначение версии документа.
        /// </summary>
        public required string Product { get; set; }

        /// <summary>
        /// Наименование версии документа.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Номер версии документа.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Имя файла.
        /// </summary>
        public required string FileName { get; set; }

        /// <summary>
        /// Форматированный размер файла.
        /// </summary>
        public required string FileSize { get; set; }
    }

    /// <summary>
    /// Информация об объекте.
    /// </summary>
    public class ObjectInfo
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
        /// Основан ли объект на документном типе?
        /// <br/> 0 - Объект основан на объектном типе;
        /// <br/> 1 - Объект основан на документном типе.
        /// </summary>
        [Column("_DOCUMENT")]
        public int IsDocument { get; set; }

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
    /// Информация о файле документа.
    /// </summary>
    public class FileInfo
    {
        /// <summary>
        /// Имя файла.
        /// </summary>
        [Column("_NAME")]
        public required string Name { get; set; }

        /// <summary>
        /// Размер файла в байтах.
        /// </summary>
        [Column("_SIZE")]
        public long Size { get; set; }
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
}
