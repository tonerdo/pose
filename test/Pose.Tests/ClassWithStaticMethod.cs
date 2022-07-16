namespace Pose.Tests
{
    internal class ClassWithStaticMethod
    {
        internal bool ExposedMethod(string parameter)
        {
            return StaticMethod(parameter);
        }

        internal static bool StaticMethod(string parameter)
        {
            return false;
        }
    }
}
