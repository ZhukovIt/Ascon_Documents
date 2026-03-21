namespace Ascon.Plm.Mapping.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ColumnAttribute : Attribute
    {
        public ColumnAttribute() : this(null, allowNull: false) { }
        public ColumnAttribute(string name) : this(name, allowNull: false) { }

        protected ColumnAttribute(string name, bool allowNull)
        {
            Name = name;
            AllowNull = allowNull;
        }

        public string Name { get; }
        public bool AllowNull { get; }
    }
}
