using COM3D2.JustAnotherTranslator.Plugin.Translator;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks;

// <summary>
//      用于处理 Yotogi 字幕的 Harmony 补丁
//      见 YotogiKagManager
//      首先会命中 @talk 标签，开始说话
//      然后会命中 @hitret 标签，结束脚本段落
//      此时在 @hitret 可以拿到脚本段落的文本
//      例如
//      ;*L1|
//      ;@talk voice=H0_GP01_17908 name=[HF1]
//      ;う～……どうして３人でするの……？　おかしくないかなぁ、ご主人様……
//      ;@hitret
//      但是游戏可能会提取执行下一个 @talk 标签，但语音排队播放，因此需要判断角色说话的 VoiceID
//      可以从 maid.AudioMan.FileName 中获取
// <summary>
public static class YotogiSubtitlePatch
{
    // 获取说话角色
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalk")]
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalkAddFt")]
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalkRepeat")]
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalkRepeatAdd")]
    [HarmonyPostfix]
    public static void YotogiKagManager_TagTalk_Postfix(KagTagSupport tag_data, YotogiKagManager __instance)
    {
        LogManager.Debug("YotogiKagManager_TagTalk_Postfix called");

        if (tag_data.IsValid("voice"))
        {
            var speakingMaid = BaseKagManager.GetVoiceTargetMaid(tag_data);

            if (speakingMaid is null)
                return;

            var voiceId = tag_data.GetTagProperty("voice").AsString();

            SubtitleManager.SetSubtitleType(JustAnotherTranslator.SubtitleTypeEnum.Yotogi);
            SubtitleManager.SetCurrentVoiceId(voiceId);
            SubtitleManager.SetCurrentSpeaker(speakingMaid);
            SubtitleManager.StartMaidMonitoringCoroutine(speakingMaid);

            LogManager.Debug(
                $"YotogiKagManager_TagTalk_Postfix tag_data name: {tag_data.GetTagProperty("name").AsString()}");
            LogManager.Debug($"YotogiKagManager_TagTalk_Postfix tag_data voiceId: {voiceId}");
            LogManager.Debug($"YotogiKagManager_TagTalk_Postfix speakingMaid: {speakingMaid.status.fullNameJpStyle}");
        }
    }


    // 在脚本段落结束时，获取文本
    [HarmonyPatch(typeof(YotogiKagManager), "TagHitRet")]
    [HarmonyPrefix]
    public static void YotogiKagManager_HitRet_Prefix(YotogiKagManager __instance)
    {
        var text = __instance.kag_.GetText();
        LogManager.Debug($"YotogiKagManager_HitRet_Prefix called with text: {text}");
        LogManager.Debug(
            $"YotogiKagManager_HitRet_Prefix instance.kag_.GetCurrentLabel(): {__instance.kag_.GetCurrentLabel()}");
        LogManager.Debug(
            $"YotogiKagManager_HitRet_Prefix instance.kag_.GetCurrentFileName(): {__instance.kag_.GetCurrentFileName()}");
        LogManager.Debug(
            $"YotogiKagManager_HitRet_Prefix instance.kag_.GetCurrentLine(): {__instance.kag_.GetCurrentLine()}");

        SubtitleManager.SetSubtitleType(JustAnotherTranslator.SubtitleTypeEnum.Yotogi);
        SubtitleManager.SetVoiceTextMapping(text, "YotogiKagManager_HitRet_Prefix");
    }
}