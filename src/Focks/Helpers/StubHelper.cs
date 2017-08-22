using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Focks.Helpers
{
    internal static class StubHelper
    {
        public static IntPtr GetMethodPointer(MethodInfo methodInfo)
        {
            if (methodInfo is DynamicMethod)
            {
                var methodDescriptior = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
                return ((RuntimeMethodHandle)methodDescriptior.Invoke(methodInfo as DynamicMethod, null)).GetFunctionPointer();
            }

            return methodInfo.MethodHandle.GetFunctionPointer();
        }
    }
}