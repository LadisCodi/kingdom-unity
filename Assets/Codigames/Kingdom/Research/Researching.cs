using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Research.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Research
{
    // The tree: Knowledge is poured into a technology on as many visits as it takes and stays there; once it is
    // full, paying the price completes the technology on the spot. Nothing is under study and nothing waits, so
    // research is not on the timeline.
    public class Researching
    {
        private readonly ResearchState _state;
        private readonly ICatalog<ITechnology> _technologies;
        private readonly ITechTree _tree;
        private readonly IBookshelf _shelf;
        private readonly IExploredGround _explored;
        private readonly KnowledgeBar _bar;
        private readonly ITreasury _treasury;
        private readonly Goods.Stockpile _stockpile;

        public Researching(ResearchState state, ICatalog<ITechnology> technologies, ITechTree tree, IBookshelf shelf,
            IExploredGround explored, KnowledgeBar bar, ITreasury treasury, Goods.Stockpile stockpile = null)
        {
            _stockpile = stockpile;
            _state = state;
            _technologies = technologies;
            _tree = tree;
            _shelf = shelf;
            _explored = explored;
            _bar = bar;
            _treasury = treasury;
        }

        // Knowledge went into a technology: its id and how much.
        public event Action<string, double> Poured;

        // A technology is about to be researched, at a moment: nothing it moves has changed yet.
        public event Action<string, double> Completing;

        // A technology was researched.
        public event Action<string> Researched;

        // Every card of a band is researched and the band pays its reward: the book, the band, the fragments.
        public event Action<string, int, int> EraFinished;

        public IReadOnlyList<string> Completed => _state.Completed;

        public bool IsComplete(string id) => _state.Completed.Contains(id);

        public double PouredInto(string id) => _state.Poured.TryGetValue(id, out var poured) ? poured : 0;

        public double Missing(string id) => Math.Max(0, _technologies.Get(id).Knowledge - PouredInto(id));

        public bool IsFilled(string id) => Missing(id) <= 0;

        public bool RequirementsMet(string id) => _technologies.Get(id).Requires.All(IsComplete);

        public bool IsEraOpen(string tome, int era) => EraShortfall(tome, era) == 0;

        // Cells still to reveal before a band opens; 0 once it is open.
        public int EraShortfall(string tome, int era)
            => era <= 1 ? 0 : Math.Max(0, _tree.CellsToOpen(tome, era) - _explored.RevealedCount);

        // Why it cannot be worked on, or None when it can.
        public ResearchRefusal Refusal(string id)
        {
            var tech = _technologies.Get(id);
            if (!tech.IsPlaced) return ResearchRefusal.MissingRequirement;
            if (IsComplete(id)) return ResearchRefusal.AlreadyDone;
            if (!_shelf.IsOpen(tech.Tome)) return ResearchRefusal.TomeClosed;
            if (!RequirementsMet(id)) return ResearchRefusal.MissingRequirement;
            if (!IsEraOpen(tech.Tome, tech.Era)) return ResearchRefusal.EraLocked;
            return ResearchRefusal.None;
        }

        public TechState StateOf(string id)
        {
            if (IsComplete(id)) return TechState.Done;
            return Refusal(id) == ResearchRefusal.None ? TechState.Progress : TechState.Locked;
        }

        // Pours from the bar as much as it holds, up to what is still missing and at most `max`.
        public PourResult Pour(string id, double now, int max = int.MaxValue)
        {
            if (Refusal(id) != ResearchRefusal.None) return PourResult.Refused;

            var missing = Missing(id);
            if (missing <= 0) return PourResult.AlreadyFull;

            var amount = Math.Min(missing, Math.Min(Math.Floor(_bar.Amount), Math.Max(0, max)));
            if (amount <= 0 || !_bar.TrySpend(amount, now)) return PourResult.NothingHeld;

            _state.Poured[id] = PouredInto(id) + amount;
            Poured?.Invoke(id, amount);
            return PourResult.Poured;
        }

        public bool CanAfford(string id) => _treasury.CanAfford(_technologies.Get(id).Price) && HasGoods(id);

        // The refined goods it asks, as charged.
        public IReadOnlyDictionary<string, double> GoodsFor(string id)
            => _stockpile?.Priced(_technologies.Get(id).GoodsPrice) ?? new Dictionary<string, double>();

        private bool HasGoods(string id) => _stockpile == null || _stockpile.CanAfford(_technologies.Get(id).GoodsPrice);

        // Could it be completed this second?
        public bool CanResearch(string id) => Refusal(id) == ResearchRefusal.None && IsFilled(id) && CanAfford(id);

        // Pays the price and completes it, at `now`. Its Knowledge must be in.
        public ResearchResult Complete(string id, double now)
        {
            if (Refusal(id) != ResearchRefusal.None) return ResearchResult.Refused;
            if (!IsFilled(id)) return ResearchResult.NotFilled;
            if (!_treasury.CanAfford(_technologies.Get(id).Price)) return ResearchResult.CannotAfford;
            if (!HasGoods(id)) return ResearchResult.NotEnoughGoods;
            _treasury.TryPay(_technologies.Get(id).Price);
            _stockpile?.TryPay(_technologies.Get(id).GoodsPrice);

            Completing?.Invoke(id, now);

            _state.Poured.Remove(id);
            _state.Completed.Add(id);
            Researched?.Invoke(id);
            ClaimEraReward(_technologies.Get(id));
            return ResearchResult.Researched;
        }

        // A press worth making now: research it, or pour enough from the bar to fill it.
        public bool IsActionable(string id)
        {
            if (Refusal(id) != ResearchRefusal.None) return false;
            return IsFilled(id) ? CanAfford(id) : _bar.Amount >= Missing(id);
        }

        public int ActionableCount() => _technologies.Items.Count(t => IsActionable(t.Id));

        // Every placed card of the band researched; a band with none placed was never finished.
        public bool IsEraFinished(string tome, int era)
        {
            var cards = _technologies.Items.Where(t => t.IsPlaced && t.Tome == tome && t.Era == era).ToList();
            return cards.Count > 0 && cards.All(t => IsComplete(t.Id));
        }

        public static string EraKey(string tome, int era) => $"{tome}:{era}";

        private void ClaimEraReward(ITechnology tech)
        {
            if (!tech.IsPlaced) return;

            var reward = _tree.EraReward(tech.Tome, tech.Era);
            var key = EraKey(tech.Tome, tech.Era);
            if (reward == null || _state.Rewarded.Contains(key) || !IsEraFinished(tech.Tome, tech.Era)) return;

            _state.Rewarded.Add(key);
            EraFinished?.Invoke(tech.Tome, tech.Era, reward.Value);
        }
    }
}
