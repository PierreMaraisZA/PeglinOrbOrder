using System;
using System.Collections.Generic;
using System.Linq;
using PeglinOrbOrder.Core;
using Xunit;

namespace PeglinOrbOrder.Tests
{
    public class WeightedOrderTests
    {
        private static OrbWeightTable Table(params (string Key, int Weight)[] pairs)
        {
            return new OrbWeightTable(pairs.ToDictionary(pair => pair.Key, pair => pair.Weight, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>A local generator, so the tests don't depend on System.Random's sequence staying the same across runtimes.</summary>
        private sealed class Lcg
        {
            private uint _state;

            public Lcg(uint seed)
            {
                _state = seed == 0u ? 1u : seed;
            }

            public double NextDouble()
            {
                _state = (_state * 1664525u) + 1013904223u;
                return _state / 4294967296.0;
            }
        }

        private static Func<double> Seeded(int seed)
        {
            Lcg random = new Lcg((uint)seed);
            return random.NextDouble;
        }

        private static List<string> Shuffle(IEnumerable<string> items, OrbWeightTable table, Func<double> nextUnit, double exponent = 1.0)
        {
            List<string> list = items.ToList();
            WeightedOrder.ShuffleInPlace(list, item => item, table, exponent, nextUnit);
            return list;
        }

        [Fact]
        public void EmptyAndSingleItem_AreLeftAlone()
        {
            List<string> empty = new List<string>();
            WeightedOrder.ShuffleInPlace(empty, item => item, Table(), 1.0, Seeded(1));
            Assert.Empty(empty);

            List<string> single = new List<string> { "stone" };
            WeightedOrder.ShuffleInPlace(single, item => item, Table(), 1.0, Seeded(1));
            Assert.Equal(new[] { "stone" }, single);
        }

        [Fact]
        public void Shuffle_IsAlwaysAPermutation()
        {
            string[] items = { "a", "b", "c", "d", "e" };
            List<string> result = Shuffle(items, Table(("a", 10), ("b", 90), ("c", 0), ("d", 50), ("e", 50)), Seeded(7));

            Assert.Equal(items.OrderBy(x => x), result.OrderBy(x => x));
        }

        [Fact]
        public void SameRandomSequence_ProducesTheSameOrder()
        {
            string[] items = { "a", "b", "c", "d" };
            OrbWeightTable table = Table(("a", 10), ("b", 90), ("c", 0), ("d", 50));

            List<string> first = Shuffle(items, table, Seeded(1234));
            List<string> second = Shuffle(items, table, Seeded(1234));

            Assert.Equal(first, second);
        }

        [Fact]
        public void ConsumesOneRandomNumberPerWeightedOrbAndNoneForZeroWeight()
        {
            int calls = 0;
            Func<double> counting = () =>
            {
                calls++;
                return 0.5;
            };

            Shuffle(new[] { "a", "b", "c", "d" }, Table(("a", 50), ("b", 90), ("c", 0), ("d", 10)), counting);

            Assert.Equal(3, calls); // "c" is weight 0, so it needs no roll
        }

        [Fact]
        public void ZeroWeight_IsNeverDrawnFirst()
        {
            OrbWeightTable table = Table(("stone", 50), ("bomb", 50), ("bramble", 0), ("shell", 0));
            string[] items = { "bramble", "shell", "stone", "bomb" };

            for (int i = 0; i < 200; i++)
            {
                List<string> result = Shuffle(items, table, Seeded(i));
                Assert.DoesNotContain(result[0], new[] { "bramble", "shell" });
            }
        }

        [Fact]
        public void MultipleZeroWeights_KeepTheirOriginalOrderAtTheEnd()
        {
            OrbWeightTable table = Table(("stone", 90), ("z1", 0), ("z2", 0), ("z3", 0));
            string[] items = { "z1", "stone", "z2", "z3" };

            for (int i = 0; i < 50; i++)
            {
                List<string> result = Shuffle(items, table, Seeded(i));
                Assert.Equal(new[] { "stone", "z1", "z2", "z3" }, result);
            }
        }

        [Fact]
        public void AllZeroWeights_LeaveTheOrderUntouched()
        {
            string[] items = { "a", "b", "c", "d" };
            List<string> result = Shuffle(items, Table(("a", 0), ("b", 0), ("c", 0), ("d", 0)), Seeded(3));

            Assert.Equal(items, result);
        }

        [Fact]
        public void A100WeightOpensAboutTwoThirdsOfTheTimeAgainstA50()
        {
            OrbWeightTable table = Table(("heavy", 100), ("stone", 50));
            int heavyFirst = 0;
            const int runs = 20000;

            for (int i = 0; i < runs; i++)
            {
                if (Shuffle(new[] { "heavy", "stone" }, table, Seeded(i))[0] == "heavy")
                {
                    heavyFirst++;
                }
            }

            double share = (double)heavyFirst / runs;
            Assert.InRange(share, 0.63, 0.70); // expected 2/3
        }

        [Fact]
        public void SupportOrbs_OpenMoreOftenThanPebballsInAMixedDeck()
        {
            OrbWeightTable table = OrbWeightTable.Defaults();
            string[] deck =
            {
                "stone", "stone", "stone", "stone", "stone", "stone", "stone",
                "poison_orb", "blind", "instigatorb",
            };
            int supportFirst = 0;
            const int runs = 20000;

            for (int i = 0; i < runs; i++)
            {
                string first = Shuffle(deck, table, Seeded(i))[0];
                if (first != "stone")
                {
                    supportFirst++;
                }
            }

            double share = (double)supportFirst / runs;
            // 95 + 90 + 100 = 285 weight of support against 7 * 50 = 350 of pebbles.
            Assert.InRange(share, 0.42, 0.48); // expected 285 / 635 = 44.9%
        }

        [Fact]
        public void ExponentZero_FlattensEveryWeight()
        {
            OrbWeightTable table = Table(("heavy", 100), ("stone", 50));
            int heavyFirst = 0;
            const int runs = 20000;

            for (int i = 0; i < runs; i++)
            {
                if (Shuffle(new[] { "heavy", "stone" }, table, Seeded(i), exponent: 0.0)[0] == "heavy")
                {
                    heavyFirst++;
                }
            }

            Assert.InRange((double)heavyFirst / runs, 0.47, 0.53); // a plain coin flip
        }

        [Fact]
        public void ExponentTwo_SharpensTheBias()
        {
            OrbWeightTable table = Table(("heavy", 100), ("stone", 50));
            int heavyFirst = 0;
            const int runs = 20000;

            for (int i = 0; i < runs; i++)
            {
                if (Shuffle(new[] { "heavy", "stone" }, table, Seeded(i), exponent: 2.0)[0] == "heavy")
                {
                    heavyFirst++;
                }
            }

            // 100^2 against 50^2 is 4:1.
            Assert.InRange((double)heavyFirst / runs, 0.77, 0.83);
        }

        [Fact]
        public void UnknownOrb_UsesTheDefaultWeight()
        {
            OrbWeightTable table = new OrbWeightTable(new Dictionary<string, int> { { "stone", 100 } }, 50);
            int stoneFirst = 0;
            const int runs = 20000;

            for (int i = 0; i < runs; i++)
            {
                if (Shuffle(new[] { "stone", "brand_new_orb" }, table, Seeded(i))[0] == "stone")
                {
                    stoneFirst++;
                }
            }

            Assert.InRange((double)stoneFirst / runs, 0.63, 0.70); // 100 vs the 50 default
        }

        [Fact]
        public void AZeroRandomValue_DoesNotThrow()
        {
            List<string> result = Shuffle(new[] { "a", "b" }, Table(("a", 50), ("b", 50)), () => 0.0);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void WeightIsNotATierSort()
        {
            // Any orb can open the battle, including the lightest one.
            OrbWeightTable table = Table(("heavy", 95), ("stone", 50), ("memorb", 35));
            HashSet<string> openers = new HashSet<string>();
            const int runs = 2000;

            for (int i = 0; i < runs; i++)
            {
                openers.Add(Shuffle(new[] { "heavy", "stone", "memorb" }, table, Seeded(i))[0]);
            }

            Assert.Contains("heavy", openers);
            Assert.Contains("stone", openers);
            Assert.Contains("memorb", openers);
        }

        [Fact]
        public void A35WeightOrbStillOpensAboutAQuarterOfTheTimeNextToA100()
        {
            OrbWeightTable table = Table(("heavy", 100), ("memorb", 35));
            int memorbFirst = 0;
            const int runs = 20000;

            for (int i = 0; i < runs; i++)
            {
                if (Shuffle(new[] { "heavy", "memorb" }, table, Seeded(i))[0] == "memorb")
                {
                    memorbFirst++;
                }
            }

            double share = (double)memorbFirst / runs;
            Assert.InRange(share, 0.22, 0.30); // 35 / 135 = 25.9%
        }

        [Fact]
        public void AHeavyOrbCanStillBeDrawnLast()
        {
            OrbWeightTable table = Table(("heavy", 100), ("stone", 50));
            int heavyLast = 0;
            const int runs = 20000;

            for (int i = 0; i < runs; i++)
            {
                List<string> result = Shuffle(new[] { "heavy", "stone" }, table, Seeded(i));
                if (result[result.Count - 1] == "heavy")
                {
                    heavyLast++;
                }
            }

            Assert.InRange((double)heavyLast / runs, 0.28, 0.39); // 1/3
        }

        [Fact]
        public void OverallOrderIsNotDeterminedByWeightAlone()
        {
            // A full deck must produce many different orders, not one canonical heavy-first list.
            OrbWeightTable table = OrbWeightTable.Defaults();
            string[] deck = { "poison_orb", "blind", "critiball", "stone", "stone", "sworb", "memorb", "snowball" };
            HashSet<string> orders = new HashSet<string>();
            const int runs = 500;

            for (int i = 0; i < runs; i++)
            {
                orders.Add(string.Join(",", Shuffle(deck, table, Seeded(i))));
            }

            Assert.True(orders.Count > runs / 2, $"only {orders.Count} distinct orders in {runs} shuffles");
        }

        [Fact]
        public void MidDeckMixesLowAndHighWeights()
        {
            // Across many shuffles a below-baseline orb must sometimes come before a baseline one.
            OrbWeightTable table = OrbWeightTable.Defaults();
            int memorbBeforeStone = 0;
            const int runs = 20000;

            for (int i = 0; i < runs; i++)
            {
                List<string> result = Shuffle(new[] { "stone", "memorb" }, table, Seeded(i));
                if (result.IndexOf("memorb") < result.IndexOf("stone"))
                {
                    memorbBeforeStone++;
                }
            }

            Assert.InRange((double)memorbBeforeStone / runs, 0.37, 0.45); // 40 / 90 = 44.4%
        }

        [Fact]
        public void NullArguments_AreRejected()
        {
            List<string> items = new List<string> { "a", "b" };
            OrbWeightTable table = Table(("a", 50), ("b", 50));

            Assert.Throws<ArgumentNullException>(() => WeightedOrder.ShuffleInPlace(items, null, table, 1.0, Seeded(1)));
            Assert.Throws<ArgumentNullException>(() => WeightedOrder.ShuffleInPlace(items, item => item, null, 1.0, Seeded(1)));
            Assert.Throws<ArgumentNullException>(() => WeightedOrder.ShuffleInPlace(items, item => item, table, 1.0, null));

            // Validation must happen even when there is nothing to shuffle.
            List<string> single = new List<string> { "a" };
            Assert.Throws<ArgumentNullException>(() => WeightedOrder.ShuffleInPlace(single, item => item, null, 1.0, null));
        }

        [Fact]
        public void AUnitRandomValue_IsClampedAndStillProducesAPermutation()
        {
            List<string> result = Shuffle(new[] { "a", "b", "c" }, Table(("a", 50), ("b", 90), ("c", 10)), () => 1.0);

            Assert.Equal(new[] { "a", "b", "c" }, result.OrderBy(x => x));
        }

        [Fact]
        public void ExponentZero_StillPutsZeroWeightOrbsLast()
        {
            OrbWeightTable table = Table(("heavy", 100), ("stone", 50), ("zero", 0));

            for (int i = 0; i < 50; i++)
            {
                List<string> result = Shuffle(new[] { "zero", "heavy", "stone" }, table, Seeded(i), exponent: 0.0);
                Assert.Equal("zero", result[result.Count - 1]);
            }
        }

        [Fact]
        public void ANonFiniteOrNegativeExponent_IsTreatedAsOne()
        {
            Assert.Equal(50.0, WeightedOrder.EffectiveWeight(50, 1.0));
            Assert.Equal(WeightedOrder.EffectiveWeight(50, 1.0), WeightedOrder.EffectiveWeight(50, double.NaN));
            Assert.Equal(WeightedOrder.EffectiveWeight(50, 1.0), WeightedOrder.EffectiveWeight(50, -2.0));
            Assert.Equal(1.0, WeightedOrder.EffectiveWeight(50, 0.0));
            Assert.Equal(0.0, WeightedOrder.EffectiveWeight(0, 1.0));
        }
    }
}

