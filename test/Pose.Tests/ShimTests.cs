using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

using Pose.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pose.Tests
{
    [TestClass]
    public class ShimTests
    {
        [TestMethod]
        public void TestReplace()
        {
            Shim shim = Shim.Replace(() => Console.WriteLine(""));

            Assert.AreEqual(typeof(Console).GetMethod("WriteLine", new[] { typeof(string) }), shim.Original);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestReplaceWithInstanceVariable()
        {
            ShimTests shimTests = new ShimTests();
            Shim shim = Shim.Replace(() => shimTests.TestReplace());

            Assert.AreEqual(typeof(ShimTests).GetMethod("TestReplace"), shim.Original);
            Assert.AreSame(shimTests, shim.Instance);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestShimReplaceWithInvalidSignature()
        {
            ShimTests shimTests = new ShimTests();
            Shim shim = Shim.Replace(() => shimTests.TestReplace());
            Assert.ThrowsException<InvalidShimSignatureException>(
                () => Shim.Replace(() => shimTests.TestReplace()).With(() => { }));
            Assert.ThrowsException<InvalidShimSignatureException>(
                () => Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(() => { }));
        }

        [TestMethod]
        public void TestShimReplaceWith()
        {
            ShimTests shimTests = new ShimTests();
            Action action = new Action(() => { });
            Action<ShimTests> actionInstance = new Action<ShimTests>((s) => { });

            Shim shim = Shim.Replace(() => Console.WriteLine()).With(action);
            Shim shim1 = Shim.Replace(() => shimTests.TestReplace()).With(actionInstance);

            Assert.AreEqual(typeof(Console).GetMethod("WriteLine", Type.EmptyTypes), shim.Original);
            Assert.AreEqual(action, shim.Replacement);

            Assert.AreEqual(typeof(ShimTests).GetMethod("TestReplace"), shim1.Original);
            Assert.AreSame(shimTests, shim1.Instance);
            Assert.AreEqual(actionInstance, shim1.Replacement);
        }

        [TestMethod]
        public void TestReplacePropertyGetter()
        {
            Shim shim = Shim.Replace(() => Thread.CurrentThread.CurrentCulture);

            Assert.AreEqual(typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo)).GetMethod, shim.Original);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestReplacePropertySetter()
        {
            Shim shim = Shim.Replace(() => Is.A<Thread>().CurrentCulture, true);

            Assert.AreEqual(typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo)).SetMethod, shim.Original);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestReplacePropertySetterAction()
        {
            var getterExecuted = false;
            var getterShim = Shim.Replace(() => Is.A<Thread>().CurrentCulture)
                .With((Thread t) =>
                {
                    getterExecuted = true;
                    return t.CurrentCulture;
                });
            var setterExecuted = false;
            var setterShim = Shim.Replace(() => Is.A<Thread>().CurrentCulture, true)
                .With((Thread t, CultureInfo value) =>
                {
                    setterExecuted = true;
                    t.CurrentCulture = value;
                });

            var currentCultureProperty = typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo));
            Assert.AreEqual(currentCultureProperty.GetMethod, getterShim.Original);
            Assert.AreEqual(currentCultureProperty.SetMethod, setterShim.Original);

            PoseContext.Isolate(() =>
            {
                var oldCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }, getterShim, setterShim);

            Assert.IsTrue(getterExecuted, "Getter not executed");
            Assert.IsTrue(setterExecuted, "Setter not executed");
        }

        [TestMethod]
        public void TestReplaceConstructor()
        {
            var dummyShim = Shim.Replace<DummyForConstructorBase>(() => new DummyForConstructor()).With(() => new DummyForConstructorReplacment());
            bool? wasCalled = false;

            PoseContext.Isolate(() =>
            {
                wasCalled = new DummyHolder().Dummy.ConstructorCalled;
            }, dummyShim);

            Assert.IsNotNull(wasCalled);
            Assert.IsFalse(wasCalled.Value);
        }

        [TestMethod]
        public void TestReplaceConstructorWithoutGeneric()
        {
            var dummyShim = Shim.Replace(() => new DummyForConstructor()).With(() => new DummyForConstructorReplacment());
            bool? wasCalled = false;

            PoseContext.Isolate(() =>
            {
                wasCalled = new DummyHolder().Dummy.ConstructorCalled;
            }, dummyShim);

            Assert.IsNotNull(wasCalled);
            Assert.IsFalse(wasCalled.Value);
        }

        [TestMethod]
        public void TestReplaceConstructorWithoutGeneric2()
        {
            var dummyShim = Shim.Replace(() => new DummyForConstructorReplacment()).With(() => new DummyForConstructorReplacment(true));

            PoseContext.Isolate(() =>
            {
                var unused = new DummyForConstructorReplacment();
            }, dummyShim);
        }

        [TestMethod]
        public void TestReplaceConstructorWithIncorrectTypes()
        {
            List<string> Replacement() => new List<string>();
            var exception = Assert.ThrowsException<InvalidShimSignatureException>(() => Shim.Replace<DummyForConstructorBase>(() => new DummyForConstructor()).With(Replacement));
            Assert.AreEqual("Mismatched construction types", exception.Message);
        }

        private class DummyHolder
        {
            public DummyHolder()
            {
                Dummy = new DummyForConstructor();
            }

            public DummyForConstructorBase Dummy { get; }
        }

        private abstract class DummyForConstructorBase
        {
            public bool? ConstructorCalled { get; protected set; }
        }

        private class DummyForConstructor : DummyForConstructorBase
        {
            public DummyForConstructor()
            {
                ConstructorCalled = true;
            }
        }

        private class DummyForConstructorReplacment : DummyForConstructorBase
        {
            public DummyForConstructorReplacment()
            {
                ConstructorCalled = false;
            }

            public DummyForConstructorReplacment(bool dummyFlag)
            {
                ConstructorCalled = false;
            }
        }
    }
}
