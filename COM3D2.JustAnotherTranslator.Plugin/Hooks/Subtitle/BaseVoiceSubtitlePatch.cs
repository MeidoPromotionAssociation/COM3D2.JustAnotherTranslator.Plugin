using System;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Subtitle;

/// <summary>
///     用于处理一般的语音字幕的 Harmony 补丁
///     见 BaseKagManager
///     命中 @PlayVoice 标签，开始说话，但是可能没有文本，例如m c01_0001.ks
///     例如
///     @PlayVoice maid=1 voice=MC_t2
///     但存在有文本的的标签，例如 mc01_0004.ks
///     @PlayVoice maid=1 voice=MC_t2 text=おはようございます
///     *L0|
///     @PlayVoice maid=2 voice=MC05_t1 wait
///     ;ありがとうございますっ！　それでは、聞いてください、Blooming∞Dreaming！（ブルーミング・ドリーミング）！
///     ;@hitret
///     但 BaseKagManager 没有 hitret 的捕获，因此基本依靠 VoiceID 进行翻译
/// </summary>
public static class BaseVoiceSubtitlePatch
{
    /// <summary>
    ///     捕获 @PlayVoice 标签
    /// </summary>
    /// <param name="tag_data"></param>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(BaseKagManager), "TagPlayVoice")]
    [HarmonyPrefix]
    public static void BaseKagManager_TagPlayVoice_Prefix(KagTagSupport tag_data, BaseKagManager __instance)
    {
        try
        {
            // voiceId + .ogg 既是音频文件，支持按音频文件名进行翻译
            var voiceId = tag_data.GetTagProperty("voice").AsString();

            // 无法从游戏本身获取文本，因此无论如何都需要从翻译获取
            // 交由 Maid启动监听协程尝试从翻译获取
            // 原方法使用 GetMaidAndMan 获取 maid 对象，这里使用相同的方法
            var speakingMaid = __instance.GetMaidAndMan(tag_data);

            SubtitleManager.SetSubtitleType(JustAnotherTranslator.SubtitleTypeEnum.Base);
            SubtitleManager.SetCurrentVoiceId(voiceId);
            SubtitleManager.SetCurrentSpeaker(speakingMaid);
            SubtitleManager.StartMaidMonitoringCoroutine(speakingMaid);

            LogManager.Debug(
                $"BaseKagManager_TagPlayVoice_Prefix tag_data maid: {tag_data.GetTagProperty("maid").AsString()}");
            LogManager.Debug(
                $"BaseKagManager_TagPlayVoice_Prefix GetMaidAndMan speakingMaid: {speakingMaid.status.fullNameJpStyle}");
            LogManager.Debug($"BaseKagManager_TagPlayVoice_Prefix tag_data voiceId: {voiceId}");
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"BaseKagManager_TagPlayVoice_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }
}