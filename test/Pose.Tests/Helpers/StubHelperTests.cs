using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using Pose.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pose.Tests
{
    [TestClass]
    public class StubHelperTests
    {
        [TestMethod]
        public void TestGetMethodPointer()
        {
            MethodInfo methodInfo = typeof(Console).GetMethod("Clear");
            DynamicMethod dynamicMethod
                = new DynamicMethod("Method", typeof(void), Type.EmptyTypes);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            Assert.AreNotEqual(IntPtr.Zero, StubHelper.GetMethodPointer(methodInfo));
            Assert.AreNotEqual(IntPtr.Zero, StubHelper.GetMethodPointer(dynamicMethod));
        }
    }
}
