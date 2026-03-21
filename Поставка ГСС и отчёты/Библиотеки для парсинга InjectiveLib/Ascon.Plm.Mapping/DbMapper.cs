using Ascon.Plm.DataPacket;
using Ascon.Plm.Mapping.Attributes;
using Ascon.Plm.Mapping.Exceptions;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Ascon.Plm.Mapping
{
    public static class DbMapper
    {
        private class EntityColumnInfo
        {
            public MemberInfo Member { get; set; }
            public Type MemberType { get; set; }
            public bool AllowNull { get; set; }
            public int Ordinal { get; set; }
            public Type ColumnType { get; set; }
        }

        private static IEnumerable<EntityColumnInfo> GetEntityColumns(IDataReader reader, Type entityType)
        {
            var ordinals = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, i => i, StringComparer.OrdinalIgnoreCase);

            var properties = TypeDescriptor.GetProperties(entityType)
                .Cast<PropertyDescriptor>()
                .Select(pd => (
                    MappedMember: (MemberInfo)entityType.GetProperty(pd.Name, pd.PropertyType),
                    MappingParameters: (ColumnAttribute)pd.Attributes[typeof(ColumnAttribute)]))
                .Where(entry => entry.MappedMember != null);
            var fields = entityType.GetFields()
                .Select(field => (
                    MappedMember: (MemberInfo)field,
                    MappingParameters: field.GetCustomAttribute<ColumnAttribute>(true)));
            var members = from entry in fields.Concat(properties)
                          let member = entry.MappedMember
                          let attribute = entry.MappingParameters
                          where attribute != null
                          select new { Name = attribute.Name ?? member.Name, Member = member, Attribute = attribute };

            var columnNotExists = members.FirstOrDefault(m => !ordinals.ContainsKey(m.Name));
            if (columnNotExists != null)
                throw new DbMapperException($"Member {columnNotExists.Name} of class {entityType.FullName} is not found in result set");

            var columns = from m in members
                          let ordinal = ordinals[m.Name]
                          select new EntityColumnInfo
                          {
                              Member = m.Member,
                              MemberType = m.Member is PropertyInfo info ? info.PropertyType : ((FieldInfo)m.Member).FieldType,
                              Ordinal = ordinal,
                              ColumnType = reader.GetFieldType(ordinal),
                              AllowNull = m.Attribute.AllowNull
                          };

            var cantBeNullable = columns.FirstOrDefault(c => c.AllowNull && c.MemberType.IsValueType && Nullable.GetUnderlyingType(c.MemberType) == null);
            if (cantBeNullable != null)
                throw new DbMapperException($"Member {cantBeNullable.Member.Name} of class {entityType.FullName} marked as Nullable but can not be null");

            return columns;
        }

        private static readonly HashSet<TypeCode> NumericTypeCodes = new()
        {
            TypeCode.Byte,
            TypeCode.Int16,
            TypeCode.Int32,
            TypeCode.Int64,
            TypeCode.SByte,
            TypeCode.UInt16,
            TypeCode.UInt32,
            TypeCode.UInt64,
            TypeCode.Decimal,
            TypeCode.Double,
            TypeCode.Single
        };

        private static bool TypeIsNumeric(Type type)
        {
            return NumericTypeCodes.Contains(Type.GetTypeCode(type));
        }

        private static readonly Dictionary<TypeCode, string> GetterMethods = new()
        {
            { TypeCode.Boolean, "GetBoolean" },
            { TypeCode.Byte, "GetByte" },
            { TypeCode.Int16, "GetInt16" },
            { TypeCode.Int32, "GetInt32" },
            { TypeCode.Int64, "GetInt64" },
            { TypeCode.Decimal, "GetDecimal" },
            { TypeCode.String, "GetString" },
            { TypeCode.DateTime, "GetDateTime" },
            { TypeCode.Double, "GetDouble" },
            { TypeCode.Object, "GetValue" },
        };

        private static Expression BuildPropertyAssignExpression(Expression instanceVar, EntityColumnInfo column, ParameterExpression readerVar)
        {
            var propType = column.MemberType;
            var getMethod = typeof(IDataRecord).GetMethod(GetterMethods[Type.GetTypeCode(column.ColumnType)]);
            var ordinalConst = Expression.Constant(column.Ordinal);

            Expression getter = Expression.Call(readerVar, getMethod, ordinalConst);

            if (propType != getter.Type)
            {
                var underlyingPropType = Nullable.GetUnderlyingType(propType) ?? propType;

                if (underlyingPropType == typeof(bool) && TypeIsNumeric(column.ColumnType))
                    getter = Expression.NotEqual(getter, Expression.Constant(Convert.ChangeType(0, getter.Type)));
                else if (getter.Type == typeof(object))
                {
                    getter = Expression.TryCatch(
                     Expression.ConvertChecked(getter, underlyingPropType),
                     Expression.Catch(typeof(InvalidCastException),
                         Expression.Block(GetTypeConversionErrorExpression(column, propType), Expression.Default(underlyingPropType))));
                }
                else if (TypeIsNumeric(underlyingPropType) && TypeIsNumeric(column.ColumnType))
                {
                    if (underlyingPropType.IsEnum)
                        getter = Expression.ConvertChecked(getter, underlyingPropType.GetEnumUnderlyingType());

                    getter = Expression.ConvertChecked(getter, underlyingPropType);
                }
                else if (underlyingPropType != column.ColumnType)
                {
                    getter = Expression.Block(propType, GetTypeConversionErrorExpression(column, propType), Expression.Default(propType));
                }

                if (getter.Type != propType)
                    getter = Expression.Convert(getter, propType);
            }

            return Expression.IfThen(
                Expression.IsFalse(Expression.Call(readerVar, typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull)), ordinalConst)),
                Expression.Assign(Expression.PropertyOrField(instanceVar, column.Member.Name), getter));

            static UnaryExpression GetTypeConversionErrorExpression(EntityColumnInfo columnPapams, Type dtoPropertyType)
            {
                return Expression.Throw(Expression.Constant(
                    new InvalidOperationException($"The column [{columnPapams.Member.Name}] cannot be converted to {dtoPropertyType.FullName}")));
            }
        }

        private static Expression<Func<IDataReader, T>> BuildCreateInstanceExpression<T>(IDataReader reader) where T : class
        {
            var entityType = typeof(T);

            var readerVar = Expression.Parameter(typeof(IDataReader), "reader");
            var instanceVar = Expression.Variable(entityType, "instance");

            var block = new List<Expression> { Expression.Assign(instanceVar, Expression.New(entityType)) };

            var columns = GetEntityColumns(reader, entityType);
            block.AddRange(columns.Select(c => BuildPropertyAssignExpression(instanceVar, c, readerVar)));
            block.Add(instanceVar);

            return Expression.Lambda<Func<IDataReader, T>>(Expression.Block(new[] { instanceVar }, block), readerVar);
        }

        private static Func<IDataReader, T> BuildCreateInstanceDelegate<T>(IDataReader reader) where T : class
        {
            return BuildCreateInstanceExpression<T>(reader).Compile();
        }

        private class CreateInstanceDelegateCacheKey : Tuple<int, int, int>
        {
            public CreateInstanceDelegateCacheKey(int fieldCount, int hashOfNames, int hashOfTypes) : base(fieldCount, hashOfNames, hashOfTypes) { }
            public override string ToString() => $"{Item1}_{Item2:X8}_{Item3:X8}";
        }

        private static class CreateInstanceDelegateCache<T>
        {
            private static readonly ConcurrentDictionary<CreateInstanceDelegateCacheKey, Lazy<Func<IDataReader, T>>> Types = new();

            public static Func<IDataReader, T> GetOrAdd(CreateInstanceDelegateCacheKey key, Func<CreateInstanceDelegateCacheKey, Func<IDataReader, T>> valueFactory)
            {
                return Types.GetOrAdd(key, k => new Lazy<Func<IDataReader, T>>(() => valueFactory(k))).Value;
            }
        }

        private static CreateInstanceDelegateCacheKey GetKey(IDataReader r)
        {
            return new CreateInstanceDelegateCacheKey(r.FieldCount,
                HashCodeCombiner.Combine(Enumerable.Range(0, r.FieldCount).Select(i => r.GetName(i).GetHashCode())),
                HashCodeCombiner.Combine(Enumerable.Range(0, r.FieldCount).Select(i => r.GetFieldType(i).GetHashCode())));
        }

        private static Func<IDataReader, T> CreateInstanceFunc<T>(IDataReader reader) where T : class
        {
            ArgumentNullException.ThrowIfNull(reader, nameof(reader));

            return CreateInstanceDelegateCache<T>.GetOrAdd(GetKey(reader), key => BuildCreateInstanceDelegate<T>(reader));
        }

        public static int Count(IDataReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader, nameof(reader));

            if (reader is DataPacketReader dataPacketReader)
            {
                return dataPacketReader.RecordsAffected;
            }
            else
            {
                int count = 0;

                while (reader.Read())
                {
                    count++;
                }

                return count;
            }
        }

        public static bool Any(IDataReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader, nameof(reader));

            if (reader is DataPacketReader dataPacketReader)
            {
                return dataPacketReader.RecordsAffected > 0;
            }
            else
            {
                return reader.Read();
            }
        }

        public static IEnumerable<T> ToEnumerable<T>(IDataReader reader) where T : class
        {
            ArgumentNullException.ThrowIfNull(reader, nameof(reader));

            var createInstanceFunc = CreateInstanceFunc<T>(reader);

            while (reader.Read())
                yield return createInstanceFunc(reader);
        }

        public static List<T> ToList<T>(IDataReader reader) where T : class
        {
            ArgumentNullException.ThrowIfNull(reader, nameof(reader));

            var createInstanceFunc = CreateInstanceFunc<T>(reader);

            var results = new List<T>();

            while (reader.Read())
            {
                results.Add(createInstanceFunc(reader));
            }

            return results;
        }

        public static T[] ToArray<T>(IDataReader reader) where T : class
        {
            ArgumentNullException.ThrowIfNull(reader, nameof(reader));

            var createInstanceFunc = CreateInstanceFunc<T>(reader);

            if (reader is DataPacketReader dataPacketReader)
            {
                var results = new T[dataPacketReader.RecordsAffected];
                int index = 0;

                while (reader.Read())
                {
                    results[index++] = createInstanceFunc(reader);
                }

                return results;
            }
            else
            {
                var results = new List<T>();

                while (reader.Read())
                {
                    results.Add(createInstanceFunc(reader));
                }

                return results.ToArray();
            }
        }

        public static T First<T>(IDataReader reader) where T : class
        {
            ArgumentNullException.ThrowIfNull(reader, nameof(reader));

            var createInstanceFunc = CreateInstanceFunc<T>(reader);

            if (reader.Read())
            {
                return createInstanceFunc(reader);
            }

            throw new InvalidOperationException("Коллекция не содержит элементов");
        }

        public static T FirstOrDefault<T>(IDataReader reader) where T : class
        {
            ArgumentNullException.ThrowIfNull(reader, nameof(reader));

            var createInstanceFunc = CreateInstanceFunc<T>(reader);

            if (reader.Read())
            {
                return createInstanceFunc(reader);
            }

            return null;
        }
    }
}
