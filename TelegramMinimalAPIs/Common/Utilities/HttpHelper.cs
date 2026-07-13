using System.Text.Json;

namespace FlutterBackendCSharp.Common.Utilities
{
    public static class HttpHelper
    {
        public static object? GetObjectValue(object obj)
        {
            var typeOfObject = ((JsonElement)obj).ValueKind;

            switch (typeOfObject)
            {
                case JsonValueKind.Number:
                    if (long.TryParse(typeOfObject.ToString(), out long value))
                    {
                        return value;
                    }
                    return 0;

                case JsonValueKind.Array:
                    List<string> list = new List<string>();
                    foreach (JsonElement element in ((JsonElement)obj).EnumerateArray())
                    {
                        // Extract the value and add it to the list
                        string? newString = element.GetString();
                        if (newString != null)
                        {
                            list.Add(newString);
                        }
                    }
                    return list;

                case JsonValueKind.String:
                    return obj.ToString();

                default:
                    return null;
            }
        }
    }
}
