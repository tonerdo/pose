using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Focks.Helpers
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
    }
}