using Microsoft.Data.Sqlite;

namespace MuhasebeSistemi.Data;

/// <summary>
/// SqliteConnection için yardımcı extension metodlar.
/// Tekrarlayan komut/parametre kodlarını azaltır.
/// </summary>
public static class SqliteExtensions
{
    public static void ExecuteNonQuery(this SqliteConnection conn,
        string sql, params (string name, object? value)[] parameters)
    {
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public static object? ExecuteScalar(this SqliteConnection conn,
        string sql, params (string name, object? value)[] parameters)
    {
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd.ExecuteScalar();
    }

    public static List<T> Query<T>(this SqliteConnection conn,
        string sql, params (string name, object? value)[] parameters) where T : new()
    {
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);

        var result = new List<T>();
        using var reader = cmd.ExecuteReader();
        var props = typeof(T).GetProperties();
        var colMap = new Dictionary<string, System.Reflection.PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        // Sütun adı → property eşlemesi (snake_case → PascalCase)
        foreach (var prop in props)
        {
            colMap[prop.Name] = prop;
            colMap[ToSnakeCase(prop.Name)] = prop;
        }

        while (reader.Read())
        {
            var obj = new T();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var colName = reader.GetName(i);
                if (colMap.TryGetValue(colName, out var prop) && !reader.IsDBNull(i))
                {
                    var val = reader.GetValue(i);
                    try
                    {
                        if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                            prop.SetValue(obj, Convert.ToDecimal(val));
                        else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                            prop.SetValue(obj, Convert.ToInt32(val));
                        else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                            prop.SetValue(obj, Convert.ToInt32(val) != 0);
                        else if (prop.PropertyType == typeof(string))
                            prop.SetValue(obj, val.ToString());
                        else
                            prop.SetValue(obj, val);
                    }
                    catch { /* tip dönüşüm hatalarını atla */ }
                }
            }
            result.Add(obj);
        }
        return result;
    }

    public static T? QueryOne<T>(this SqliteConnection conn,
        string sql, params (string name, object? value)[] parameters) where T : new()
    {
        var list = conn.Query<T>(sql, parameters);
        return list.Count > 0 ? list[0] : default;
    }

    private static string ToSnakeCase(string name)
    {
        // IslemNo → islem_no, AnaKategori → ana_kategori
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                sb.Append('_');
            sb.Append(char.ToLower(name[i]));
        }
        return sb.ToString();
    }
}
