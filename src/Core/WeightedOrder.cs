using System;
using System.Collections.Generic;

namespace PeglinOrbOrder.Core
{
    /// <summary>
    /// Turns a deck into a draw order where each orb's chance of coming next is proportional to its weight.
    ///
    /// Uses the Efraimidis-Spirakis weighted sampling trick: give every item the key u^(1/w) with u uniform,
    /// then sort descending. That is a valid "sample without replacement, proportional to weight" ordering,
    /// so it needs exactly one random number per weighted orb and stays deterministic for a given sequence.
    ///
    /// A weight of 0 (or below) means "never first": those items sort last, keeping the order they came in.
    /// </summary>
    public static class WeightedOrder
    {
        public static void ShuffleInPlace<T>(
            IList<T> items,
            Func<T, string> keyOf,
            OrbWeightTable table,
            double exponent,
            Func<double> nextUnit)
        {
            if (keyOf == null)
            {
                throw new ArgumentNullException(nameof(keyOf));
            }
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }
            if (nextUnit == null)
            {
                throw new ArgumentNullException(nameof(nextUnit));
            }
            if (items == null || items.Count < 2)
            {
                return;
            }

            int count = items.Count;
            double[] keys = new double[count];
            int[] order = new int[count];

            for (int i = 0; i < count; i++)
            {
                order[i] = i;
                int weight = table.For(keyOf(items[i]));
                if (weight <= 0)
                {
                    keys[i] = 0.0;
                    continue;
                }

                double effective = EffectiveWeight(weight, exponent);
                double unit = nextUnit();
                if (double.IsNaN(unit) || unit <= 0.0)
                {
                    unit = double.Epsilon;
                }
                else if (unit > 1.0)
                {
                    unit = 1.0;
                }

                keys[i] = Math.Pow(unit, 1.0 / effective);
            }

            // Descending key, ties keep the incoming order (stable).
            Array.Sort(order, (a, b) =>
            {
                int byKey = keys[b].CompareTo(keys[a]);
                return byKey != 0 ? byKey : a.CompareTo(b);
            });

            T[] copy = new T[count];
            for (int i = 0; i < count; i++)
            {
                copy[i] = items[i];
            }
            for (int i = 0; i < count; i++)
            {
                items[i] = copy[order[i]];
            }
        }

        /// <summary>Weights are raised to the configured exponent: 0 is a plain shuffle, 1 is linear, 2 is sharper.</summary>
        public static double EffectiveWeight(int weight, double exponent)
        {
            if (weight <= 0)
            {
                return 0.0;
            }
            if (double.IsNaN(exponent) || exponent < 0.0)
            {
                exponent = 1.0;
            }
            return Math.Pow(weight, exponent);
        }
    }
}
