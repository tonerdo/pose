using System;
using System.Collections.Generic;
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
        public void TestStaticMethodRewrite()
        {
            MethodInfo methodInfo = typeof(DateTime).GetMethod("get_Now");
            MethodRewriter methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);
            DynamicMethod dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;

            Delegate func = dynamicMethod.CreateDelegate(typeof(Func<DateTime>));
            Assert.AreEqual(DateTime.Now.ToString("yyyyMMdd_HHmm"), ((DateTime)func.DynamicInvoke()).ToString("yyyyMMdd_HHmm"));
        }

        [TestMethod]
        public void TestInstanceMethodRewrite()
        {
            string item = "Item 1";
            List<string> list = new List<string>();
            MethodInfo methodInfo = typeof(List<string>).GetMethod("Add");
            MethodRewriter methodRewriter = MethodRewriter.CreateRewriter(methodInfo, false);
            DynamicMethod dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;

            Delegate func = dynamicMethod.CreateDelegate(typeof(Action<List<string>, string>));
            func.DynamicInvoke(list, item);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(item, list[0]);
        }

        [TestMethod]
        public void TestConstructorRewrite()
        {
            List<string> list = new List<string>();
            ConstructorInfo constructorInfo = typeof(List<string>).GetConstructor(Type.EmptyTypes);
            MethodRewriter methodRewriter = MethodRewriter.CreateRewriter(constructorInfo, false);
            DynamicMethod dynamicMethod = methodRewriter.Rewrite() as DynamicMethod;

            Assert.AreEqual(typeof(void), dynamicMethod.ReturnType);
            Assert.AreEqual(typeof(List<string>), dynamicMethod.GetParameters()[0].ParameterType);
        }
    }
}