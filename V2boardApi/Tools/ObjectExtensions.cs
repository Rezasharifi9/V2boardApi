using System;
using System.Collections.Generic;
using System.Reflection;

public static class ObjectExtensions
{
    public static Dictionary<string, object> ToDictionary(this object obj)
    {
        Dictionary<string, object> dictionary = new Dictionary<string, object>();

        PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo property in properties)
        {
            if (property.CanRead)
            {
                var value = property.GetValue(obj, null);
                if (value != null && (value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(List<int>)))
                {
                    // اگر مقدار یک لیست باشد، آن را به یک لیست از آبجکت‌ها تبدیل کنید
                    var list = (List<int>)value;
                    dictionary[property.Name] = new List<int>(list);
                }
                else
                {
                    dictionary[property.Name] = value;
                }
            }
        }

        return dictionary;
    }
}
