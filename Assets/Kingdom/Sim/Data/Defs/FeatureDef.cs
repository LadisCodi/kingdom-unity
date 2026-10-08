namespace Kingdom.Sim.Data
{
    // Something on a cell: the source it is, where it respawns, how big a block of it may be.
    public sealed class FeatureDef
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string RespawnTerrain { get; set; }
        public int? MaxFootprint { get; set; }
    }
}
