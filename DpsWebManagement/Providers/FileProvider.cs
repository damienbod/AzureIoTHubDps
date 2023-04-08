using System.Reflection;

namespace DpsWebManagement.Providers
{
    public static class FileProvider
    {
        static readonly string? _directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
        static readonly string _pathToCerts = $"{_directory}\\..\\..\\..\\..\\Certs\\";

        private static bool _writeToDiskActive = false;
        private static bool _writePfxToDiskActive = true;

        public static void WriteToDisk(string name, string pemPayload)
        {
            if(_writeToDiskActive)
            {
                File.WriteAllText($"{_pathToCerts}{name}", pemPayload);
            }
        }

        public static string WritePfxToDisk(string name, byte[] bytesPayload)
        {
            if (_writePfxToDiskActive)
            {
                File.WriteAllBytes($"{_pathToCerts}{name}", bytesPayload);
            }

            return $"{_pathToCerts}{name}";
        }
    }
}
