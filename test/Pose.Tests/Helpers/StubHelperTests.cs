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

        [TestMethod]
        public void TestGetShimInstance()
        {
            Action action = new Action(() => Console.Clear());
            Shim shim = Shim.Replace(() => Console.Clear()).With(action);
            new IsolationContext(() => { }, shim);

            Assert.AreEqual(action.Target, StubHelper.GetShimInstance(0));
            Assert.AreSame(action.Target, StubHelper.GetShimInstance(0));
        }
    }
}
