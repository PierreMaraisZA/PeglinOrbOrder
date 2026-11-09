using System.Collections.Generic;
using System.Linq;
using PeglinOrbOrder.Core;
using Xunit;

namespace PeglinOrbOrder.Tests
{
    public class OrbWeightTableTests
    {
        [Fact]
        public void Defaults_CoverEveryShippedOrb()
        {
            Assert.Equal(110, DefaultWeights.All.Count);
            Assert.Equal(110, OrbWeightTable.Defaults().Count);
        }

        [Fact]
        public void Defaults_AreAllInsideTheDocumentedRange()
        {
            Assert.All(DefaultWeights.All, pair => Assert.InRange(pair.Value, OrbWeightTable.MinWeight, OrbWeightTable.MaxWeight));
        }

        [Fact]
        public void PebballAndPlainDamage_ShareTheBaselineWeight()
        {
            OrbWeightTable table = OrbWeightTable.Defaults();

            Assert.Equal(50, table.For("stone"));
            Assert.Equal(50, table.For("sworb"));
            Assert.Equal(50, table.For("warhammer"));
            Assert.Equal(50, table.For("dagger"));
        }

        [Fact]
        public void SupportOrbs_OutweighPebball()
        {
            OrbWeightTable table = OrbWeightTable.Defaults();

            Assert.True(table.For("instigatorb") > table.For("stone"));
            Assert.True(table.For("poison_orb") > table.For("stone"));
            Assert.True(table.For("blind") > table.For("stone"));
        }

        [Fact]
        public void UnknownOrb_FallsBackToTheDefaultWeight()
        {
            OrbWeightTable table = new OrbWeightTable(new Dictionary<string, int> { { "stone", 80 } }, 42);

            Assert.Equal(42, table.For("an_orb_added_next_patch"));
            Assert.Equal(80, table.For("stone"));
            Assert.Equal(42, table.For("an_orb_added_next_patch"));
        }

        [Fact]
        public void NullOrEmptyKey_UsesTheDefaultWeight()
        {
            OrbWeightTable table = new OrbWeightTable(null, 37);

            Assert.Equal(37, table.For(null));
            Assert.Equal(37, table.For(string.Empty));
            Assert.Equal(0, table.Count);
        }

        [Fact]
        public void Weights_AreClampedToTheDocumentedRange()
        {
            OrbWeightTable table = new OrbWeightTable(new Dictionary<string, int>
            {
                { "too_high", 900 },
                { "too_low", -20 },
            });

            Assert.Equal(OrbWeightTable.MaxWeight, table.For("too_high"));
            Assert.Equal(OrbWeightTable.MinWeight, table.For("too_low"));
        }

        [Fact]
        public void Keys_AreCaseInsensitive()
        {
            OrbWeightTable table = new OrbWeightTable(new Dictionary<string, int> { { "stone", 61 } });

            Assert.Equal(61, table.For("STONE"));
            Assert.Equal(61, table.For("Stone"));
        }

        [Fact]
        public void EmptyKeys_AreDropped()
        {
            OrbWeightTable table = new OrbWeightTable(new Dictionary<string, int> { { string.Empty, 90 }, { "stone", 50 } });

            Assert.Equal(1, table.Count);
        }


        [Fact]
        public void DefaultForUnknown_IsClamped()
        {
            Assert.Equal(OrbWeightTable.MaxWeight, new OrbWeightTable(null, 500).DefaultForUnknown);
            Assert.Equal(OrbWeightTable.MinWeight, new OrbWeightTable(null, -1).DefaultForUnknown);
        }
    }
}

