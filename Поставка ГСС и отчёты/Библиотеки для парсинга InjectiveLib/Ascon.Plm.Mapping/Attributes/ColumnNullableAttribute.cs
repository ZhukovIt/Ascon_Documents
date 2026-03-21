namespace Ascon.Plm.Mapping.Attributes
{
    public sealed class ColumnNullableAttribute : ColumnAttribute
    {
        public ColumnNullableAttribute() : base(null, allowNull: true) { }
        public ColumnNullableAttribute(string name) : base(name, allowNull: true) { }
    }
}
