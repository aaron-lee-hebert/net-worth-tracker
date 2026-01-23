using System.Data;
using System.Data.Common;
using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace NetWorthTracker.Infrastructure.Types;

/// <summary>
/// Custom NHibernate type for handling DateTimeOffset.
/// - SQLite: Stores as ISO 8601 string format
/// - PostgreSQL: Stores as timestamptz (timestamp with time zone)
/// </summary>
public class DateTimeOffsetUserType : IUserType
{
    // Use String type which works across all databases
    // SQLite stores as ISO 8601 string, PostgreSQL converts from string to timestamptz
    public SqlType[] SqlTypes => [new SqlType(DbType.String)];

    public Type ReturnedType => typeof(DateTimeOffset?);

    public bool IsMutable => false;

    public object Assemble(object cached, object owner) => cached;

    public object DeepCopy(object value) => value;

    public object Disassemble(object value) => value;

    public new bool Equals(object? x, object? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Equals(y);
    }

    public int GetHashCode(object x) => x?.GetHashCode() ?? 0;

    public object? NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
    {
        var ordinal = rs.GetOrdinal(names[0]);
        if (rs.IsDBNull(ordinal))
            return null;

        var fieldType = rs.GetFieldType(ordinal);

        // Handle different database representations
        if (fieldType == typeof(DateTime))
        {
            // PostgreSQL timestamptz returns as DateTime
            var dateTime = rs.GetDateTime(ordinal);
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        }
        else
        {
            // SQLite stores as string (ISO 8601)
            var stringValue = rs.GetString(ordinal);
            if (DateTimeOffset.TryParse(stringValue, out var result))
                return result;
            return null;
        }
    }

    public void NullSafeSet(DbCommand cmd, object? value, int index, ISessionImplementor session)
    {
        var parameter = cmd.Parameters[index];

        if (value == null)
        {
            parameter.Value = DBNull.Value;
        }
        else
        {
            var dateTimeOffset = (DateTimeOffset)value;

            // Detect database type from parameter type or connection
            var paramTypeName = parameter.GetType().FullName ?? "";

            if (paramTypeName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                // PostgreSQL: Store as UTC DateTime (timestamptz)
                parameter.Value = dateTimeOffset.UtcDateTime;
            }
            else
            {
                // SQLite: Store as ISO 8601 string
                parameter.Value = dateTimeOffset.ToString("o");
            }
        }
    }

    public object Replace(object original, object target, object owner) => original;
}
