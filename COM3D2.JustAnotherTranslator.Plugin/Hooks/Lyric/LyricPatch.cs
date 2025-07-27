using System;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Lyric;

/// <summary>
///     处理舞蹈歌词字幕的 Harmony 补丁
/// </summary>
public static class LyricPatch
{
    // /// <summary>
    // ///     初始化字幕管理器
    // ///     处理舞蹈加载
    // /// </summary>
    // /// <param name="__instance"></param>
    // [HarmonyPatch(typeof(DanceSubtitleMgr), "Start")]
    // [HarmonyPrefix]
    // public static void DanceSubtitleMgr_Start_Prefix(DanceSubtitleMgr __instance)
    // {
    //     LogManager.Debug("DanceSubtitleMgr_Start_Prefix called");
    //
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

    /// <summary>
    ///     处理舞蹈加载
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(RhythmAction_Mgr), "Awake")]
    [HarmonyPostfix]
    public static void RhythmActionMgr_Awake_Postfix(RhythmAction_Mgr __instance)
    {
        try
        {
            LogManager.Debug("RhythmActionMgr_Awake_Postfix called");

            var musicName = Traverse.Create(__instance).Field<string>("m_UseMusicName").Value;
            if (string.IsNullOrEmpty(musicName))
                return;

            LogManager.Info($"Current dance name (musicName)/当前舞蹈（musicName）: {musicName}");

            LyricManger.HandleDanceLoaded(musicName);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"RhythmActionMgr_Awake_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     处理开始舞蹈
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(RhythmAction_Mgr), "RhythmGame_Start")]
    [HarmonyPrefix]
    public static void RhythmActionMgr_RhythmGame_Start_Prefix(RhythmAction_Mgr __instance)
    {
        try
        {
            LogManager.Debug("RhythmActionMgr_RhythmGame_Start called");
            LogManager.Info("Dance started/舞蹈开始");
            // just be safe, use __instance here
            LyricManger.HandleDanceStart(__instance);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"RhythmActionMgr_RhythmGame_Start_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }


    /// <summary>
    ///     处理结束舞蹈
    /// </summary>
    [HarmonyPatch(typeof(RhythmAction_Mgr), "RhythmGame_End")]
    [HarmonyPrefix]
    public static void RhythmActionMgr_RhythmGame_End_Prefix()
    {
        try
        {
            LogManager.Debug("RhythmActionMgr_RhythmGame_End called");
            LogManager.Info("Dance ended/舞蹈结束");
            LyricManger.HandleDanceEnd();
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"RhythmActionMgr_RhythmGame_End_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }
}