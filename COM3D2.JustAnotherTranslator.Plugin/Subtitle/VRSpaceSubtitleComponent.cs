using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     VR空间字幕组件，在VR游戏空间中显示字幕，跟随玩家视角
/// </summary>
public class VRSpaceSubtitleComponent : BaseSubtitleComponent
{
    private Coroutine _followHeadCoroutine;

    // VR相关
    private Transform _vrHeadTransform;
    private Transform _vrSubtitleContainer;

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

        // 启动跟随头部的协程
        StartFollowHeadCoroutine();

        gameObject.SetActive(false);
        LogManager.Debug("VR空间字幕组件已初始化/VR space subtitle component initialized");
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
            LogManager.Error("无法找到主相机，VR字幕将无法跟随头部/Cannot find main camera, VR subtitle will not follow head");
        }
    }

    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected override void CreateSubtitleUI()
    {
        // 创建VR字幕容器
        var vrSubtitleObject = new GameObject("VRSubtitleContainer");
        vrSubtitleObject.transform.SetParent(transform);
        vrSubtitleObject.transform.localPosition = Vector3.zero;
        vrSubtitleObject.transform.localRotation = Quaternion.identity;
        vrSubtitleObject.transform.localScale = Vector3.one;
        _vrSubtitleContainer = vrSubtitleObject.transform;

        // 创建Canvas
        var canvasObject = new GameObject("SubtitleCanvas");
        canvasObject.transform.SetParent(_vrSubtitleContainer);
        canvasObject.transform.localPosition = Vector3.zero;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one;

        _canvas = canvasObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;

        _canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        _canvasScaler.dynamicPixelsPerUnit = 20;

        _canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        var rectTransform = _canvas.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(2, 1);

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
        _speakerTextRect.sizeDelta = new Vector2(0, 0.2f);
        _speakerTextRect.anchoredPosition = new Vector2(0, 0);

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
        speakerOutline.effectDistance = new Vector2(_config.OutlineWidth * 0.01f, _config.OutlineWidth * 0.01f);

        // 创建正文文本
        var textObject = new GameObject("Text");
        textObject.transform.SetParent(backgroundObject.transform);
        _textRect = textObject.AddComponent<RectTransform>();
        _textRect.anchorMin = new Vector2(0, 0);
        _textRect.anchorMax = new Vector2(1, 1);
        _textRect.pivot = new Vector2(0.5f, 0.5f);
        _textRect.sizeDelta = Vector2.zero;
        _textRect.offsetMin = new Vector2(0.1f, 0.1f); // 左下边距
        _textRect.offsetMax = new Vector2(-0.1f, _config.EnableSpeakerName ? -0.2f : -0.1f); // 右上边距

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
        _outline.effectDistance = new Vector2(_config.OutlineWidth * 0.01f, _config.OutlineWidth * 0.01f);

        // 设置活动状态
        _speakerText.gameObject.SetActive(_config.EnableSpeakerName);
    }

    /// <summary>
    ///     销毁字幕组件
    /// </summary>
    public override void Destroy()
    {
        StopFollowHeadCoroutine();
        base.Destroy();
    }

    /// <summary>
    ///     启动跟随头部的协程
    /// </summary>
    private void StartFollowHeadCoroutine()
    {
        StopFollowHeadCoroutine();
        _followHeadCoroutine = StartCoroutine(FollowHeadCoroutine());
    }

    /// <summary>
    ///     停止跟随头部的协程
    /// </summary>
    private void StopFollowHeadCoroutine()
    {
        if (_followHeadCoroutine != null)
        {
            StopCoroutine(_followHeadCoroutine);
            _followHeadCoroutine = null;
        }
    }

    /// <summary>
    ///     跟随头部的协程
    /// </summary>
    private IEnumerator FollowHeadCoroutine()
    {
        if (_vrHeadTransform == null || _vrSubtitleContainer == null)
        {
            LogManager.Error(
                "VR头部变换或字幕容器为空，无法跟随头部/VR head transform or subtitle container is null, cannot follow head");
            yield break;
        }

        while (true)
        {
            if (_vrHeadTransform != null && _vrSubtitleContainer != null)
            {
                // 设置位置为头部前方
                var headPosition = _vrHeadTransform.position;
                var headForward = _vrHeadTransform.forward;
                var headUp = _vrHeadTransform.up;

                // 计算字幕位置（在头部前方固定距离）
                var distance = _config.VRDistance;
                var position = headPosition + headForward * distance;

                // 更新位置和旋转
                _vrSubtitleContainer.position = position;
                _vrSubtitleContainer.rotation = Quaternion.LookRotation(headForward, headUp);
            }

            yield return null;
        }
    }
}