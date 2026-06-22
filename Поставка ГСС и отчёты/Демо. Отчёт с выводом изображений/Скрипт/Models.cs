namespace ImageOutputReport
{
    /// <summary>
    /// Строка таблицы отчета
    /// </summary>
    public class ReportRow
    {
        /// <summary>
        /// Название типа версии объекта
        /// </summary>
        public required string TypeName { get; set; }

        /// <summary>
        /// Обозначение версии объекта
        /// </summary>
        public required string Product { get; set; }

        /// <summary>
        /// Номер версии объекта 
        /// </summary>
        public required string VersionNumber { get; set; }

        /// <summary>
        /// Значение атрибута "Наименование"
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Значение атрибута "Прикрепленное изображение"
        /// </summary>
        public string? Image { get; set; }
    }

    /// <summary>
    /// Атрибут объекта
    /// </summary>
    public class Attribute
    {
        public int id { get; set; }
        public string name { get; set; }
        public string value { get; set; }
    }

    /// <summary>
    /// Информация о версии объекта
    /// </summary>
    public class ObjectInfo
    {
        public int idVersion { get; set; }
        public string type { get; set; }
        public string product { get; set; }
        public string version { get; set; }
    }

    /// <summary>
    /// Атрибут типа "Изображение" для версии объекта
    /// </summary>
    public class ImageAttribute
    {
        public int id { get; set; }
        public string name { get; set; }
        public string image { get; set; }
    }

}
