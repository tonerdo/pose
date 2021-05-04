using System;
using System.Reflection;

namespace Pose.Extensions
{
    internal static class MethodBaseExtensions
    {
        public static bool IsForValueType(this MethodBase methodBase) => methodBase.DeclaringType.IsValueType;

        public static bool InSystemAssembly(this MethodBase methodBase)
        {
            return methodBase.DeclaringType.Assembly.FullName.StartsWith("System.Private.CoreLib");
        }

        public static bool IsOverride(this MethodBase methodBase)
        {
            if (!(methodBase is MethodInfo methodInfo))
                return false;

            return methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType;
        }
    }
}