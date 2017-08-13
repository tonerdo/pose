using System;
using System.Reflection;

namespace Focks
{
    internal static class Utilities
    {
        internal static string BuildMethodString(MethodBase methodBase)
        {
            Type declaringType = methodBase.DeclaringType;
            return $"[{declaringType.Assembly.GetName().Name}]{declaringType.FullName}::{methodBase.ToString()}";
        }
    }
}