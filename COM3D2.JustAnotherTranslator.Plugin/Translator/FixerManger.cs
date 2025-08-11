using System;
using System.Reflection;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.Fixer;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using MaidCafe;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

public static class FixerManger
{
    private static bool _initialized;

    private static Harmony _maidCafeDlcLineBreakCommentFixPatch;

    public static void Init()
    {
        if (_initialized) return;

        if (JustAnotherTranslator.EnableMaidCafeDlcLineBreakCommentFix.Value)
            RegisterMaidCafeDlcFixPatch();

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized) return;

        _maidCafeDlcLineBreakCommentFixPatch?.UnpatchSelf();
        _maidCafeDlcLineBreakCommentFixPatch = null;

        _initialized = false;
    }

    private static void RegisterMaidCafeDlcFixPatch()
    {
        if (MaidCafeManagerHelper.IsMaidCafeAvailable())
            try
            {
                var original = typeof(MaidCafeComment).GetMethod("LineBreakComment",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                var isPatchedByOthers = false;
                if (original != null)
                {
                    var patches = Harmony.GetPatchInfo(original);
                    isPatchedByOthers = patches?.Owners?.Count > 0;
                }

                var isPatchedByLegacy =
                    Harmony.HasAnyPatches(
                        "com.github.90135.com3d2_scripts_901.maidcafelinebreakcommentfix") ||
                    Harmony.HasAnyPatches(
                        "github.90135.com3d2_scripts_901.maidcafelinebreakcommentfix") ||
                    Harmony.HasAnyPatches(
                        "github.meidopromotionassociation.com3d2_scripts.maidcafelinebreakcommentfix");

                if (isPatchedByLegacy || isPatchedByOthers)
                    LogManager.Warning(
                        "MaidCafeDlcLineBreakCommentFix patch already applied by someone else, skipping/MaidCafeDlcLineBreakCommentFix 已被其他人应用，跳过\n" +
                        "if you got maid_cafe_line_break_fix.cs in your scripts folder, please remove it/如果你在 scripts 脚本文件夹中有 maid_cafe_line_break_fix.cs，请删除它");
                else
                    _maidCafeDlcLineBreakCommentFixPatch = Harmony.CreateAndPatchAll(
                        typeof(MaidCafeDlcLineBreakCommentFix),
                        "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.fixer.maidcafedlclinebreakcommentfix");
            }
            catch (Exception e)
            {
                LogManager.Warning(
                    $"Failed to patch MaidCafeDlcLineBreakCommentFix/补丁 MaidCafeDlcLineBreakCommentFix 失败: {e.Message}");
            }
    }
}