using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Focks.Extensions
{
    internal static class DictionaryExtensions
    {
        public static bool TryAdd(this Dictionary<int, Label> dictionary, int key, Label value)
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