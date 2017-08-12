using System;

namespace Focks
{
    public static class Of
    {
        public static T Type<T>() => default(T);
    }
}