using System.Collections.Generic;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class RouteContactResetTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void GivenACorrectionDecision_WhenResetting_ThenOnlyAcceptedCorrectionsRefreshTheRoute(
            bool isCorrected)
        {
            CarMemory car = new();
            car[0x160] = 42;
            car[0x164] = 32;
            car[0x168] = 613;
            car[0xA0] = 16;
            List<string> calls = [];

            RouteContactReset.Update(
                car,
                body =>
                {
                    Assert.That(body, Is.SameAs(car));
                    Assert.That(body[0x160], Is.EqualTo(42));
                    calls.Add("correction");
                    body[0xA0] = 64;

                    return isCorrected;
                },
                body =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(body[0x160], Is.Zero);
                        Assert.That(body[0x164], Is.Zero);
                        Assert.That(body[0x168], Is.EqualTo(613));
                    }

                    calls.Add("query");
                },
                body => calls.Add("basis"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0x168], Is.EqualTo(613));
                Assert.That(car[0xA0], Is.EqualTo(64));
            }


            if (isCorrected)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(calls, Is.EqualTo(new[] { "correction", "query", "basis" }));
                    Assert.That(car[0x160], Is.Zero);
                    Assert.That(car[0x164], Is.Zero);
                }


                return;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(calls, Is.EqualTo(new[] { "correction" }));
                Assert.That(car[0x160], Is.EqualTo(42));
                Assert.That(car[0x164], Is.EqualTo(32));
            }

        }

        [Test]
        public void GivenAFailingRouteQuery_WhenResetting_ThenEventClearingPrecedesTheFailure()
        {
            CarMemory car = new();
            car[0x160] = 42;
            car[0x164] = 32;
            car[0x168] = 613;
            AssertionException failure = new("The route query failed.");

            Assert.That(
                () => RouteContactReset.Update(
                    car, body => true, body => throw failure,
                    body => Assert.Fail("The basis refresh must not execute.")),
                Throws.Exception.SameAs(failure));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0x160], Is.Zero);
                Assert.That(car[0x164], Is.Zero);
                Assert.That(car[0x168], Is.EqualTo(613));
            }

        }

        [Test]
        public void GivenMissingDependencies_WhenResetting_ThenArgumentErrorsAreReported()
        {
            CarMemory car = new();

            Assert.That(
                () => RouteContactReset.Update(null!, body => true, body => { }, body => { }),
                Throws.ArgumentNullException);
            Assert.That(
                () => RouteContactReset.Update(car, null!, body => { }, body => { }),
                Throws.ArgumentNullException);
            Assert.That(
                () => RouteContactReset.Update(car, body => true, null!, body => { }),
                Throws.ArgumentNullException);
            Assert.That(
                () => RouteContactReset.Update(car, body => true, body => { }, null!),
                Throws.ArgumentNullException);
        }
    }
}