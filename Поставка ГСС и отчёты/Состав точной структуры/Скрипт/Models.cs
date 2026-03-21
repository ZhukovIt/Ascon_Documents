namespace ExactProductStructureReport
{
    /// <summary>
    /// Строка таблицы отчета о структуре изделия
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
        public string Type { get; set; }

        /// <summary>
        /// Обозначение версии
        /// </summary>
        public string Product { get; set; }

        /// <summary>
        /// Наименование версии
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Номер версии (версии с разными номерами в разных строках)
        /// </summary>
        public string VersionNumber { get; set; }

        /// <summary>
        /// Суммарное количество значений атрибута связи
        /// </summary>
        public double Quantity { get; set; }

        /// <summary>
        /// Суммарная масса с учетом рассчитанного количества
        /// </summary>
        public string Weight { get; set; }

        /// <summary>
        /// Позиция (через запятую если разные позиции в разных вхождениях)
        /// </summary>
        public string Position { get; set; }
    }

    /// <summary>
    /// Атрибут объекта или связи
    /// </summary>
    public class Attributes
    {
        public int id { get; set; }
        public string name { get; set; }
        public string value { get; set; }
    }

    /// <summary>
    /// Информация об объекте из системы Loodsman
    /// </summary>
    public class ObjectInfo
    {
        public int idLink { get; set; }
        public int idVersion { get; set; }
        public string type { get; set; }
        public string product { get; set; }
        public string version { get; set; }
        public string state { get; set; }
        public double minCalc { get; set; }
        public double maxCalc { get; set; }
    }
}