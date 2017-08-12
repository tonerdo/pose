using System;

namespace Focks
{
    public static class Arg
    {
        public static T Is<T>() => default(T);
    }
}