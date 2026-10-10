using Codigames.Game.Saves;
using Codigames.Kingdom;
using Codigames.Kingdom.City.State;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Game.Tests
{
    public class KingdomSaveCodecTests
    {
        private static KingdomState Kingdom()
        {
            var state = new KingdomState { LastAdvance = 1_700_000_000_000 };
            state.City.Builders = 2;
            state.City.NextId = 7;
            state.City.Districts.Add(new DistrictState
            {
                Id = "district-3", DefinitionId = "Housing", Ordinal = 2, Level = 1, Anchor = new Vector2Int(-1, 4), Built = false,
            });
            state.City.Jobs.Add(new ConstructionJob { Id = "job-6", DistrictId = "district-3", TargetLevel = 1, StartedAt = 1_699_999_990_000, Seconds = 24 });
            state.Ground.Features[new Vector2Int(3, -2)] = "Trees";
            state.Balances["Gold"] = 585;
            return state;
        }

        [Test]
        public void Decode_ShouldReadBackWhatEncodeWrote()
        {
            var codec = new KingdomSaveCodec();

            var raw = codec.Parse(codec.Encode(Kingdom(), 4));
            var read = codec.Decode(raw);

            Assert.That(codec.VersionOf(raw), Is.EqualTo(4));
            Assert.That(read.LastAdvance, Is.EqualTo(1_700_000_000_000));
            Assert.That(read.City.Builders, Is.EqualTo(2));
            Assert.That(read.City.NextId, Is.EqualTo(7));
            Assert.That(read.City.Districts[0].Anchor, Is.EqualTo(new Vector2Int(-1, 4)));
            Assert.That(read.City.Districts[0].Ordinal, Is.EqualTo(2));
            Assert.That(read.City.Jobs[0].CompletesAt, Is.EqualTo(1_700_000_014_000));
            Assert.That(read.Ground.Features[new Vector2Int(3, -2)], Is.EqualTo("Trees"));
            Assert.That(read.Balances["Gold"], Is.EqualTo(585));
        }

        // Every map by cell, whatever it holds: the fog's treasures once broke a reload.
        [Test]
        public void EveryMapByCell_ShouldReadBack()
        {
            var codec = new KingdomSaveCodec();
            var state = Kingdom();
            state.Fog.Treasures[new Vector2Int(4, -2)] = new Kingdom.Fog.State.Treasure { N = 3, Coin = "Gold", At = 12 };
            state.Fog.Progress[new Vector2Int(-1, 2)] = 2;

            var read = codec.Decode(codec.Parse(codec.Encode(state, 21)));

            Assert.That(read.Fog.Treasures[new Vector2Int(4, -2)].N, Is.EqualTo(3));
            Assert.That(read.Fog.Treasures[new Vector2Int(4, -2)].Coin, Is.EqualTo("Gold"));
            Assert.That(read.Fog.Progress[new Vector2Int(-1, 2)], Is.EqualTo(2));
        }

        // An older build wrote the treasures keyed "x,y": they still read.
        [Test]
        public void AMapKeyedByText_ShouldStillRead()
        {
            var codec = new KingdomSaveCodec();
            var raw = codec.Parse(codec.Encode(Kingdom(), 21));
            raw["kingdom"]["Fog"]["Treasures"] = Newtonsoft.Json.Linq.JObject.Parse("{\"4,-2\": { \"N\": 5, \"Coin\": \"Wood\", \"At\": 1 }}");

            var read = codec.Decode(raw);

            Assert.That(read.Fog.Treasures[new Vector2Int(4, -2)].N, Is.EqualTo(5));
        }
    }
}
