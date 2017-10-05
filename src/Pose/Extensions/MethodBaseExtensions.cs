using System;
using System.Reflection;

namespace Pose.Extensions
{
    internal static class MethodBaseExtensions
    {
        public static bool IsForValueType(this MethodBase methodBase) => methodBase.DeclaringType.IsSubclassOf(typeof(ValueType));
    }
}