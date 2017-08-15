using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Focks.Extensions
{
    internal static class DictionaryExtensions
    {
        public static bool TryAdd<T, U>(this Dictionary<T, U> dictionary, T key, U value)
        {
            try
            {
                dictionary.Add(key, value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}