using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace io.github.toyota32k.BooLib.Utils;

public static class Json {
    public static async Task<T?> ParseAsync<T>(string value) {
        return await Task.Run(() => {
            return JsonConvert.DeserializeObject<T>(value);
        });
    }

    public static async Task<string> StringifyAsync(object? value) {
        return await Task.Run(() => {
            return JsonConvert.SerializeObject(value);
        });
    }
    public static string Stringify(object? value) {
        return JsonConvert.SerializeObject(value);
    }
    public static T? Parse<T>(string value) {
        return JsonConvert.DeserializeObject<T>(value);
    }

    public static string? GetStringValue(this JObject jObject, string key) {
        return jObject.GetValue(key)?.Value<string>();
    }
    public static string GetStringValue(this JObject jObject, string key, string fallback) {
        return jObject.GetValue(key)?.Value<string>() ?? fallback;
    }
    public static int GetIntValue(this JObject jObject, string key, int fallback = 0) {
        return jObject.GetValue(key)?.Value<int>() ?? fallback;
    }
    public static long GetLongValue(this JObject jObject, string key, long fallback = 0) {
        return jObject.GetValue(key)?.Value<long>() ?? fallback;
    }
}

public class JsonHelper
{
    public static JsonHelper? TryParse(string? json) {
        try {
            if (string.IsNullOrWhiteSpace(json)) {
                return null;
            }
            return new JsonHelper(json);
        }
        catch (JsonReaderException) {
            return null;
        }
    }

    private readonly JObject _jObject;

    private JsonHelper(string json)
    {
        _jObject = JObject.Parse(json);
    }

    public string? GetString(string key)
    {
        return _jObject.GetStringValue(key);
    }

    public string GetString(string key, string fallback)
    {
        return _jObject.GetStringValue(key, fallback);
    }

    public int GetInt(string key, int fallback = 0)
    {
        return _jObject.GetIntValue(key, fallback);
    }

    public long GetLong(string key, long fallback = 0)
    {
        return _jObject.GetLongValue(key, fallback);
    }
}
