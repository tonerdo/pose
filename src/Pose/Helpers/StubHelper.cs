using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Pose.Helpers
{
    internal static class StubHelper
    {
        public static IntPtr GetMethodPointer(MethodBase methodBase)
        {
            if (methodBase is DynamicMethod)
            {
                var methodDescriptior = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
                return ((RuntimeMethodHandle)methodDescriptior.Invoke(methodBase as DynamicMethod, null)).GetFunctionPointer();
            }

            return methodBase.MethodHandle.GetFunctionPointer();
        }

        public static object GetShimInstance(int index)
            => PoseContext.Shims[index].Replacement.Target;

        public static MethodInfo GetShimReplacementMethod(int index)
            => PoseContext.Shims[index].Replacement.Method;

        public static int GetMatchingShimIndex(MethodInfo methodInfo, object obj)
        {
            if (methodInfo.IsStatic || obj == null)
                return Array.FindIndex(PoseContext.Shims, s => s.Original == methodInfo);

            int index = Array.FindIndex(PoseContext.Shims,
                s => Object.ReferenceEquals(obj, s.Instance) && s.Original == methodInfo);

            if (index == -1)
                return Array.FindIndex(PoseContext.Shims,
                            s => s.Original == methodInfo && s.Instance == null);

            return index;
        }

        public static MethodInfo GetRuntimeMethodForVirtual(Type type, MethodInfo methodInfo)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | (methodInfo.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic);
            Type[] types = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            return type.GetMethod(methodInfo.Name, bindingFlags, null, types, null);
        }

        public static Module GetOwningModule()
            => Assembly.GetAssembly(typeof(StubHelper)).Modules.FirstOrDefault();
    }
}