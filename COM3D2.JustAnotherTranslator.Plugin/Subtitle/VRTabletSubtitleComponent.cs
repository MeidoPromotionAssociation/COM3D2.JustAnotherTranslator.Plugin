using System;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     VR平板字幕组件，在VR模式下将字幕显示在虚拟平板上
/// </summary>
public class VRTabletSubtitleComponent : BaseSubtitleComponent
{
    // VR相关
    private Transform _vrHeadTransform;
    private Transform _vrTabletContainer;

    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    public override void Initialize(SubtitleConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // 查找VR头部变换
        InitVRHeadTransform();

        // 创建UI
        CreateSubtitleUI();

        // 应用配置
        ApplyConfig();

        gameObject.SetActive(false);
        LogManager.Debug("VR平板字幕组件已初始化/VR tablet subtitle component initialized");
    }

    /// <summary>
    ///     初始化VR头部变换
    /// </summary>
    private void InitVRHeadTransform()
    {
        // 查找VR头部变换
        var camera = Camera.main;
        if (camera != null)
        {
            _vrHeadTransform = camera.transform;
            LogManager.Debug("已找到VR头部变换/VR head transform found");
        }
        else
        {
            LogManager.Error(
                "无法找到主相机，VR字幕将无法正确定位/Cannot find main camera, VR subtitle will not be properly positioned");
        }
    }

    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected override void CreateSubtitleUI()
    {
        // 创建VR平板容器
        var vrTabletObject = new GameObject("VRTabletContainer");
        vrTabletObject.transform.SetParent(transform);
        vrTabletObject.transform.localPosition = Vector3.zero;
        vrTabletObject.transform.localRotation = Quaternion.identity;
        vrTabletObject.transform.localScale = Vector3.one;
        _vrTabletContainer = vrTabletObject.transform;

        // 设置平板位置和旋转
        SetTabletTransform();

        // 创建Canvas
        var canvasObject = new GameObject("SubtitleCanvas");
        canvasObject.transform.SetParent(_vrTabletContainer);
        canvasObject.transform.localPosition = Vector3.zero;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one;

        _canvas = canvasObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;

        _canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        _canvasScaler.dynamicPixelsPerUnit = 30;

        _canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        var rectTransform = _canvas.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1.5f, 0.8f);

        // 创建平板模型（简单矩形）
        var tabletModelObject = new GameObject("TabletModel");
        tabletModelObject.transform.SetParent(_canvas.transform);
        var tabletRect = tabletModelObject.AddComponent<RectTransform>();
        tabletRect.anchorMin = new Vector2(0, 0);
        tabletRect.anchorMax = new Vector2(1, 1);
        tabletRect.offsetMin = new Vector2(-0.05f, -0.05f);
        tabletRect.offsetMax = new Vector2(0.05f, 0.05f);
        var tabletImage = tabletModelObject.AddComponent<Image>();
        tabletImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        // 创建背景
        var backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(_canvas.transform);
        _backgroundRect = backgroundObject.AddComponent<RectTransform>();
        _backgroundRect.anchorMin = new Vector2(0, 0);
        _backgroundRect.anchorMax = new Vector2(1, 1);
        _backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        _backgroundRect.sizeDelta = Vector2.zero;
        _backgroundRect.localPosition = Vector3.zero;

        _backgroundImage = backgroundObject.AddComponent<Image>();
        _backgroundImage.color = _config.BackgroundColor;

        // 创建说话者名称文本
        var speakerObject = new GameObject("SpeakerName");
        speakerObject.transform.SetParent(backgroundObject.transform);
        _speakerTextRect = speakerObject.AddComponent<RectTransform>();
        _speakerTextRect.anchorMin = new Vector2(0, 1);
        _speakerTextRect.anchorMax = new Vector2(1, 1);
        _speakerTextRect.pivot = new Vector2(0.5f, 1);
        _speakerTextRect.sizeDelta = new Vector2(0, 0.15f);
        _speakerTextRect.anchoredPosition = new Vector2(0, -0.02f);

        _speakerText = speakerObject.AddComponent<Text>();
        _speakerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _speakerText.fontSize = _config.FontSize - 4; // 说话者名称字体略小
        _speakerText.alignment = TextAnchor.MiddleCenter;
        _speakerText.color = _config.SpeakerNameColor;
        _speakerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _speakerText.verticalOverflow = VerticalWrapMode.Overflow;

        // 说话者名称描边
        var speakerOutline = speakerObject.AddComponent<Outline>();
        speakerOutline.effectColor = _config.OutlineColor;
        speakerOutline.effectDistance = new Vector2(_config.OutlineWidth * 0.005f, _config.OutlineWidth * 0.005f);

        // 创建正文文本
        var textObject = new GameObject("Text");
        textObject.transform.SetParent(backgroundObject.transform);
        _textRect = textObject.AddComponent<RectTransform>();
        _textRect.anchorMin = new Vector2(0, 0);
        _textRect.anchorMax = new Vector2(1, 1);
        _textRect.pivot = new Vector2(0.5f, 0.5f);
        _textRect.sizeDelta = Vector2.zero;
        _textRect.offsetMin = new Vector2(0.05f, 0.05f); // 左下边距
        _textRect.offsetMax = new Vector2(-0.05f, _config.EnableSpeakerName ? -0.17f : -0.05f); // 右上边距

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
        _outline.effectDistance = new Vector2(_config.OutlineWidth * 0.005f, _config.OutlineWidth * 0.005f);

        // 设置活动状态
        _speakerText.gameObject.SetActive(_config.EnableSpeakerName);
    }

    /// <summary>
    ///     设置平板变换（位置和旋转）
    /// </summary>
    private void SetTabletTransform()
    {
        if (_vrTabletContainer == null || _vrHeadTransform == null) return;

        // 设置平板位置和旋转
        var headPosition = _vrHeadTransform.position;
        var headForward = _vrHeadTransform.forward;
        var headRight = _vrHeadTransform.right;
        var headUp = _vrHeadTransform.up;

        // 应用配置中的位置和旋转偏移
        var positionOffset = new Vector3(
            _config.VRTabletPositionX,
            _config.VRTabletPositionY,
            _config.VRTabletPositionZ
        );

        var rotationOffset = Quaternion.Euler(
            _config.VRTabletRotationX,
            _config.VRTabletRotationY,
            _config.VRTabletRotationZ
        );

        // 计算平板位置（在头部偏移位置）
        var position = headPosition +
                       headForward * positionOffset.z +
                       headRight * positionOffset.x +
                       headUp * positionOffset.y;

        // 更新位置和旋转
        _vrTabletContainer.position = position;
        _vrTabletContainer.rotation = Quaternion.LookRotation(headForward, headUp) * rotationOffset;
    }

    /// <summary>
    ///     更新字幕配置
    /// </summary>
    /// <param name="config">新的字幕配置</param>
    public override void UpdateConfig(SubtitleConfig config)
    {
        base.UpdateConfig(config);

        // 更新平板位置和旋转
        SetTabletTransform();
    }
}