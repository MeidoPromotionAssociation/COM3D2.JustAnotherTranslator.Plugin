using COM3D2.JustAnotherTranslator.Plugin.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     VR平板字幕组件，在 VR 模式中原有 UI 会被放到一个悬浮平板电脑上
/// </summary>
public class VRTabletSubtitleComponent : BaseSubtitleComponent
{
    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected override void CreateSubtitleUI()
    {
        // 在 VR 模式中原有 UI 会被放到一个悬浮平板电脑上，见 OvrTablet
        var canvasObj = new GameObject("JAT_Subtitle_SubtitleCanvas_VR_Tablet");

        // var ovrTablet = FindObjectOfType<OvrTablet>();
        // if (ovrTablet is not null)
        // {
        //     var screenTransform = ovrTablet.transform.Find("Screen");
        //     if (screenTransform is null)
        //     {
        //         LogManager.Warning(
        //             "OvrTablet Screen not found, subtitles mar will not display on VR tablet/OvrTablet Screen 未找到，VR平板电脑上的字幕可能不会显示");
        //         canvasObj.transform.SetParent(transform, false);
        //     }
        //     else
        //     {
        //         canvasObj.transform.SetParent(screenTransform, false);
        //         canvasObj.layer = screenTransform.gameObject.layer;
        //     }
        // }

        var systemUI = GameObject.Find("SystemUI Root");
        if (systemUI is not null)
        {
            canvasObj.transform.SetParent(systemUI.transform, false);
            canvasObj.layer = systemUI.layer;
        }
        else
        {
            canvasObj.transform.SetParent(transform, false);
            LogManager.Warning(
                "SystemUI Root not found, subtitles mar will not display on VR tablet/SystemUI Root 未找到，VR平板电脑上的字幕可能不会显示");
        }

        Canvas = canvasObj.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.WorldSpace;
        Canvas.sortingOrder = 32767; // 确保在最上层显示

        // Canvas.transform.localPosition = Vector3.zero;
        // Canvas.transform.localRotation = Quaternion.identity;
        // Canvas.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // 添加画布缩放器
        CanvasScaler = Canvas.gameObject.AddComponent<CanvasScaler>();
        CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        CanvasScaler.referenceResolution = new Vector2(1920, 1080);
        CanvasScaler.matchWidthOrHeight = 0.5f;

        // 创建背景面板
        var backgroundObj = new GameObject("JAT_Subtitle_SubtitleBackground");
        backgroundObj.transform.SetParent(Canvas.transform, false);
        BackgroundImage = backgroundObj.AddComponent<Image>();
        BackgroundImage.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景

        // 设置背景位置和大小
        var backgroundRect = BackgroundImage.rectTransform;
        backgroundRect.anchorMin = new Vector2(0, Config.VerticalPosition);
        backgroundRect.anchorMax = new Vector2(1, Config.VerticalPosition);
        backgroundRect.pivot = new Vector2(0.5f, 0);
        backgroundRect.sizeDelta = new Vector2(0, Config.BackgroundHeight);


        // 创建文本对象
        var textObj = new GameObject("JAT_Subtitle_SubtitleText");
        textObj.transform.SetParent(backgroundRect, false);
        TextComponent = textObj.AddComponent<Text>();

        // 设置文本位置和大小
        var textRect = TextComponent.rectTransform;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(-40, -20); // 左右上下各留出10像素的边距
        textRect.anchoredPosition = new Vector2(0, 0);

        // 设置默认文本样式
        TextComponent.alignment = TextAnchor.MiddleCenter;
        TextComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        TextComponent.verticalOverflow = VerticalWrapMode.Truncate;

        // 设置文本和背景不拦截点击事件
        TextComponent.raycastTarget = false;
        BackgroundImage.raycastTarget = false;

        // 添加描边组件
        Outline = TextComponent.gameObject.AddComponent<Outline>();
        Outline.enabled = false;

        LogManager.Debug("VRTabletSubtitleComponent Subtitle UI created");
    }
}