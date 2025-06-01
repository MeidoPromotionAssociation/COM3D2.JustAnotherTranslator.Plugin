using System;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.ConfigStrategy;

/// <summary>
///     VR空间字幕配置策略
/// </summary>
public class VRSpaceSubtitleConfigStrategy : BaseSubtitleConfigStrategy
{
    /// <summary>
    ///     创建VR字幕UI
    /// </summary>
    /// <param name="component">字幕组件</param>
    public override void CreateSubtitleUI(SubtitleComponent component)
    {
        LogManager.Debug("Creating VR space subtitle UI");

        // 创建世界空间Canvas
        var canvas = component.gameObject.AddComponent<Canvas>();
        component.SetCanvas(canvas);
        canvas.renderMode = RenderMode.WorldSpace;

        // 创建用于VR头部跟踪的容器
        var vrContainer = new GameObject("VRSubtitleContainer").transform;
        vrContainer.SetParent(component.transform, false);
        component.SetVRSubtitleContainer(vrContainer);

        // 将Canvas放在容器下
        canvas.transform.SetParent(vrContainer, false);

        // 创建Canvas缩放器
        var canvasScaler = component.gameObject.AddComponent<CanvasScaler>();
        component.SetCanvasScaler(canvasScaler);
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;

        // 创建射线检测器
        component.gameObject.AddComponent<GraphicRaycaster>();

        // 创建Canvas组
        var canvasGroup = component.gameObject.AddComponent<CanvasGroup>();
        component.SetCanvasGroup(canvasGroup);

        // 创建背景面板
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvas.transform, false);
        var bgImage = bgObj.AddComponent<Image>();
        component.SetBackgroundImage(bgImage);
        bgImage.color = new Color(0, 0, 0, 0.8f); // 默认黑色背景，80%透明度

        // 获取背景RectTransform
        var bgRect = bgObj.GetComponent<RectTransform>();
        component.SetBackgroundRect(bgRect);
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.sizeDelta = new Vector2(0, 200); // 默认高度
        bgRect.anchoredPosition = Vector2.zero;

        // 创建文本
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(bgRect.transform, false);
        var text = textObj.AddComponent<Text>();
        component.SetTextComponent(text);
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 36;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = "VR空间字幕";

        // 添加描边
        var outline = text.gameObject.AddComponent<Outline>();
        component.SetOutline(outline);
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);

        // 设置文本RectTransform
        var textRect = text.GetComponent<RectTransform>();
        component.SetTextRect(textRect);
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        LogManager.Debug("VR space subtitle UI created");
    }

    /// <summary>
    ///     应用VR字幕配置
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
            // 设置世界空间Canvas参数
            var canvas = component.GetCanvas();
            canvas.renderMode = RenderMode.WorldSpace;

            // 头部变换已在SubtitleComponent的InitVRComponents中设置
            var vrHeadTransform = component.GetVRHeadTransform();
            var vrSubtitleContainer = component.GetVRSubtitleContainer();
            
            if (vrHeadTransform != null && vrSubtitleContainer != null)
            {
                // 设置初始位置
                var headForward = vrHeadTransform.forward;
                var headUp = vrHeadTransform.up;
                var headRight = vrHeadTransform.right;

                // 应用偏移角度（水平和垂直）
                var verticalRotation = Quaternion.AngleAxis(config.VRSubtitleVerticalOffset, headRight);
                var horizontalRotation = Quaternion.AngleAxis(config.VRSubtitleHorizontalOffset, headUp);
                var offsetDirection = horizontalRotation * verticalRotation * headForward;

                // 计算最终位置（头部位置 + 偏移方向 * 距离）
                var targetPosition = vrHeadTransform.position +
                                     offsetDirection * config.VRSubtitleDistance;

                // 设置初始位置
                vrSubtitleContainer.position = targetPosition;

                // 设置初始朝向
                vrSubtitleContainer.rotation = Quaternion.LookRotation(
                    vrSubtitleContainer.position - vrHeadTransform.position);

                // 设置Canvas的尺寸
                canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(
                    config.VRSubtitleWidth * 1000,
                    config.BackgroundHeight * 2);

                // 设置Canvas的缩放
                canvas.transform.localScale = Vector3.one * config.VRSubtitleScale * 0.001f;
            }
            else
            {
                LogManager.Warning("VR head transform not found, cannot position VR subtitle");
            }

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

            LogManager.Debug("VR space subtitle config applied");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Failed to apply VR subtitle config: {ex.Message}");
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
            HandleShowSubtitle(component, currentText, currentSpeakerName, 0f);
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

        var vrHeadTransform = component.GetVRHeadTransform();
        var vrSubtitleContainer = component.GetVRSubtitleContainer();
        
        // 如果VR头部跟踪可用，更新位置
        if (vrHeadTransform != null && vrSubtitleContainer != null)
        {
            var config = component.GetConfig();
            
            // 设置初始位置
            var headForward = vrHeadTransform.forward;
            var headUp = vrHeadTransform.up;
            var headRight = vrHeadTransform.right;

            // 应用偏移角度（水平和垂直）
            var verticalRotation = Quaternion.AngleAxis(config.VRSubtitleVerticalOffset, headRight);
            var horizontalRotation = Quaternion.AngleAxis(config.VRSubtitleHorizontalOffset, headUp);
            var offsetDirection = horizontalRotation * verticalRotation * headForward;

            // 计算最终位置（头部位置 + 偏移方向 * 距离）
            var targetPosition = vrHeadTransform.position +
                                offsetDirection * config.VRSubtitleDistance;

            // 设置初始位置
            vrSubtitleContainer.position = targetPosition;

            // 设置朝向（总是面向玩家）
            vrSubtitleContainer.rotation = Quaternion.LookRotation(
                vrSubtitleContainer.position - vrHeadTransform.position);
        }

        LogManager.Debug($"VR space subtitle shown: {text}");
    }

    /// <summary>
    ///     处理字幕隐藏
    /// </summary>
    /// <param name="component">字幕组件</param>
    public override void HandleHideSubtitle(SubtitleComponent component)
    {
        base.HandleHideSubtitle(component);
        LogManager.Debug("VR subtitle hidden");
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

        LogManager.Debug("VR space subtitle config updated");
    }
}