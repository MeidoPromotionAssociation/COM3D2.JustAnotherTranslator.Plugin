using System;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Lyric;

public static class LyricPatch
{
    // // 初始化字幕管理器
    // [HarmonyPatch(typeof(DanceSubtitleMgr), "Start")]
    // [HarmonyPrefix]
    // public static void DanceSubtitleMgr_Start_Prefix(DanceSubtitleMgr __instance)
    // {
    //     if (DanceMain.SelectDanceData is null)
    //         return;
    //
    //     var csv_path = RhythmAction_Mgr.Instance.MusicCSV_Path;
    //     var musicName = string.Empty;
    //
    //     if (!string.IsNullOrEmpty(csv_path))
    //     {
    //         var prefix = "csv_rhythm_action/";
    //         var suffix = "/";
    //
    //
    //         if (csv_path.StartsWith(prefix) && csv_path.EndsWith(suffix))
    //         {
    //             musicName = csv_path.Substring(prefix.Length, csv_path.Length - prefix.Length - suffix.Length);
    //             LogManager.Debug($"Extracted m_UseMusicName: {musicName}");
    //
    //             LyricManger.CreateMusicPath(musicName);
    //             LyricManger.TryToLoadLyric(musicName);
    //         }
    //         else
    //         {
    //             LogManager.Warning($"MusicCSV_Path format is unexpected: {csv_path}");
    //         }
    //     }
    // }

    // 舞蹈加载
    [HarmonyPatch(typeof(RhythmAction_Mgr), "Awake")]
    [HarmonyPostfix]
    public static void RhythmActionMgr_Awake_Postfix(RhythmAction_Mgr __instance)
    {
        try
        {
            var musicName = Traverse.Create(__instance).Field<string>("m_UseMusicName").Value;
            if (string.IsNullOrEmpty(musicName))
                return;

            LyricManger.HandleDanceLoaded(musicName);
        }
        catch (Exception e)
        {
            LogManager.Error("HandleDanceLoaded failed/处理舞蹈加载失败: " + e.Message);
        }
    }

    // 开始舞蹈
    [HarmonyPatch(typeof(RhythmAction_Mgr), "RhythmGame_Start")]
    [HarmonyPrefix]
    public static void RhythmActionMgr_RhythmGame_Start_Prefix(RhythmAction_Mgr __instance)
    {
        // just be safe, use __instance here
        LyricManger.HandleDanceStart(__instance);
    }


    // 结束舞蹈
    [HarmonyPatch(typeof(RhythmAction_Mgr), "RhythmGame_End")]
    [HarmonyPrefix]
    public static void RhythmActionMgr_RhythmGame_End_Prefix()
    {
        LyricManger.HandleDanceEnd();
    }
}