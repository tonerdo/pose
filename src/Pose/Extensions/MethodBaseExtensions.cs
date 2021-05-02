using System;
using System.Reflection;

namespace Pose.Extensions
{
    internal static class MethodBaseExtensions
    {
        public static bool IsForValueType(this MethodBase methodBase) => methodBase.DeclaringType.IsValueType;

        public static bool IsOverride(this MethodBase methodBase)
        {
            if (!(methodBase is MethodInfo methodInfo))
                return false;

            return methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType;
        }
    }
}