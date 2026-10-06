using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FrappFresh.Infrastructure.Persistence;

// MySQL datetime no guarda offset: todos los instantes se escriben y leen en UTC.
public class UtcDateTimeOffsetConverter() : ValueConverter<DateTimeOffset, DateTime>(
    value => value.UtcDateTime,
    value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));
