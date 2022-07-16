using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pose.Tests
{
    [TestClass]
    public class EndToEnd
    {
        [TestMethod]
        public void Test()
        {
            TextWriter writer = Console.Out;
            // Arrange
            Shim consoleShim = Shim
                .Replace(() => Console.WriteLine(Is.A<string>()))
                .With(delegate (string s) { Console.WriteLine("Hijacked: {0}", s); });
            // Act

            PoseContext.Isolate(() =>
            {
                Console.WriteLine("Hello world");
            }, consoleShim);

            // Assert
            Assert.AreEqual("Hijacked: Hello World", writer.ToString());
        }

        [TestMethod]
        public void TestPrivateStaticMethod()
        {
            // Arrange
            Shim shim = Shim
                .Replace(() => ClassWithStaticMethod.StaticMethod(Is.A<string>()))
                .With(delegate (string parameter) { return true; });

            bool result = false;

            ClassWithStaticMethod classTested = new ClassWithStaticMethod();

            // Act
            PoseContext.Isolate(() =>
            {
                result = classTested.ExposedMethod("");
            }, shim);

            // Assert
            Assert.IsTrue(result);
        }
    }
}
