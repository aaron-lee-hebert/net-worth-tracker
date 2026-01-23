using System.Data;
using System.Data.Common;
using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using NpgsqlTypes;

namespace NetWorthTracker.Infrastructure.Types;

/// <summary>
/// Custom NHibernate type for handling DateTime with PostgreSQL's timestamptz type.
/// Ensures proper UTC conversion and timezone handling.
/// </summary>
public class PostgresTimestampType : IUserType
{
    public SqlType[] SqlTypes => [new SqlType(DbType.DateTime)];

    public Type ReturnedType => typeof(DateTime);

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
        var value = NHibernateUtil.DateTime.NullSafeGet(rs, names[0], session);
        if (value == null)
            return null;

        var dateTime = (DateTime)value;
        // Ensure we return UTC datetime
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    public void NullSafeSet(DbCommand cmd, object? value, int index, ISessionImplementor session)
    {
        if (value == null)
        {
            NHibernateUtil.DateTime.NullSafeSet(cmd, null, index, session);
        }
        else
        {
            var dateTime = (DateTime)value;
            // Convert to UTC if not already
            if (dateTime.Kind == DateTimeKind.Local)
            {
                dateTime = dateTime.ToUniversalTime();
            }
            else if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }

            NHibernateUtil.DateTime.NullSafeSet(cmd, dateTime, index, session);
        }
    }

    public object Replace(object original, object target, object owner) => original;
}

/// <summary>
/// Custom NHibernate type for handling nullable DateTime with PostgreSQL's timestamptz type.
/// </summary>
public class PostgresNullableTimestampType : IUserType
{
    public SqlType[] SqlTypes => [new SqlType(DbType.DateTime)];

    public Type ReturnedType => typeof(DateTime?);

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
        var value = NHibernateUtil.DateTime.NullSafeGet(rs, names[0], session);
        if (value == null)
            return null;

        var dateTime = (DateTime)value;
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    public void NullSafeSet(DbCommand cmd, object? value, int index, ISessionImplementor session)
    {
        if (value == null)
        {
            NHibernateUtil.DateTime.NullSafeSet(cmd, null, index, session);
        }
        else
        {
            var dateTime = (DateTime)value;
            if (dateTime.Kind == DateTimeKind.Local)
            {
                dateTime = dateTime.ToUniversalTime();
            }
            else if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }

            NHibernateUtil.DateTime.NullSafeSet(cmd, dateTime, index, session);
        }
    }

    public object Replace(object original, object target, object owner) => original;
}

/// <summary>
/// Custom NHibernate type for handling DateTimeOffset with PostgreSQL's timestamptz type.
/// </summary>
public class PostgresDateTimeOffsetType : IUserType
{
    public SqlType[] SqlTypes => [new SqlType(DbType.DateTimeOffset)];

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

        // PostgreSQL timestamptz returns as DateTime, convert to DateTimeOffset
        var value = rs.GetDateTime(ordinal);
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
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
            parameter.Value = dateTimeOffset.UtcDateTime;
        }
    }

    public object Replace(object original, object target, object owner) => original;
}
