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

        var ovrTablet = FindObjectOfType<OvrTablet>();
        if (ovrTablet is not null)
        {
            var screenTransform = ovrTablet.transform.Find("Screen");
            if (screenTransform is null)
            {
                LogManager.Warning(
                    "OvrTablet Screen not found, subtitles mar will not display on VR tablet/OvrTablet Screen 未找到，VR平板电脑上的字幕可能不会显示");
                canvasObj.transform.SetParent(transform, false);
            }
            else
            {
                canvasObj.transform.SetParent(screenTransform, false);
                canvasObj.layer = screenTransform.gameObject.layer;
            }
        }

        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 32767; // 确保在最上层显示

        _canvas.transform.localPosition = Vector3.zero;
        _canvas.transform.localRotation = Quaternion.identity;
        _canvas.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // 添加画布缩放器
        _canvasScaler = _canvas.gameObject.AddComponent<CanvasScaler>();
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = new Vector2(1920, 1080);
        _canvasScaler.matchWidthOrHeight = 0.5f;

        // 创建背景面板
        var backgroundObj = new GameObject("JAT_Subtitle_SubtitleBackground");
        backgroundObj.transform.SetParent(_canvas.transform, false);
        _backgroundImage = backgroundObj.AddComponent<Image>();
        _backgroundImage.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景

        // 设置背景位置和大小
        var backgroundRect = _backgroundImage.rectTransform;
        backgroundRect.anchorMin = new Vector2(0, _config.VerticalPosition);
        backgroundRect.anchorMax = new Vector2(1, _config.VerticalPosition);
        backgroundRect.pivot = new Vector2(0.5f, 0);
        backgroundRect.sizeDelta = new Vector2(0, _config.BackgroundHeight);


        // 创建文本对象
        var textObj = new GameObject("JAT_Subtitle_SubtitleText");
        textObj.transform.SetParent(backgroundRect, false);
        _text = textObj.AddComponent<Text>();

        // 设置文本位置和大小
        var textRect = _text.rectTransform;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(-40, -20); // 左右上下各留出10像素的边距
        textRect.anchoredPosition = new Vector2(0, 0);

        // 设置默认文本样式
        _text.alignment = TextAnchor.MiddleCenter;
        _text.horizontalOverflow = HorizontalWrapMode.Wrap;
        _text.verticalOverflow = VerticalWrapMode.Overflow;

        // 设置文本和背景不拦截点击事件
        _text.raycastTarget = false;
        _backgroundImage.raycastTarget = false;

        // 添加描边组件
        _outline = _text.gameObject.AddComponent<Outline>();
        _outline.enabled = false;

        LogManager.Debug("VR Subtitle UI created");
    }
}