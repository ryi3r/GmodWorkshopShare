using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

// ReSharper disable once CheckNamespace
namespace JHolloway.SteamLibrary
{
    public partial class SteamLibrary
    {
        public string? LibraryPath { get; protected set; }
        public string? SteamAppsPath { get; protected set; }
        public SteamGame[] Games { get; protected set; }

        public SteamLibrary(string path = "")
        {
            this.LibraryPath = path;
            if (string.IsNullOrEmpty(path))
            {
                this.Games = [];
                return;
            }

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Steam library not found at path: {path}");

            this.SteamAppsPath = Path.Join(this.LibraryPath, "steamapps");

            List<SteamGame> games = new();
            string[] manifestFiles = Directory.GetFiles(SteamAppsPath, "appmanifest_*.acf");
            foreach (string manifestPath in manifestFiles)
            {
                VProperty manifest = VdfConvert.Deserialize(File.ReadAllText(manifestPath));
                SteamGame game = new(manifest.Value["appid"]?.Value<uint>() ?? 0, this, manifest);
                games.Add(game);
            }
            this.Games = games.ToArray();
        }
    }
}
