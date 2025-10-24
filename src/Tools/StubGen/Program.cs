using System.Reflection;
using System.Text;

namespace StubGen;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 4 || args[0] != "--interface" || args[2] != "--out")
        {
            Console.WriteLine("Usage: StubGen --interface <FullTypeName> --out <path>");
            Console.WriteLine("Example: StubGen --interface Bridge.Abstractions.ITextOps --out py_src/netpy_ops/text.py");
            return 1;
        }

        var interfaceName = args[1];
        var outputPath = args[3];

        try
        {
            // Load the Bridge assembly to find the interface
            var bridgePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Bridge", "bin", "Debug", "net8.0", "Bridge.dll");
            var bridgeAssembly = Assembly.LoadFrom(bridgePath);
            var interfaceType = bridgeAssembly.GetType(interfaceName);
            if (interfaceType == null)
            {
                Console.WriteLine($"Error: Interface '{interfaceName}' not found");
                return 1;
            }

            if (!interfaceType.IsInterface)
            {
                Console.WriteLine($"Error: '{interfaceName}' is not an interface");
                return 1;
            }

            var stub = GeneratePythonStub(interfaceType);
            File.WriteAllText(outputPath, stub);

            Console.WriteLine($"Generated Python stub for '{interfaceName}' at '{outputPath}'");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static string GeneratePythonStub(Type interfaceType)
    {
        var sb = new StringBuilder();

        var className = interfaceType.Name.Substring(1);

        sb.AppendLine($"class {className}:");
        sb.AppendLine("    def __init__(self, env=None):");
        sb.AppendLine("        self.env = env");
        sb.AppendLine();

        var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            GenerateMethodStub(sb, method);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void GenerateMethodStub(StringBuilder sb, MethodInfo method)
    {
        var parameters = method.GetParameters();
        var paramList = new List<string>();

        foreach (var param in parameters)
        {
            var paramName = param.Name ?? "arg";
            var pythonType = MapCSharpTypeToPython(param.ParameterType);
            paramList.Add($"{paramName}: {pythonType}");
        }

        var returnType = MapCSharpTypeToPython(method.ReturnType);
        var paramString = string.Join(", ", paramList);

        sb.AppendLine($"    def {method.Name}(self, {paramString}) -> {returnType}:");
        sb.AppendLine("        # TODO: Implement this method");
        sb.AppendLine("        pass");
    }

    private static string MapCSharpTypeToPython(Type type)
    {
        if (type == typeof(string))
            return "str";
        if (type == typeof(int))
            return "int";
        if (type == typeof(long))
            return "int";
        if (type == typeof(double))
            return "float";
        if (type == typeof(float))
            return "float";
        if (type == typeof(bool))
            return "bool";
        if (type == typeof(Task))
            return "None";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            return MapCSharpTypeToPython(type.GetGenericArguments()[0]);
        if (type == typeof(CancellationToken))
            return "None";

        return "Any";
    }
}
