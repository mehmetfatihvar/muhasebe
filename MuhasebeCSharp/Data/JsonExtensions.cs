using System.Text.Json;

namespace MuhasebeSistemi.Data;

public static class JsonExtensions
{
    public static string? TryGetProp(this JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val))
            return val.ValueKind == JsonValueKind.Null ? null : val.ToString();
        return null;
    }
}

public static class SqliteConnExtensions
{
    public static void CommitIfTransaction(this Microsoft.Data.Sqlite.SqliteConnection conn) { }
}
