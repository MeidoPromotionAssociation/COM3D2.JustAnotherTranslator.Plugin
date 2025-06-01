using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.ConfigStrategy;

/// <summary>
///     VR平板字幕配置策略
/// </summary>
public class VRTabletSubtitleConfigStrategy : BaseSubtitleConfigStrategy
{
    /// <summary>
    ///     创建VR平板字幕UI
    /// </summary>
    /// <param name="component">字幕组件</param>
    public override void CreateSubtitleUI(SubtitleComponent component)
    {
        LogManager.Debug("Creating VR tablet subtitle UI");

        // 创建世界空间Canvas
        var canvasObj = new GameObject("VRTabletSubtitleCanvas");

        // 查找OvrTablet组件并设置父对象
        var ovrTablet = Object.FindObjectOfType<OvrTablet>();
        if (ovrTablet is not null)
        {
            var screenTransform = ovrTablet.transform.Find("Screen");
            if (screenTransform is null)
            {
                LogManager.Warning(
                    "OvrTablet Screen not found, subtitles mar will not display on VR tablet/OvrTablet Screen 未找到，VR平板电脑上的字幕可能不会显示");
                canvasObj.transform.SetParent(component.transform, false);
            }
            else
            {
                canvasObj.transform.SetParent(screenTransform, false);
                canvasObj.layer = screenTransform.gameObject.layer;
            }
        }
        else
        {
            canvasObj.transform.SetParent(component.transform, false);
        }

        var canvas = canvasObj.AddComponent<Canvas>();
        component.SetCanvas(canvas);
        canvas.renderMode = RenderMode.WorldSpace;

        // 添加Canvas缩放器
        var canvasScaler = canvas.gameObject.AddComponent<CanvasScaler>();
        component.SetCanvasScaler(canvasScaler);
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;

        // 添加CanvasGroup用于控制透明度
        var canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        component.SetCanvasGroup(canvasGroup);

        // 添加射线检测
        var raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();

        // 创建背景面板
        var backgroundObj = new GameObject("TabletSubtitleBackground");
        backgroundObj.transform.SetParent(canvas.transform, false);
        var backgroundImage = backgroundObj.AddComponent<Image>();
        component.SetBackgroundImage(backgroundImage);
        backgroundImage.color = new Color(0, 0, 0, 0.5f);

        // 设置背景位置和大小
        var backgroundRect = backgroundImage.rectTransform;
        component.SetBackgroundRect(backgroundRect);
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 1);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.sizeDelta = Vector2.zero;

        // 创建文本对象
        var textObj = new GameObject("TabletSubtitleText");
        textObj.transform.SetParent(backgroundRect, false);
        var text = textObj.AddComponent<Text>();
        component.SetTextComponent(text);

        // 设置文本位置和大小
        var textRect = text.rectTransform;
        component.SetTextRect(textRect);
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(-40, -20); // 左右上下各留出20像素的边距
        textRect.anchoredPosition = Vector2.zero;

        // 设置默认文本样式
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        // 添加描边组件
        var outline = text.gameObject.AddComponent<Outline>();
        component.SetOutline(outline);

        LogManager.Debug("VR tablet subtitle UI created");
    }

    /// <summary>
    ///     应用字幕配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    public override void ApplyConfig(SubtitleComponent component)
    {
        var config = component.GetConfig();
        if (config is null)
        {
            LogManager.Warning("Cannot apply config, subtitle config is null");
            return;
        }

        try
        {
            // 加载字体
            var font = Resources.GetBuiltinResource<Font>(config.FontName);
            if (font is null)
            {
                LogManager.Warning($"Font not found: {config.FontName}, using Arial instead");
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            // 设置文本样式
            var text = component.GetTextComponent();
            text.font = font;
            text.fontSize = config.FontSize;
            text.color = config.TextColor;
            text.alignment = config.TextAlignment;

            // 设置背景样式
            var bgColor = config.BackgroundColor;
            bgColor.a = config.BackgroundOpacity; // 设置不透明度
            var backgroundImage = component.GetBackgroundImage();
            backgroundImage.color = bgColor;

            // 设置描边
            var outline = component.GetOutline();
            outline.enabled = config.OutlineEnabled;
            outline.effectColor = config.OutlineColor;
            outline.effectDistance = new Vector2(
                config.OutlineWidth,
                config.OutlineWidth);

            LogManager.Debug("VR tablet subtitle config applied");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Failed to apply VR tablet subtitle config: {ex.Message}");
        }
    }

    /// <summary>
    ///     更新字幕UI配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="currentText">当前文本</param>
    /// <param name="currentSpeakerName">当前说话者名称</param>
    public override void UpdateUI(SubtitleComponent component, string currentText, string currentSpeakerName)
    {
        // 应用新配置
        ApplyConfig(component);

        // 如果当前有显示文本，则使用新配置重新显示
        if (!string.IsNullOrEmpty(currentText))
            HandleShowSubtitle(component, currentText, currentSpeakerName, 0);
    }

    /// <summary>
    ///     处理字幕显示
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="text">要显示的文本</param>
    /// <param name="speakerName">说话人名称</param>
    /// <param name="duration">显示持续时间，0 为一直显示</param>
    public override void HandleShowSubtitle(SubtitleComponent component, string text, string speakerName,
        float duration)
    {
        base.HandleShowSubtitle(component, text, speakerName, duration);
        LogManager.Debug($"VR tablet subtitle shown: {text}");
    }

    /// <summary>
    ///     处理字幕隐藏
    /// </summary>
    /// <param name="component">字幕组件</param>
    public override void HandleHideSubtitle(SubtitleComponent component)
    {
        base.HandleHideSubtitle(component);
        LogManager.Debug("VR tablet subtitle hidden");
    }

    /// <summary>
    ///     更新字幕配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="config">新的字幕配置</param>
    public override void UpdateConfig(SubtitleComponent component, SubtitleConfig config)
    {
        // 调用基类方法
        base.UpdateConfig(component, config);

        LogManager.Debug("VR tablet subtitle config updated");
    }
}