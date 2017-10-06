using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using Pose.Helpers;
using Pose.IL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pose.Tests
{
    [TestClass]
    public class MethodRewriterTests
    {
        [TestMethod]
        public void TestRewrite()
        {
            MethodInfo methodInfo = typeof(StubHelper).GetMethod("GetMethodPointer");
            MethodRewriter methodRewriter = MethodRewriter.CreateRewriter(methodInfo);
            DynamicMethod dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;

            Delegate func = dynamicMethod.CreateDelegate(typeof(Func<MethodBase, IntPtr>));
            Assert.AreEqual(StubHelper.GetMethodPointer(methodInfo), func.DynamicInvoke(methodInfo));
        }
    }
}