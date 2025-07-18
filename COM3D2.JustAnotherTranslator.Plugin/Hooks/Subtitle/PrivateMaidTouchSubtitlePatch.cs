using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Subtitle;

/// <summary>
///     用于触摸模式语音字幕的 Harmony 补丁
/// </summary>
public static class PrivateMaidTouchSubtitlePatch
{
    /// <summary>
    ///     捕获 @Talk @rcTalk @TalkRepeat 标签
    ///     获取说话角色
    /// </summary>
    /// <param name="tag_data"></param>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(PrivateMaidTouchKagManager), "TagTalk")]
    [HarmonyPatch(typeof(PrivateMaidTouchKagManager), "TagRcTalk")]
    [HarmonyPatch(typeof(PrivateMaidTouchKagManager), "TagTalkRepeat")]
    [HarmonyPostfix]
    public static void PrivateMaidTouchKagManager_TagTalk_Postfix(KagTagSupport tag_data, ADVKagManager __instance)
    {
        LogManager.Debug("PrivateMaidTouchKagManager_TagTalk_Postfix called");

        if (tag_data.IsValid("voice"))
        {
            var speakingMaid = BaseKagManager.GetVoiceTargetMaid(tag_data);

            if (speakingMaid is null)
                return;

            var voiceId = tag_data.GetTagProperty("voice").AsString();

            SubtitleManager.SetSubtitleType(JustAnotherTranslator.SubtitleTypeEnum.Base);
            SubtitleManager.SetCurrentVoiceId(voiceId);
            SubtitleManager.SetCurrentSpeaker(speakingMaid);
            SubtitleManager.StartMaidMonitoringCoroutine(speakingMaid);

            LogManager.Debug($"PrivateMaidTouchKagManager_TagTalk_Postfix tag_data voiceId: {voiceId}");
            LogManager.Debug(
                $"PrivateMaidTouchKagManager_TagTalk_Postfix tag_data name: {tag_data.GetTagProperty("name").AsString()}");
            LogManager.Debug(
                $"PrivateMaidTouchKagManager_TagTalk_Postfix speakingMaid: {speakingMaid.status.fullNameJpStyle}");
        }
    }


    /// <summary>
    ///     捕获 @HitRet 标签
    ///     在脚本段落结束时，获取文本
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(PrivateMaidTouchKagManager), "TagHitRet")]
    [HarmonyPrefix]
    public static void PrivateMaidTouchKagManager_HitRet_Prefix(ADVKagManager __instance)
    {
        var text = __instance.kag_.GetText();
        LogManager.Debug($"PrivateMaidTouchKagManager_HitRet_Prefix called with text: {text}");
        LogManager.Debug(
            $"PrivateMaidTouchKagManager_HitRet_Prefix instance.kag_.GetCurrentLabel(): {__instance.kag_.GetCurrentLabel()}");
        LogManager.Debug(
            $"PrivateMaidTouchKagManager_HitRet_Prefix instance.kag_.GetCurrentFileName(): {__instance.kag_.GetCurrentFileName()}");
        LogManager.Debug(
            $"PrivateMaidTouchKagManager_HitRet_Prefix instance.kag_.GetCurrentLine(): {__instance.kag_.GetCurrentLine()}");

        SubtitleManager.SetSubtitleType(JustAnotherTranslator.SubtitleTypeEnum.Base);
        SubtitleManager.SetVoiceTextMapping(text, "PrivateMaidTouchKagManager_HitRet_Prefix");
    }
}