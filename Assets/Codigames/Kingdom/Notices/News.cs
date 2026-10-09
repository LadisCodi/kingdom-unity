namespace Codigames.Kingdom.Notices
{
    // Something that happened the player may not have seen, kept until its bubble is opened. Its key names the event,
    // never the moment it was noticed, so the same event never makes two.
    public sealed class News
    {
        public string Key { get; set; }
        public NewsGroup Group { get; set; }

        // Epoch milliseconds: when it happened.
        public double At { get; set; }

        // Built: the district and the level it reached (1 for a build).
        public string District { get; set; }
        public int Level { get; set; }

        // Sighted: the landmark or abandoned building.
        public string Site { get; set; }

        public static News Built(string district, int level, double at)
            => new() { Group = NewsGroup.Built, Key = $"built:{district}:{level}", At = at, District = district, Level = level };

        public static News Sighted(string site, double at)
            => new() { Group = NewsGroup.Sighted, Key = $"sighted:{site}", At = at, Site = site };

        public static News ChainDone(double at) => new() { Group = NewsGroup.ChainDone, Key = "chainDone", At = at };
    }
}
