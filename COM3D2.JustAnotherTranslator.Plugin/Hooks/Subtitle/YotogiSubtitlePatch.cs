using System;
using COM3D2.JustAnotherTranslator.Plugin.Manger;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Subtitle;

/// <summary>
///     用于处理 Yotogi 字幕的 Harmony 补丁
///     见 YotogiKagManager
///     首先会命中 @talk 标签，开始说话
///     然后会命中 @hitret 标签，结束脚本段落
///     此时在 @hitret 可以拿到脚本段落的文本
///     例如
///     ;*L1|
///     ;@talk voice=H0_GP01_17908 name=[HF1]
///     ;う～……どうして３人でするの……？　おかしくないかなぁ、ご主人様……
///     ;@hitret
///     但是游戏可能会提取执行下一个 @talk 标签，但语音排队播放，因此需要判断角色说话的 VoiceID
///     可以从 maid.AudioMan.FileName 中获取
/// </summary>
public static class YotogiSubtitlePatch
{
    /// <summary>
    ///     捕获 @talk @TalkAddFt @TalkRepeat @TalkRepeatAdd 标签
    ///     获取说话角色
    /// </summary>
    /// <param name="tag_data"></param>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalk")]
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalkAddFt")]
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalkRepeat")]
    [HarmonyPatch(typeof(YotogiKagManager), "TagTalkRepeatAdd")]
    [HarmonyPostfix]
    public static void YotogiKagManager_TagTalk_Postfix(KagTagSupport tag_data,
        YotogiKagManager __instance)
    {
        try
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
                SubtitleManager.StartYotogiMaidMonitoringCoroutine(speakingMaid);

                LogManager.Debug(
                    $"YotogiKagManager_TagTalk_Postfix tag_data name: {tag_data.GetTagProperty("name").AsString()}");
                LogManager.Debug($"YotogiKagManager_TagTalk_Postfix tag_data voiceId: {voiceId}");
                LogManager.Debug(
                    $"YotogiKagManager_TagTalk_Postfix speakingMaid: {speakingMaid.status.fullNameJpStyle}");
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiKagManager_TagTalk_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     新夜伽模式暂停 Play 并跳回 ADV KAG 时结束夜伽字幕。
    ///     学习剧情等 skl 脚本正是通过该路径在夜伽后继续播放普通对话。
    /// </summary>
    [HarmonyPatch(typeof(YotogiPlayManager), "Suspend")]
    [HarmonyPostfix]
    public static void YotogiPlayManager_Suspend_Postfix()
    {
        EndYotogiSubtitleSession("YotogiPlayManager.Suspend");
    }

    /// <summary>
    ///     当前新夜伽技能结束或切换下一技能时结束对应字幕监控。
    /// </summary>
    [HarmonyPatch(typeof(YotogiPlayManager), "OnNextSkillMove")]
    [HarmonyPostfix]
    public static void YotogiPlayManager_OnNextSkillMove_Postfix()
    {
        EndYotogiSubtitleSession("YotogiPlayManager.OnNextSkillMove");
    }

    /// <summary>
    ///     旧夜伽模式的同等技能结束边界。
    /// </summary>
    [HarmonyPatch(typeof(YotogiOldPlayManager), "OnNextSkillMove")]
    [HarmonyPostfix]
    public static void YotogiOldPlayManager_OnNextSkillMove_Postfix()
    {
        EndYotogiSubtitleSession("YotogiOldPlayManager.OnNextSkillMove");
    }

    private static void EndYotogiSubtitleSession(string callBy)
    {
        try
        {
            // Suspend/OnNextSkillMove 存在提前返回路径；只有游戏确实禁用了 Yotogi KAG，
            // 才把它视为字幕会话边界。
            var yotogiKag = GameMain.Instance?.ScriptMgr?.yotogi_kag;
            if (yotogiKag != null && yotogiKag.enabled)
            {
                LogManager.Debug(
                    $"Skipping Yotogi subtitle session cleanup from {callBy}: Yotogi KAG is still enabled");
                return;
            }

            LogManager.Debug($"Ending Yotogi subtitle session from {callBy}");
            SubtitleManager.EndYotogiSubtitleSession();
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"EndYotogiSubtitleSession called by {callBy} failed/结束夜伽字幕会话失败 {e.Message}\n{e.StackTrace}");
        }
    }


    /// <summary>
    ///     捕获 @hitret 标签
    ///     在脚本段落结束时，获取文本
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(YotogiKagManager), "TagHitRet")]
    [HarmonyPrefix]
    public static void YotogiKagManager_HitRet_Prefix(YotogiKagManager __instance)
    {
        try
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
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiKagManager_HitRet_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }
}
