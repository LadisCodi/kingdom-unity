using System;
using System.Collections.Generic;
using Codigames.Kingdom.City;

namespace Codigames.Kingdom.Economy
{
    // Skipping a wait with Gems (the web's rush.ts): a Gem a few seconds, never less than one, rounded as every
    // calculated price is. A building's work and the Townhall's line of villagers are the waits it finishes.
    public class GemRush
    {
        public const string GEMS = "Gems";

        private readonly Construction _construction;
        private readonly VillagerTraining _training;
        private readonly ITreasury _treasury;
        private readonly IRushSettings _settings;

        public GemRush(Construction construction, VillagerTraining training, ITreasury treasury, IRushSettings settings)
        {
            _construction = construction;
            _training = training;
            _treasury = treasury;
            _settings = settings;
        }

        public static double GemsToFinish(double seconds, double secondsPerGem)
            => Math.Max(1, Prices.RoundPrice(Math.Ceiling(Math.Max(0, seconds) / secondsPerGem)));

        public double JobCost(string jobId, double now)
            => GemsToFinish(_construction.RemainingSeconds(jobId, now) ?? 0, _settings.SecondsPerGem);

        public bool FinishJob(string jobId, double now)
        {
            var left = _construction.RemainingSeconds(jobId, now);
            if (left == null || !Pay(JobCost(jobId, now))) return false;
            return _construction.Hurry(jobId, left.Value, now);
        }

        public double LineCost(double now) => GemsToFinish(_training.RemainingSeconds(now) ?? 0, _settings.SecondsPerGem);

        public bool FinishLine(double now)
        {
            var left = _training.RemainingSeconds(now);
            if (left == null || !Pay(LineCost(now))) return false;
            _training.Hurry(left.Value, now);
            return true;
        }

        private bool Pay(double gems) => _treasury.TryPay(new Dictionary<string, double> { [GEMS] = gems });
    }
}
