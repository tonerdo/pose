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
    public class StubsTests
    {
        [TestMethod]
        public void TestGenerateStubForStaticMethod()
        {
            MethodInfo methodInfo = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
            DynamicMethod dynamicMethod = Stubs.GenerateStubForMethod(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 2);
            Assert.AreEqual(methodInfo.GetParameters()[0].ParameterType, dynamicMethod.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(RuntimeMethodHandle), dynamicMethod.GetParameters()[count - 2].ParameterType);
            Assert.AreEqual(typeof(RuntimeTypeHandle), dynamicMethod.GetParameters()[count - 1].ParameterType);
        }

        [TestMethod]
        public void TestGenerateStubForInstanceMethod()
        {
            MethodInfo methodInfo = typeof(List<string>).GetMethod("Add");
            DynamicMethod dynamicMethod = Stubs.GenerateStubForMethod(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 3);
            Assert.AreEqual(typeof(List<string>), dynamicMethod.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(RuntimeMethodHandle), dynamicMethod.GetParameters()[count - 2].ParameterType);
            Assert.AreEqual(typeof(RuntimeTypeHandle), dynamicMethod.GetParameters()[count - 1].ParameterType);
        }

        [TestMethod]
        public void TestGenerateStubForVirtualMethod()
        {
            MethodInfo methodInfo = typeof(List<string>).GetMethod("Add");
            DynamicMethod dynamicMethod = Stubs.GenerateStubForVirtualMethod(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 3);
            Assert.AreEqual(typeof(List<string>), dynamicMethod.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(RuntimeMethodHandle), dynamicMethod.GetParameters()[count - 2].ParameterType);
            Assert.AreEqual(typeof(RuntimeTypeHandle), dynamicMethod.GetParameters()[count - 1].ParameterType);
        }
    }
}