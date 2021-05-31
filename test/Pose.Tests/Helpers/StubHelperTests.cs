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
        public void TestGetShimDelegateTarget()
        {
            Action action = new Action(() => Console.Clear());
            Shim shim = Shim.Replace(() => Console.Clear()).With(action);
            PoseContext.Isolate(() => { }, shim);

            Assert.AreEqual(action.Target, StubHelper.GetShimDelegateTarget(0));
            Assert.AreSame(action.Target, StubHelper.GetShimDelegateTarget(0));
        }

        [TestMethod]
        public void TestGetShimReplacementMethod()
        {
            Action action = new Action(() => Console.Clear());
            Shim shim = Shim.Replace(() => Console.Clear()).With(action);
            PoseContext.Isolate(() => { }, shim);

            Assert.AreEqual(action.Method, StubHelper.GetShimReplacementMethod(0));
            Assert.AreSame(action.Method, StubHelper.GetShimReplacementMethod(0));
        }

        [TestMethod]
        public void TestGetIndexOfMatchingShim()
        {
            StubHelperTests stubHelperTests = new StubHelperTests();
            Action staticAction = new Action(() => { });
            Action<StubHelperTests> instanceAction = new Action<StubHelperTests>((@this) => { });

            Shim shim = Shim.Replace(() => Console.Clear()).With(staticAction);
            Shim shim1 = Shim.Replace(() => Is.A<StubHelperTests>().TestGetIndexOfMatchingShim()).With(instanceAction);
            Shim shim2 = Shim.Replace(() => stubHelperTests.TestGetIndexOfMatchingShim()).With(instanceAction);
            PoseContext.Isolate(() => { }, shim, shim1, shim2);

            MethodInfo consoleMethodInfo = typeof(Console).GetMethod("Clear");
            MethodInfo stubMethodInfo = typeof(StubHelperTests).GetMethod("TestGetIndexOfMatchingShim");

            Assert.AreEqual(0, StubHelper.GetIndexOfMatchingShim(consoleMethodInfo, null));
            Assert.AreEqual(1, StubHelper.GetIndexOfMatchingShim(stubMethodInfo, new StubHelperTests()));
            Assert.AreEqual(2, StubHelper.GetIndexOfMatchingShim(stubMethodInfo, stubHelperTests));
        }

        [TestMethod]
        public void TestGetRuntimeMethodForVirtual()
        {
            Type type = typeof(StubHelperTests);
            MethodInfo methodInfo = type.GetMethod("TestGetRuntimeMethodForVirtual");
            Assert.AreEqual(methodInfo, StubHelper.DevirtualizeMethod(type, methodInfo));
        }

        [TestMethod]
        public void TestGetOwningModule()
        {
            Assert.AreEqual(typeof(StubHelper).Module, StubHelper.GetOwningModule());
            Assert.AreNotEqual(typeof(StubHelperTests).Module, StubHelper.GetOwningModule());
        }
    }
}
