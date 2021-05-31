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
            DynamicMethod dynamicMethod = Stubs.GenerateStubForDirectCall(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length);
            Assert.AreEqual(methodInfo.GetParameters()[0].ParameterType, dynamicMethod.GetParameters()[0].ParameterType);
        }

        [TestMethod]
        public void TestGenerateStubForInstanceMethod()
        {
            Type thisType = typeof(List<string>);
            MethodInfo methodInfo = thisType.GetMethod("Add");
            DynamicMethod dynamicMethod = Stubs.GenerateStubForDirectCall(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 1);
            Assert.AreEqual(thisType, dynamicMethod.GetParameters()[0].ParameterType);
        }

        [TestMethod]
        public void TestGenerateStubForVirtualMethod()
        {
            Type thisType = typeof(List<string>);
            MethodInfo methodInfo = thisType.GetMethod("Add");
            DynamicMethod dynamicMethod = Stubs.GenerateStubForVirtualCall(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 1);
            Assert.AreEqual(thisType, dynamicMethod.GetParameters()[0].ParameterType);
        }

        [TestMethod]
        public void TestGenerateStubForReferenceTypeConstructor()
        {
            Type thisType = typeof(List<string>);
            ConstructorInfo constructorInfo = thisType.GetConstructor(Type.EmptyTypes);
            DynamicMethod dynamicMethod = Stubs.GenerateStubForObjectInitialization(constructorInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(constructorInfo.GetParameters().Length, dynamicMethod.GetParameters().Length);
            Assert.AreEqual(thisType, dynamicMethod.ReturnType);
        }

        [TestMethod]
        public void TestGenerateStubForMethodPointer()
        {
            MethodInfo methodInfo = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
            DynamicMethod dynamicMethod = Stubs.GenerateStubForDirectLoad(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(0, dynamicMethod.GetParameters().Length);
            Assert.AreEqual(typeof(IntPtr), dynamicMethod.ReturnType);
        }
    }
}