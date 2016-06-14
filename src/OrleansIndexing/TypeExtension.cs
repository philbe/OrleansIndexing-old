using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    public static class TypeExtensions
    {
        public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
        {
            return givenType.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == genericType) ||
                   givenType.BaseType != null && (givenType.BaseType.IsGenericType && givenType.BaseType.GetGenericTypeDefinition() == genericType ||
                                                  givenType.BaseType.IsAssignableToGenericType(genericType));
        }

        public static Type GetGenericType(this Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return it;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return givenType;

            Type baseType = givenType.BaseType;
            if (baseType == null) return null;

            return GetGenericType(baseType, genericType);
        }
    }
}
