namespace HexMapEditor
{
    // The test channel includes published prereleases. The default is planned
    // to become stable at 1.0.0.
    internal static class UpdateChannelConfiguration
    {
        internal const string Owner = "amdesar";
        internal const string Repository = "HexMapEditor";
        internal const bool IncludePrereleases = true;
        internal const string ProjectUrl = "https://github.com/" + Owner + "/" + Repository;
    }
}
