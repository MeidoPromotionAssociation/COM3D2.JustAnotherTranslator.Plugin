using COM3D2.JustAnotherTranslator.Plugin.Translator;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Lyric;

public static class LyricPatch
{
    private static bool isActive;


    [HarmonyPatch(typeof(DanceSubtitleMgr), "Start")]
    [HarmonyPrefix]
    public static void DanceSubtitleMgr_Start_Prefix(DanceSubtitleMgr __instance)
    {
        if (DanceMain.SelectDanceData is null)
            return;

        var csv_path = RhythmAction_Mgr.Instance.MusicCSV_Path;
        var musicName = string.Empty;

        if (!string.IsNullOrEmpty(csv_path))
        {
            var prefix = "csv_rhythm_action/";
            var suffix = "/";

            if (csv_path.StartsWith(prefix) && csv_path.EndsWith(suffix))
            {
                musicName = csv_path.Substring(prefix.Length, csv_path.Length - prefix.Length - suffix.Length);
                LogManager.Debug($"Extracted m_UseMusicName: {musicName}");
            }
            else
            {
                LogManager.Warning($"MusicCSV_Path format is unexpected: {csv_path}");
            }
        }

        LyricManger.CreateMusicPath(musicName);
    }
}