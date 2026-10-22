using System;
using System.IO;
using System.Text;

namespace SkullKing.Core
{
    /// <summary>Local-only, exclusive-create UTF-8 text files. Never transmits or attaches player data.</summary>
    public static class FeedbackFileStore
    {
        public const int CharacterLimit = 4000;
        public static bool IsValidBody(string body)
            => !string.IsNullOrWhiteSpace(body) && body.Length <= CharacterLimit;

        public static string Save(string folder, string report)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("저장 폴더가 없습니다.");
            if (string.IsNullOrWhiteSpace(report)) throw new ArgumentException("저장할 내용이 없습니다.");
            Directory.CreateDirectory(folder);
            string name = "feedback_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N") + ".txt";
            string path = Path.Combine(folder, name);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(report);
            return path;
        }
    }
}
