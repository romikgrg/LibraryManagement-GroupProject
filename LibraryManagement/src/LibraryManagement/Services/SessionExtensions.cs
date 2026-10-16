using System.Text.Json;

namespace LibraryManagement.Services;

/// <summary>
/// Session state only stores strings, ints and byte arrays. These helpers keep
/// the JSON round trip in one place instead of repeating it in every controller.
/// </summary>
public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value) =>
        session.SetString(key, JsonSerializer.Serialize(value));

    public static T? GetObject<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }
}
