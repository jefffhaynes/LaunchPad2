using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waveform;

namespace LaunchPad2.Test
{
    [TestClass]
    public class LinqTest
    {
        [TestMethod]
        public void ZipManyTest()
        {
            var manyCollections = new[]
            {
                new[] {1, 0, 3},
                new[] {1, 2, 0},
                new[] {0, 2, 3}
            };

            var zipped = manyCollections.ZipMany(c => c.Sum()).ToList();

            Assert.AreEqual(2, zipped[0]);
            Assert.AreEqual(4, zipped[1]);
            Assert.AreEqual(6, zipped[2]);
        }

        [TestMethod]
        public void ZipManyTransposeTest()
        {
            var manyCollections = new[]
            {
                new[] {1, 0, 3},
                new[] {1, 2, 0},
                new[] {0, 2, 3}
            };

            var zipped = manyCollections.ZipMany(c => c.Select(x => x)).ToList();

            Assert.AreEqual(2, zipped[0]);
            Assert.AreEqual(4, zipped[1]);
            Assert.AreEqual(6, zipped[2]);
        }
    }
}
