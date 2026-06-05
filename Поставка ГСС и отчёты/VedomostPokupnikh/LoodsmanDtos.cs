using System.Text.Json.Serialization;

namespace VedomostPokupnikh
{
    /// <summary>
    /// Сведения о версии объекта, полученные из Loodsman Web API.
    /// </summary>
    public sealed class LoodsmanVersionInfo
    {
        /// <summary>
        /// Идентификатор версии объекта.
        /// </summary>
        [JsonPropertyName("idVersion")]
        public int IdVersion { get; set; }

        /// <summary>
        /// Название типа объекта.
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Обозначение объекта.
        /// </summary>
        [JsonPropertyName("product")]
        public string? Product { get; set; }

        /// <summary>
        /// Номер версии объекта.
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        /// <summary>
        /// Признак документного типа объекта.
        /// </summary>
        [JsonPropertyName("document")]
        public int Document { get; set; }
    }

    /// <summary>
    /// Сведения об объекте, связанном с другой версией через заданный тип связи.
    /// </summary>
    public sealed class LoodsmanLinkedObject
    {
        /// <summary>
        /// Идентификатор связи между объектами.
        /// </summary>
        [JsonPropertyName("idLink")]
        public int IdLink { get; set; }

        /// <summary>
        /// Идентификатор версии связанного объекта.
        /// </summary>
        [JsonPropertyName("idVersion")]
        public int IdVersion { get; set; }

        /// <summary>
        /// Название типа связанного объекта.
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Обозначение связанного объекта.
        /// </summary>
        [JsonPropertyName("product")]
        public string? Product { get; set; }

        /// <summary>
        /// Признак документного типа связанного объекта.
        /// </summary>
        [JsonPropertyName("document")]
        public int Document { get; set; }

        /// <summary>
        /// Минимальное количество в связи.
        /// </summary>
        [JsonPropertyName("minQuantity")]
        public double? MinQuantity { get; set; }

        /// <summary>
        /// Единица измерения количества в связи.
        /// </summary>
        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
    }

    /// <summary>
    /// Значение атрибута версии объекта.
    /// </summary>
    public sealed class LoodsmanAttributeValue
    {
        /// <summary>
        /// Идентификатор версии объекта, которому принадлежит атрибут.
        /// </summary>
        [JsonPropertyName("idVersion")]
        public int IdVersion { get; set; }

        /// <summary>
        /// Название атрибута.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Отображаемое значение атрибута.
        /// </summary>
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        /// <summary>
        /// Базовое значение атрибута.
        /// </summary>
        [JsonPropertyName("baseValue")]
        public string? BaseValue { get; set; }
    }

    /// <summary>
    /// Значение атрибута связи между объектами.
    /// </summary>
    public sealed class LoodsmanLinkAttributeValue
    {
        /// <summary>
        /// Название атрибута связи.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Отображаемое значение атрибута связи.
        /// </summary>
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        /// <summary>
        /// Базовое значение атрибута связи.
        /// </summary>
        [JsonPropertyName("baseValue")]
        public string? BaseValue { get; set; }

        /// <summary>
        /// Единица измерения значения атрибута связи.
        /// </summary>
        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
    }

    /// <summary>
    /// Описание типа объекта из метаданных Loodsman.
    /// </summary>
    public sealed class LoodsmanTypeInfo
    {
        /// <summary>
        /// Идентификатор типа объекта.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Название типа объекта.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Идентификатор родительского типа.
        /// </summary>
        [JsonPropertyName("parentId")]
        public int ParentId { get; set; }
    }

    /// <summary>
    /// Описание типа связи из метаданных Loodsman.
    /// </summary>
    public sealed class LoodsmanLinkTypeInfo
    {
        /// <summary>
        /// Идентификатор типа связи.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Прямое название типа связи.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Обратное название типа связи.
        /// </summary>
        [JsonPropertyName("inverseName")]
        public string? InverseName { get; set; }
    }
}
