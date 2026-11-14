using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class PhysicsTablesTests
    {
        [Test]
        public void GivenExecutableTables_WhenHashing_ThenTheSpecificationHashesAreIdentical()
        {
            Assert.Multiple(() =>
            {
                Assert.That(PhysicsTables.QuarterWave.Length, Is.EqualTo(257));
                Assert.That(Hash(PhysicsTables.QuarterWave),
                    Is.EqualTo("c3b03a2581960f9b22f2f29fb52f30bf36eb87c3896c8e4f62c708b3c247f880"));
                Assert.That(PhysicsTables.RatioAngle.Length, Is.EqualTo(260));
                Assert.That(Convert.ToHexStringLower(SHA256.HashData(PhysicsTables.RatioAngle)),
                    Is.EqualTo("a1dfcf4f91dfde6bb331ca7f108becef3edd0168064b26dfcc8c1477fd33a259"));
                Assert.That(PhysicsTables.InterpolatedAngle.Length, Is.EqualTo(258));
                Assert.That(Hash(PhysicsTables.InterpolatedAngle),
                    Is.EqualTo("309cc3edcf94abb9ce99f7c16b6f89b1fcc6ed736df5a5c977203bc4cac422af"));
            });
        }

        private static string Hash(ReadOnlySpan<int> values)
        {
            byte[] bytes = new byte[values.Length * sizeof(int)];

            for (int index = 0; index < values.Length; index += 1)
            {
                BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(index * sizeof(int)), values[index]);
            }

            return Convert.ToHexStringLower(SHA256.HashData(bytes));
        }
    }
}