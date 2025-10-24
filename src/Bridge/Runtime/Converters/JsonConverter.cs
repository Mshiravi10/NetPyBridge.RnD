using System.Text.Json;
using Python.Runtime;

namespace Bridge.Runtime.Converters;

public static class JsonConverter
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static PyObject ToPython(object value)
    {
        var json = JsonSerializer.Serialize(value, DefaultOptions);
        return new PyString(json);
    }

    public static object? FromPython(PyObject pyObject, Type targetType)
    {
        if (pyObject == null || pyObject.IsNone())
            return null;

        var json = pyObject.As<string>();
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonSerializer.Deserialize(json, targetType, DefaultOptions);
    }
}

public interface IJsonSerializer
{
    string Serialize(object value);
    T? Deserialize<T>(string json);
    object? Deserialize(string json, Type type);
}

public class SystemTextJsonSerializer : IJsonSerializer
{
    private readonly JsonSerializerOptions _options;

    public SystemTextJsonSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? JsonConverter.DefaultOptions;
    }

    public string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, _options);
    }

    public T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options);
    }

    public object? Deserialize(string json, Type type)
    {
        return JsonSerializer.Deserialize(json, type, _options);
    }
}