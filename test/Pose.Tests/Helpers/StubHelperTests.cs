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

        [TestMethod]
        public void TestGetShimReplacementMethod()
        {
            Action action = new Action(() => Console.Clear());
            Shim shim = Shim.Replace(() => Console.Clear()).With(action);
            new IsolationContext(() => { }, shim);

            Assert.AreEqual(action.Method, StubHelper.GetShimReplacementMethod(0));
            Assert.AreSame(action.Method, StubHelper.GetShimReplacementMethod(0));
        }

        [TestMethod]
        public void TestGetMatchingShimIndex()
        {
            StubHelperTests stubHelperTests = new StubHelperTests();
            Action staticAction = new Action(() => { });
            Action<StubHelperTests> instanceAction = new Action<StubHelperTests>((@this) => { });

            Shim shim = Shim.Replace(() => Console.Clear()).With(staticAction);
            Shim shim1 = Shim.Replace(() => Of.Type<StubHelperTests>().TestGetMatchingShimIndex()).With(instanceAction);
            Shim shim2 = Shim.Replace(() => stubHelperTests.TestGetMatchingShimIndex()).With(instanceAction);
            new IsolationContext(() => { }, shim, shim1, shim2);

            MethodInfo consoleMethodInfo = typeof(Console).GetMethod("Clear");
            MethodInfo stubMethodInfo = typeof(StubHelperTests).GetMethod("TestGetMatchingShimIndex");

            Assert.AreEqual(0, StubHelper.GetMatchingShimIndex(consoleMethodInfo, null));
            Assert.AreEqual(1, StubHelper.GetMatchingShimIndex(stubMethodInfo, new StubHelperTests()));
            Assert.AreEqual(2, StubHelper.GetMatchingShimIndex(stubMethodInfo, stubHelperTests));
        }
    }
}
