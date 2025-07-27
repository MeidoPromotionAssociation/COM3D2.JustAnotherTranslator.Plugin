using System;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Subtitle;

/// <summary>
///     用于处理 ADV 字幕的 Harmony 补丁
///     ADV 场景都自带字幕，因此基本只在 VR 模式有用
///     基本和 SubtitleManager 中的代码相同
/// </summary>
public static class AdvSubtitlePatch
{
    /// <summary>
    ///     捕获 @Talk 标签
    ///     获取说话角色
    /// </summary>
    /// <param name="tag_data"></param>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(ADVKagManager), "TagTalk")]
    [HarmonyPostfix]
    public static void ADVKagManager_TagTalk_Postfix(KagTagSupport tag_data, ADVKagManager __instance)
    {
        try
        {
            LogManager.Debug("ADVKagManager_TagTalk_Postfix called");

            if (tag_data.IsValid("voice"))
            {
                var speakingMaid = BaseKagManager.GetVoiceTargetMaid(tag_data);

                if (speakingMaid is null)
                    return;

                var voiceId = tag_data.GetTagProperty("voice").AsString();

                SubtitleManager.SetCurrentSpeaker(speakingMaid);
                SubtitleManager.SetCurrentVoiceId(voiceId);
                SubtitleManager.SetSubtitleType(JustAnotherTranslator.SubtitleTypeEnum.Adv);
                SubtitleManager.StartMaidMonitoringCoroutine(speakingMaid);

                LogManager.Debug($"ADVKagManager_TagTalk_Postfix tag_data voiceId: {voiceId}");
                LogManager.Debug(
                    $"ADVKagManager_TagTalk_Postfix tag_data name: {tag_data.GetTagProperty("name").AsString()}");
                LogManager.Debug($"ADVKagManager_TagTalk_Postfix speakingMaid: {speakingMaid.status.fullNameJpStyle}");
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ADVKagManager_TagTalk_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }


    /// <summary>
    ///     捕获 @HitRet 标签
    ///     在脚本段落结束时，获取文本
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(ADVKagManager), "TagHitRet")]
    [HarmonyPrefix]
    public static void ADVKagManager_HitRet_Prefix(ADVKagManager __instance)
    {
        try
        {
            var text = __instance.kag_.GetText();
            LogManager.Debug($"ADVKagManager_HitRet_Prefix called with text: {text}");
            LogManager.Debug(
                $"ADVKagManager_HitRet_Prefix instance.kag_.GetCurrentLabel(): {__instance.kag_.GetCurrentLabel()}");
            LogManager.Debug(
                $"ADVKagManager_HitRet_Prefix instance.kag_.GetCurrentFileName(): {__instance.kag_.GetCurrentFileName()}");
            LogManager.Debug(
                $"ADVKagManager_HitRet_Prefix instance.kag_.GetCurrentLine(): {__instance.kag_.GetCurrentLine()}");

            SubtitleManager.SetSubtitleType(JustAnotherTranslator.SubtitleTypeEnum.Adv);
            SubtitleManager.SetVoiceTextMapping(text, "ADVKagManager_HitRet_Prefix");
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ADVKagManager_HitRet_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }
}