using System;

namespace Pose
{
    public static class Of
    {
        public static T Type<T>() => default(T);
    }
}