using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Focks.Helpers
{
    internal static class StubHelper
    {
        public static string GetMethodTypeArguments(MethodInfo methodInfo)
        {
            if (!methodInfo.IsGenericMethod)
                return string.Empty;

            string name = "[";
            name += string.Join<Type>(",", methodInfo.GetGenericArguments());
            name += "]";
            return name;
        }

        public static MethodInfo FindMethod(Type type, int metadataToken, string genericArguments)
        {
            MethodInfo method = type.Module.ResolveMember(metadataToken) as MethodInfo;
            Type[] typeArguments = ParseGenericArguments(genericArguments);
            if (typeArguments.Length > 0)
                method = method.MakeGenericMethod(typeArguments);

            return method;
        }

        public static IntPtr GetMethodPointer(MethodInfo methodInfo)
        {
            if (methodInfo is DynamicMethod)
            {
                var methodDescriptior = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
                return ((RuntimeMethodHandle)methodDescriptior.Invoke(methodInfo as DynamicMethod, null)).GetFunctionPointer();
            }

            return methodInfo.MethodHandle.GetFunctionPointer();
        }

        private static Type[] ParseGenericArguments(string genericArguments)
        {
            Type[] typeArguments = new Type[0];
            int index = genericArguments.IndexOf("[");
            if (index == -1)
                return typeArguments;
            
            string generics = genericArguments.Substring(index);
            genericArguments = genericArguments.Substring(0, index);
            generics = generics.Replace("[", "").Replace("]", "");

            string[] split = generics.Split(','); 
            typeArguments = split.Select(s => FindType(IsolationContext.EntryPointType.Assembly, s)).ToArray();
            return typeArguments;
        }

        private static Type FindType(Assembly assembly, string type)
        {
            Type t = assembly.GetType(type);
            if (t != null)
                return t;
            
            foreach (var asm in assembly.GetReferencedAssemblies())
            {
                t = FindType(Assembly.Load(asm), type);
                if (t != null)
                    return t;
            }

            return null;
        }
    }
}