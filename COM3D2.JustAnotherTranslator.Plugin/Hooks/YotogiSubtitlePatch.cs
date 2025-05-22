using COM3D2.JustAnotherTranslator.Plugin.Translator;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks;

// YotogiKagManager
// 首先会命中 @talk 标签，开始说话
// 然后会命中 @hitret 标签，结束脚本段落
// 此时在 @hitret 可以拿到脚本段落的文本
// 例如
// ;*L1|
// ;@talk voice=H0_GP01_17908 name=[HF1]
// ;う～……どうして３人でするの……？　おかしくないかなぁ、ご主人様……
// ;@hitret
// 但是游戏可能会提取执行下一个 @talk 标签，但语音排队播放，因此需要判断角色说话的 VoiceID
// 可以从 maid.AudioMan.FileName 中获取
public static class YotogiSubtitlePatch
{
    // 获取说话角色
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalk")]
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalkAddFt")]
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalkRepeat")]
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalkRepeatAdd")]
    [HarmonyPostfix]
    public static void TagTalk_Postfix(KagTagSupport tag_data, YotogiKagManager __instance)
    {
        LogManager.Debug("TagTalk_Postfix called");

        if (tag_data.IsValid("voice"))
        {
            var speakingMaid = BaseKagManager.GetVoiceTargetMaid(tag_data);

            if (speakingMaid is null)
                return;

            YotogiSubtitleManager.CurrentSpeaker = speakingMaid;
            var voiceId = tag_data.GetTagProperty("voice").AsString();
            YotogiSubtitleManager.CurrentVoiceId = voiceId;

            // 为每个Maid启动监听协程（如果尚未启动）
            YotogiSubtitleManager.StartMaidMonitoringCoroutine(speakingMaid);

            LogManager.Debug($"TagTalk_Postfix tag_data voiceId: {voiceId}");
            LogManager.Debug($"TagTalk_Postfix tag_data name: {tag_data.GetTagProperty("name").AsString()}");
            LogManager.Debug($"TagTalk_Postfix speakingMaid: {speakingMaid.status.fullNameJpStyle}");
        }
    }


    [HarmonyPatch(typeof(YotogiKagManager), "TagHitRet")]
    [HarmonyPrefix]
    public static void HitRet_Prefix(YotogiKagManager __instance)
    {
        var text = __instance.kag_.GetText();
        LogManager.Debug($"HitRet_Prefix called with text: {text}");
        LogManager.Debug($"HitRet_Prefix instance.kag_.GetCurrentLabel(): {__instance.kag_.GetCurrentLabel()}");
        LogManager.Debug($"HitRet_Prefix instance.kag_.GetCurrentFileName(): {__instance.kag_.GetCurrentFileName()}");
        LogManager.Debug($"HitRet_Prefix instance.kag_.GetCurrentLine(): {__instance.kag_.GetCurrentLine()}");
        if (!string.IsNullOrEmpty(text))
        {
            if (YotogiSubtitleManager.CurrentSpeaker is null)
                return;

            // 建立VoiceID和文本的映射关系
            if (!string.IsNullOrEmpty(YotogiSubtitleManager.CurrentVoiceId))
            {
                YotogiSubtitleManager.VoiceIdToTextMap[YotogiSubtitleManager.CurrentVoiceId] = text;
                LogManager.Debug(
                    $"HitRet_Prefix Create a mapping: VoiceID={YotogiSubtitleManager.CurrentVoiceId}, Text={text}");
            }
        }
    }

    // 在游戏场景结束或切换时清理所有协程
    [HarmonyPatch(typeof(YotogiManager), "OnDestroy")]
    [HarmonyPostfix]
    public static void OnYotogiSceneDestroy()
    {
        YotogiSubtitleManager.CleanupAllCoroutines();
    }
}