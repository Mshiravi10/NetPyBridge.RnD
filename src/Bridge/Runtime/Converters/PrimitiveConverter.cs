using System.Text.Json;
using Python.Runtime;

namespace Bridge.Runtime.Converters;

public static class PrimitiveConverter
{
    public static PyObject ToPython(object? value)
    {
        if (value == null)
            return Python.Runtime.Runtime.None;

        using (Py.GIL())
        {
            return value switch
            {
                string str => str.ToPython(),
                int i => i.ToPython(),
                long l => l.ToPython(),
                double d => d.ToPython(),
                float f => f.ToPython(),
                bool b => b.ToPython(),
                _ => JsonConverter.ToPython(value)
            };
        }
    }

    public static T FromPython<T>(PyObject pyObject)
    {
        return (T)FromPython(pyObject, typeof(T))!;
    }

    public static object? FromPython(PyObject pyObject, Type targetType)
    {
        if (pyObject == null || pyObject.IsNone())
            return null;

        using (Py.GIL())
        {
            if (targetType == typeof(string))
                return pyObject.As<string>();

            if (targetType == typeof(int))
                return pyObject.As<int>();

            if (targetType == typeof(long))
                return pyObject.As<long>();

            if (targetType == typeof(double))
                return pyObject.As<double>();

            if (targetType == typeof(float))
                return pyObject.As<float>();

            if (targetType == typeof(bool))
                return pyObject.As<bool>();

            return JsonConverter.FromPython(pyObject, targetType);
        }
    }
}
