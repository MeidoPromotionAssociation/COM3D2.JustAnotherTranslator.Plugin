using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     屏幕字幕组件，在游戏屏幕上显示字幕
/// </summary>
public class ScreenSubtitleComponent : BaseSubtitleComponent
{
    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected override void CreateSubtitleUI()
    {
        // 创建Canvas
        var canvasObject = new GameObject("SubtitleCanvas");
        canvasObject.transform.SetParent(transform);
        canvasObject.transform.localPosition = Vector3.zero;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one;
        
        _canvas = canvasObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999; // 确保字幕在最上层显示
        
        _canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = new Vector2(1920, 1080);
        _canvasScaler.matchWidthOrHeight = 0.5f; // 宽高适配混合
        
        _canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        
        // 创建背景
        var backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(canvasObject.transform);
        _backgroundRect = backgroundObject.AddComponent<RectTransform>();
        _backgroundRect.anchorMin = new Vector2(0.5f, 0);
        _backgroundRect.anchorMax = new Vector2(0.5f, 0);
        _backgroundRect.pivot = new Vector2(0.5f, 0);
        _backgroundRect.sizeDelta = new Vector2(_config.Width, 100); // 高度会根据文本自动调整
        _backgroundRect.anchoredPosition = new Vector2(_config.PositionX, _config.PositionY);
        
        _backgroundImage = backgroundObject.AddComponent<Image>();
        _backgroundImage.color = _config.BackgroundColor;
        
        // 创建说话者名称文本
        var speakerObject = new GameObject("SpeakerName");
        speakerObject.transform.SetParent(backgroundObject.transform);
        _speakerTextRect = speakerObject.AddComponent<RectTransform>();
        _speakerTextRect.anchorMin = new Vector2(0, 1);
        _speakerTextRect.anchorMax = new Vector2(1, 1);
        _speakerTextRect.pivot = new Vector2(0.5f, 0);
        _speakerTextRect.sizeDelta = new Vector2(0, 30);
        _speakerTextRect.anchoredPosition = new Vector2(0, 0);
        
        _speakerText = speakerObject.AddComponent<Text>();
        _speakerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _speakerText.fontSize = _config.FontSize - 4; // 说话者名称字体略小
        _speakerText.alignment = TextAnchor.MiddleLeft;
        _speakerText.color = _config.SpeakerNameColor;
        _speakerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _speakerText.verticalOverflow = VerticalWrapMode.Overflow;
        
        // 说话者名称描边
        var speakerOutline = speakerObject.AddComponent<Outline>();
        speakerOutline.effectColor = _config.OutlineColor;
        speakerOutline.effectDistance = new Vector2(_config.OutlineWidth, _config.OutlineWidth);
        
        // 创建正文文本
        var textObject = new GameObject("Text");
        textObject.transform.SetParent(backgroundObject.transform);
        _textRect = textObject.AddComponent<RectTransform>();
        _textRect.anchorMin = new Vector2(0, 0);
        _textRect.anchorMax = new Vector2(1, 1);
        _textRect.pivot = new Vector2(0.5f, 0.5f);
        _textRect.sizeDelta = Vector2.zero;
        _textRect.offsetMin = new Vector2(10, 10); // 左下边距
        _textRect.offsetMax = new Vector2(-10, _config.EnableSpeakerName ? -30 : -10); // 右上边距
        
        _text = textObject.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _text.fontSize = _config.FontSize;
        _text.alignment = TextAnchor.MiddleCenter;
        _text.color = _config.TextColor;
        _text.horizontalOverflow = HorizontalWrapMode.Wrap;
        _text.verticalOverflow = VerticalWrapMode.Overflow;
        
        // 添加描边
        _outline = textObject.AddComponent<Outline>();
        _outline.effectColor = _config.OutlineColor;
        _outline.effectDistance = new Vector2(_config.OutlineWidth, _config.OutlineWidth);
        
        // 设置活动状态
        _speakerText.gameObject.SetActive(_config.EnableSpeakerName);
    }
}
