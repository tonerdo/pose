using System;
using System.Reflection;

namespace Pose.Extensions
{
    internal static class MethodBaseExtensions
    {
        public static bool InCoreLibrary(this MethodBase methodBase)
        {
            return methodBase.DeclaringType.Assembly == typeof(Exception).Assembly;
        }

        public static bool IsOverride(this MethodBase methodBase)
        {
            if (!(methodBase is MethodInfo methodInfo))
                return false;

            return methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType;
        }
    }
}