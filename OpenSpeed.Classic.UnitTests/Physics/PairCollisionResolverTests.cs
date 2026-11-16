using System.Collections.Generic;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class PairCollisionResolverTests
    {
        [TestCase(false, false, "contact")]
        [TestCase(true, false, "contact,impulse")]
        public void GivenAnInitialRejection_WhenResolving_ThenOnlyEventMagnitudesAreCleared(
            bool hasContact,
            bool wasImpulseApplied,
            string expectedCalls)
        {
            CarMemory first = new();
            CarMemory second = new();
            first[0x160] = 42;
            second[0x160] = 32;
            first[0x164] = 613;
            second[0x168] = 16;
            List<string> calls = [];

            bool wasResolved = PairCollisionResolver.Resolve(
                first, second,
                (firstBody, secondBody) =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(firstBody[0x160], Is.Zero);
                        Assert.That(secondBody[0x160], Is.Zero);
                    }

                    calls.Add("contact");

                    return hasContact;
                },
                (firstBody, secondBody) =>
                {
                    calls.Add("impulse");

                    return wasImpulseApplied;
                },
                body => Assert.Fail("No refresh is expected."),
                body => Assert.Fail("No fallback is expected."),
                (firstBody, secondBody) => Assert.Fail("No completion is expected."));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wasResolved, Is.False);
                Assert.That(string.Join(",", calls), Is.EqualTo(expectedCalls));
                Assert.That(first[0x164], Is.EqualTo(613));
                Assert.That(second[0x168], Is.EqualTo(16));
            }

        }

        [TestCase(1, 0, 1, 0)]
        [TestCase(2, 0, 2, 0)]
        [TestCase(20, 0, 9, 2)]
        [TestCase(20, 2, 2, 0)]
        public void GivenRepeatedContacts_WhenResolving_ThenNativeIterationAndFallbackLimitsArePreserved(
            int acceptedContacts,
            int rejectedImpulse,
            int expectedImpulses,
            int expectedFallbacks)
        {
            CarMemory first = new();
            CarMemory second = new();
            int contacts = 0;
            int impulses = 0;
            int refreshes = 0;
            int fallbacks = 0;
            int completions = 0;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PairCollisionResolver.Resolve(
                            first, second,
                            (firstBody, secondBody) =>
                            {
                                contacts += 1;

                                return contacts <= acceptedContacts;
                            },
                            (firstBody, secondBody) =>
                            {
                                impulses += 1;

                                return impulses != rejectedImpulse;
                            },
                            body => refreshes += 1,
                            body =>
                            {
                                Assert.That(impulses, Is.EqualTo(9));
                                Assert.That(refreshes, Is.EqualTo(18));
                                fallbacks += 1;
                            },
                            (firstBody, secondBody) => completions += 1));

                Assert.That(impulses, Is.EqualTo(expectedImpulses));
                Assert.That(fallbacks, Is.EqualTo(expectedFallbacks));
            }


            if (rejectedImpulse != 0)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(refreshes, Is.EqualTo(2));
                    Assert.That(completions, Is.Zero);
                }


                return;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refreshes, Is.EqualTo(expectedImpulses * 2));
                Assert.That(completions, Is.EqualTo(1));
                Assert.That(contacts, Is.EqualTo(expectedImpulses + 1));
            }

        }
    }
}