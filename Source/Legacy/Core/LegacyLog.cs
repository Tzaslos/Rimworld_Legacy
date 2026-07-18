using Verse;

namespace Legacy.Core
{
    public static class LegacyLog
    {
        private const string Prefix = "[Legacy] ";

        public static void Message(string message)
        {
            Log.Message(Prefix + message);
        }

        public static void Warning(string message)
        {
            Log.Warning(Prefix + message);
        }

        public static void Error(string message)
        {
            Log.Error(Prefix + message);
        }
    }
}
