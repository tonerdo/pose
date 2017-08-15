using System;
using System.Linq;
using System.Reflection;

namespace Focks.Extensions
{
    internal static class MethodBaseExtensions
    {
        public static string ToFullString(this MethodBase methodBase)
        {
            Type declaringType = methodBase.DeclaringType;
            return $"[{declaringType.Assembly.GetName().Name}]{declaringType.FullName}::{methodBase.ToString()}";
        }

        public static bool HasShim(this MethodBase method, Shim[] shims)
            => shims.Select(s => s.Original.ToFullString()).Contains(method.ToFullString());
    }
}