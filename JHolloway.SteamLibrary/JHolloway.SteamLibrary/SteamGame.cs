using Gameloop.Vdf.Linq;

// ReSharper disable once CheckNamespace
namespace JHolloway.SteamLibrary
{
    public class SteamGame
    {
        public uint AppId { get; protected set; }
        public bool Installed => true;
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public string Name;
        public string? InstallPath { get; protected set; }
        public VToken? Manifest { get; protected set; }
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public SteamLibrary? Library;

        public SteamGame(uint appId, SteamLibrary? library, VProperty manifest)
        {
            this.AppId = appId;
            this.Library = library;

            this.Manifest = manifest.Key.ToLower() == "appstate" ? manifest.Value : manifest;

            this.Name = this.Manifest?["name"]?.Value<string>() ?? "";

            if (Library != null)
            {
                string installdir = this.Manifest?["installdir"]?.Value<string>() ?? "";
                if (!string.IsNullOrEmpty(installdir))
                {
                    this.InstallPath = Path.Join(Library.SteamAppsPath, "common", installdir);
                }
            }
        }

        public override string ToString()
        {
            return $"SteamGame \"{Name}\"[{AppId}]";
        }
    }
}
