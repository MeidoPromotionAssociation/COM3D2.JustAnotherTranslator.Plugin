using System;
using System.Reflection;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.Fixer;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using MaidCafe;

namespace COM3D2.JustAnotherTranslator.Plugin.Manger;

public static class FixerManger
{
    private static bool _initialized;

    private static Harmony _maidCafeDlcLineBreakCommentFixPatch;

    private static Harmony _uiFontReplacePatch;

    private static Harmony _i2LocalizeNoneCjkFixPatch;

    public static void Init()
    {
        if (_initialized) return;

        if (JustAnotherTranslator.EnableMaidCafeDlcLineBreakCommentFix.Value)
            RegisterMaidCafeDlcFixPatch();

        if (JustAnotherTranslator.EnableUIFontReplace.Value)
            RegisterUIFontReplacePatch();

        if (JustAnotherTranslator.EnableNoneCjkFix.Value)
            RegisterI2LocalizeNoneCjkFixPatch();

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized) return;

        _maidCafeDlcLineBreakCommentFixPatch?.UnpatchSelf();
        _maidCafeDlcLineBreakCommentFixPatch = null;

        _uiFontReplacePatch?.UnpatchSelf();
        _uiFontReplacePatch = null;

        _i2LocalizeNoneCjkFixPatch?.UnpatchSelf();
        _i2LocalizeNoneCjkFixPatch = null;

        _initialized = false;
    }

    /// <summary>
    ///     Registers and applies the "UIFontReplace" Harmony patch to replace the game's default font with
    ///     a custom font.
    ///     Checks if the custom font is available and ensures patches are not already applied by other
    ///     plugins
    ///     or legacy scripts. If it detects prior patches, it logs a warning and skips this patch to
    ///     prevent duplication.
    ///     Otherwise, it attempts to apply the required Harmony patch to the target method.
    ///     Logs warnings in case of errors during the patching process or if conflicts with existing
    ///     patches are detected.
    /// </summary>
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


    /// <summary>
    ///     Registers and applies the Harmony patch for replacing default UI fonts with custom fonts.
    ///     This method initializes the patching process by creating and applying all the required
    ///     patches defined in the <c>UIFontReplace</c> class. Ensures that the patch is applied only once
    ///     and associates it with a unique Harmony ID. The purpose of these patches is to intercept
    ///     and modify text rendering logic, enabling the use of custom fonts across UI elements such as
    ///     Unity's Text components and UILabels.
    /// </summary>
    private static void RegisterUIFontReplacePatch()
    {
        _uiFontReplacePatch = Harmony.CreateAndPatchAll(
            typeof(UIFontReplace),
            "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.fixer.uifontreplace");
    }

    /// <summary>
    ///     Registers and applies the Harmony patch for fixing I2 Localization behaviors
    ///     that incorrectly apply when JAT provides non-CJK translations while the game
    ///     language code remains "ja".
    /// </summary>
    private static void RegisterI2LocalizeNoneCjkFixPatch()
    {
        _i2LocalizeNoneCjkFixPatch = Harmony.CreateAndPatchAll(
            typeof(I2LocalizeNoneCjkFix),
            "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.fixer.i2localizefix");
    }
}