using System;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Subtitle;

/// <summary>
///     用于处理 VR 字幕的 Harmony 补丁
///     见 VRTouchKagManager
///     可以看到只有 @VRTouch 标签会命中，其余标签为注释
///     @VRTouch maid=0 type=wait random voice=S2_32080 face=微笑み faceblend=頬１涙０
///     ;;会話ランダム１
///     ;*L0|
///     ;@talk voice=S2_32080 name=[HF]
///     ;さて……何を歌いましょうか。
///     ;@hitret
///     因此也依赖于 VoiceID 翻译
/// </summary>
public static class VRTouchSubtitlePatch
{
    /// <summary>
    ///     捕获 @Shuffle 标签
    ///     获取说话角色
    /// </summary>
    /// <param name="tag_data"></param>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(VRTouchKagManager), "TagShuffle")]
    [HarmonyPostfix]
    public static void VRTouchKagManager_TagTalk_Postfix(KagTagSupport tag_data,
        ADVKagManager __instance)
    {
        try
        {
            LogManager.Debug("VRTouchKagManager_TagTalk_Postfix called");

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

                LogManager.Debug(
                    $"VRTouchKagManager_TagTalk_Postfix tag_data name: {tag_data.GetTagProperty("name").AsString()}");
                LogManager.Debug($"VRTouchKagManager_TagTalk_Postfix tag_data voiceId: {voiceId}");
                LogManager.Debug(
                    $"VRTouchKagManager_TagTalk_Postfix speakingMaid: {speakingMaid.status.fullNameJpStyle}");
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"VRTouchKagManager_TagTalk_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }
}