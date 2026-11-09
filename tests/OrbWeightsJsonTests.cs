using System.Collections.Generic;
using Newtonsoft.Json;
using PeglinOrbOrder.Core;
using Xunit;

namespace PeglinOrbOrder.Tests
{
    public class OrbWeightsJsonTests
    {
        [Fact]
        public void CreateDefault_ContainsEveryOrb()
        {
            OrbWeightsDocument document = OrbWeightsJson.CreateDefault();

            Assert.Equal(110, document.Orbs.Count);
            Assert.Equal(1.0, document.Exponent);
            Assert.Equal((long)OrbWeightTable.DefaultWeight, document.DefaultWeight);
            Assert.False(string.IsNullOrWhiteSpace(document.Readme));
        }

        [Fact]
        public void RoundTrip_KeepsEveryWeightAndTheSettings()
        {
            OrbWeightsDocument original = OrbWeightsJson.CreateDefault();
            original.Exponent = 1.5;
            original.Orbs["stone"] = 12;

            OrbWeightsDocument parsed = OrbWeightsJson.Parse(OrbWeightsJson.Serialize(original));

            Assert.Equal(1.5, parsed.Exponent);
            Assert.Equal((long)OrbWeightTable.DefaultWeight, parsed.DefaultWeight);
            Assert.Equal(original.Orbs.Count, parsed.Orbs.Count);
            foreach (KeyValuePair<string, long> pair in original.Orbs)
            {
                Assert.Equal(pair.Value, parsed.Orbs[pair.Key]);
            }
        }

        [Fact]
        public void Parse_MinimalFile_UsesDocumentedDefaults()
        {
            OrbWeightsDocument document = OrbWeightsJson.Parse("{\"orbs\":{\"stone\":10}}");

            Assert.Equal(10, document.Table.For("stone"));
            Assert.Equal((long)OrbWeightTable.DefaultWeight, document.DefaultWeight);
            Assert.Equal(1.0, document.Exponent);
            Assert.Equal(OrbWeightTable.DefaultWeight, document.Table.For("poison_orb"));
        }

        [Fact]
        public void Parse_ClampsOutOfRangeWeights()
        {
            OrbWeightsDocument document = OrbWeightsJson.Parse("{\"orbs\":{\"a\":900,\"b\":-5}}");

            Assert.Equal(OrbWeightTable.MaxWeight, document.Table.For("a"));
            Assert.Equal(OrbWeightTable.MinWeight, document.Table.For("b"));
        }

        [Fact]
        public void Parse_ClampsTheExponent()
        {
            Assert.Equal(OrbWeightsJson.MaxExponent, OrbWeightsJson.Parse("{\"exponent\":99}").Exponent);
            Assert.Equal(OrbWeightsJson.MinExponent, OrbWeightsJson.Parse("{\"exponent\":-3}").Exponent);
        }

        [Fact]
        public void Parse_ResetsANonFiniteExponent()
        {
            Assert.Equal(1.0, OrbWeightsJson.Parse("{\"exponent\":\"NaN\"}").Exponent);
            Assert.Equal(1.0, OrbWeightsJson.Parse("{\"exponent\":\"Infinity\"}").Exponent);
            Assert.Equal(1.0, OrbWeightsJson.Parse("{\"exponent\":\"-Infinity\"}").Exponent);
        }

        [Fact]
        public void Parse_ClampsTheDefaultWeight()
        {
            Assert.Equal((long)OrbWeightTable.MaxWeight, OrbWeightsJson.Parse("{\"defaultWeight\":5000}").DefaultWeight);
            Assert.Equal((long)OrbWeightTable.MinWeight, OrbWeightsJson.Parse("{\"defaultWeight\":-5}").DefaultWeight);
            Assert.Equal((long)OrbWeightTable.MaxWeight, OrbWeightsJson.Parse("{\"defaultWeight\":2147483648}").DefaultWeight);
            Assert.Equal(OrbWeightTable.MaxWeight, OrbWeightsJson.Parse("{\"defaultWeight\":5000}").Table.For("unknown_orb"));
        }

        [Fact]
        public void Parse_IgnoresUnknownProperties()
        {
            OrbWeightsDocument document = OrbWeightsJson.Parse("{\"orbs\":{\"stone\":60},\"somethingNew\":true}");

            Assert.Equal(60, document.Table.For("stone"));
        }

        [Fact]
        public void Parse_RejectsEmptyAndBrokenFiles()
        {
            Assert.ThrowsAny<JsonException>(() => OrbWeightsJson.Parse(string.Empty));
            Assert.ThrowsAny<JsonException>(() => OrbWeightsJson.Parse("   "));
            Assert.ThrowsAny<JsonException>(() => OrbWeightsJson.Parse("{ this is not json"));
            Assert.ThrowsAny<JsonException>(() => OrbWeightsJson.Parse("null"));
        }

        [Fact]
        public void WithMissingDefaults_FillsGapsWithoutOverwritingUserValues()
        {
            OrbWeightsDocument document = OrbWeightsJson.Parse("{\"orbs\":{\"stone\":10}}");
            Assert.Equal(109, document.MissingDefaultCount());

            document.WithMissingDefaults();

            Assert.Equal(0, document.MissingDefaultCount());
            Assert.Equal(110, document.Orbs.Count);
            Assert.Equal(10, document.Table.For("stone"));
            Assert.Equal(95, document.Table.For("poison_orb"));
        }

        [Fact]
        public void Serialize_WritesAReadableFileWithTheReadme()
        {
            string json = OrbWeightsJson.Serialize(OrbWeightsJson.CreateDefault());

            Assert.Contains("\"readme\"", json);
            Assert.Contains("\"orbs\"", json);
            Assert.Contains("\"stone\": 50", json);
            Assert.Contains("\n", json);
        }

        [Fact]
        public void DuplicateKeysInFile_KeepTheLastValue()
        {
            OrbWeightsDocument document = OrbWeightsJson.Parse("{\"orbs\":{\"stone\":10,\"stone\":70}}");

            Assert.Equal(70, document.Table.For("stone"));
        }

        [Fact]
        public void CaseOnlyDuplicateKeys_AreOneOrb()
        {
            OrbWeightsDocument document = OrbWeightsJson.Parse("{\"orbs\":{\"stone\":10,\"STONE\":80}}");

            Assert.Single(document.Orbs);
            Assert.Equal(80, document.Table.For("stone"));
        }

        [Fact]
        public void AWeightBeyondInt32_IsClampedInsteadOfRejectingTheFile()
        {
            OrbWeightsDocument document = OrbWeightsJson.Parse("{\"orbs\":{\"stone\":2147483648,\"memorb\":-9999999999}}");

            Assert.Equal(OrbWeightTable.MaxWeight, document.Table.For("stone"));
            Assert.Equal(OrbWeightTable.MinWeight, document.Table.For("memorb"));
        }

        [Fact]
        public void NullOrbsObject_IsTreatedAsEmpty()
        {
            OrbWeightsDocument document = OrbWeightsJson.Parse("{\"orbs\":null}");

            Assert.Empty(document.Orbs);
            Assert.Equal(OrbWeightTable.DefaultWeight, document.Table.For("stone"));
            Assert.Equal(110, document.MissingDefaultCount());
        }

        [Fact]
        public void AnEmptyOrbKeyIsDropped()
        {
            OrbWeightsDocument document = OrbWeightsJson.Parse("{\"orbs\":{\"\":80,\"stone\":10}}");

            Assert.Single(document.Orbs);
            Assert.Equal(10, document.Table.For("stone"));
        }

        [Fact]
        public void UnknownKeys_AreReportedButKept()
        {
            OrbWeightsDocument document = OrbWeightsJson.Parse("{\"orbs\":{\"stonne\":80,\"stone\":50}}");

            Assert.Contains("stonne", document.UnknownKeys());
            Assert.Equal(80, document.Table.For("stonne"));
            Assert.Equal(50, document.Table.For("stone"));
        }
    }
}

