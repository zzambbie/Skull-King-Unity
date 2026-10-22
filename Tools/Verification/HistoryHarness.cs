using System;
namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MenuItem : Attribute { public MenuItem(string path) {} }
}
namespace UnityEngine
{
    public static class Debug { public static void Log(object text) => Console.WriteLine(text); }
}
public static class HistoryHarness
{
    public static void Main()
    {
        SkullKing.Editor.CressionTrickHistoryChecks.Run();
        SkullKing.Editor.CressionWhalePresentationChecks.Run();
    }
}
