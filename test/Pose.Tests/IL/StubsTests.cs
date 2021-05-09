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

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 2);
            Assert.AreEqual(methodInfo.GetParameters()[0].ParameterType, dynamicMethod.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(RuntimeMethodHandle), dynamicMethod.GetParameters()[count - 2].ParameterType);
            Assert.AreEqual(typeof(RuntimeTypeHandle), dynamicMethod.GetParameters()[count - 1].ParameterType);
        }

        [TestMethod]
        public void TestGenerateStubForInstanceMethod()
        {
            MethodInfo methodInfo = typeof(List<string>).GetMethod("Add");
            DynamicMethod dynamicMethod = Stubs.GenerateStubForDirectCall(methodInfo);
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
            DynamicMethod dynamicMethod = Stubs.GenerateStubForVirtualCall(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(methodInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 3);
            Assert.AreEqual(typeof(List<string>), dynamicMethod.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(RuntimeMethodHandle), dynamicMethod.GetParameters()[count - 2].ParameterType);
            Assert.AreEqual(typeof(RuntimeTypeHandle), dynamicMethod.GetParameters()[count - 1].ParameterType);
        }

        [TestMethod]
        public void TestGenerateStubForConstructorNewobj()
        {
            ConstructorInfo constructorInfo = typeof(List<string>).GetConstructor(Type.EmptyTypes);
            DynamicMethod dynamicMethod = Stubs.GenerateStubForConstructor(constructorInfo, OpCodes.Newobj, false);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(constructorInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 2);
            Assert.AreEqual(typeof(List<string>), dynamicMethod.ReturnType);
            Assert.AreEqual(typeof(RuntimeMethodHandle), dynamicMethod.GetParameters()[count - 2].ParameterType);
            Assert.AreEqual(typeof(RuntimeTypeHandle), dynamicMethod.GetParameters()[count - 1].ParameterType);
        }

        [TestMethod]
        public void TestGenerateStubForConstructorCall()
        {
            ConstructorInfo constructorInfo = typeof(List<string>).GetConstructor(Type.EmptyTypes);
            DynamicMethod dynamicMethod = Stubs.GenerateStubForConstructor(constructorInfo, OpCodes.Call, false);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(constructorInfo.GetParameters().Length, dynamicMethod.GetParameters().Length - 3);
            Assert.AreEqual(typeof(List<string>), dynamicMethod.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(void), dynamicMethod.ReturnType);
            Assert.AreEqual(typeof(RuntimeMethodHandle), dynamicMethod.GetParameters()[count - 2].ParameterType);
            Assert.AreEqual(typeof(RuntimeTypeHandle), dynamicMethod.GetParameters()[count - 1].ParameterType);
        }

        [TestMethod]
        public void TestGenerateStubForMethodPointer()
        {
            MethodInfo methodInfo = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
            DynamicMethod dynamicMethod = Stubs.GenerateStubForMethodPointer(methodInfo);
            int count = dynamicMethod.GetParameters().Length;

            Assert.AreEqual(2, dynamicMethod.GetParameters().Length);
            Assert.AreEqual(typeof(IntPtr), dynamicMethod.ReturnType);
            Assert.AreEqual(typeof(RuntimeMethodHandle), dynamicMethod.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(RuntimeTypeHandle), dynamicMethod.GetParameters()[1].ParameterType);
        }
    }
}