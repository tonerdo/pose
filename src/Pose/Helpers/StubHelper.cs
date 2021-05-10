using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Pose.Extensions;

namespace Pose.Helpers
{
    internal static class StubHelper
    {
        public static IntPtr GetMethodPointer(MethodBase method)
        {
            if (method is DynamicMethod)
            {
                var methodDescriptior = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
                return ((RuntimeMethodHandle)methodDescriptior.Invoke(method as DynamicMethod, null)).GetFunctionPointer();
            }

            return method.MethodHandle.GetFunctionPointer();
        }

        public static object GetShimDelegateTarget(int index)
            => PoseContext.Shims[index].Replacement.Target;

        public static MethodInfo GetShimReplacementMethod(int index)
            => PoseContext.Shims[index].Replacement.Method;

        public static int GetIndexOfMatchingShim(MethodBase methodBase, Type type, object obj)
        {
            if (methodBase.IsStatic || obj == null)
                return Array.FindIndex(PoseContext.Shims, s => s.Original == methodBase);

            int index = Array.FindIndex(PoseContext.Shims,
                s => Object.ReferenceEquals(obj, s.Instance) && s.Original == methodBase);

            if (index == -1)
                return Array.FindIndex(PoseContext.Shims,
                            s => SignatureEquals(s, type, methodBase) && s.Instance == null);

            return index;
        }

        public static int GetIndexOfMatchingShim(MethodBase methodBase, object obj)
            => GetIndexOfMatchingShim(methodBase, methodBase.DeclaringType, obj);

        public static MethodInfo DevirtualizeMethod(object obj, MethodInfo virtualMethod)
        {
            return DevirtualizeMethod(obj.GetType(), virtualMethod);
        }

        public static MethodInfo DevirtualizeMethod(Type thisType, MethodInfo virtualMethod)
        {
            if (thisType == virtualMethod.DeclaringType) return virtualMethod;
            BindingFlags bindingFlags = BindingFlags.Instance | (virtualMethod.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic);
            Type[] types = virtualMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            return thisType.GetMethod(virtualMethod.Name, bindingFlags, null, types, null);
        }

        public static Module GetOwningModule() => typeof(StubHelper).Module;

        public static bool IsIntrinsic(MethodBase method)
        {
            return method.CustomAttributes.Any(ca => ca.AttributeType.FullName == "System.Runtime.CompilerServices.IntrinsicAttribute") ||
                    method.DeclaringType.CustomAttributes.Any(ca => ca.AttributeType.FullName == "System.Runtime.CompilerServices.IntrinsicAttribute") ||
                    method.DeclaringType.FullName.StartsWith("System.Runtime.Intrinsics");
        }

        public static string CreateStubNameFromMethod(string prefix, MethodBase method)
        {
            string name = prefix;
            name += "_";
            name += method.DeclaringType.ToString();
            name += "_";
            name += method.Name;

            if (!method.IsConstructor)
            {
                var genericArguments = method.GetGenericArguments();
                if (genericArguments.Length > 0)
                {
                    name += "[";
                    name += string.Join(',', genericArguments.Select(g => g.Name));
                    name += "]";
                }
            }

            return name;
        }

        private static bool SignatureEquals(Shim shim, Type type, MethodBase method)
        {
            if (shim.Type == null || type == shim.Type)
                return $"{shim.Type}::{shim.Original.ToString()}" == $"{type}::{method.ToString()}";

            if (type.IsSubclassOf(shim.Type))
            {
                if ((shim.Original.IsAbstract || !shim.Original.IsVirtual)
                        || (shim.Original.IsVirtual && !method.IsOverride()))
                {
                    return $"{shim.Original.ToString()}" == $"{method.ToString()}";
                }
            }

            return false;
        }
    }
}