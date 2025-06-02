using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     VR空间字幕组件，在VR游戏空间中显示字幕，跟随玩家视角
/// </summary>
public class VRSpaceSubtitleComponent : BaseSubtitleComponent
{
    // 字幕跟随平滑度
    protected readonly float _followSmoothness = 5.0f;
    private Coroutine _followHeadCoroutine;

    // VR头部位置参考，用于跟随头部运动
    protected Transform _vrHeadTransform;

    // 世界空间VR字幕
    protected RectTransform _vrSpaceCanvasRect;

    // VR悬浮字幕容器
    protected GameObject _vrSubtitleContainer;

    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    public override void Initialize(SubtitleConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // 创建UI
        CreateSubtitleUI();

        // 应用配置
        ApplyConfig();

        // 启动跟随头部的协程
        StartFollowHeadCoroutine();

        gameObject.SetActive(false);
        LogManager.Debug("VR space subtitle component initialized");
    }

    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected override void CreateSubtitleUI()
    {
        // 创建一个容器来承载悬浮字幕
        _vrSubtitleContainer = new GameObject("Subtitle_JAT_SubtitleContainer_VR_Space");
        _vrSubtitleContainer.transform.SetParent(transform, false);

        // 创建世界空间Canvas
        var canvasObj = new GameObject("Subtitle_JAT_SubtitleCanvas_VR_Space");
        canvasObj.transform.SetParent(_vrSubtitleContainer.transform, false);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 32767; // 确保在最上层显示
        _canvas.overrideSorting = true; // 确保覆盖所有其他排序

        // 设置Canvas尺寸，使其在世界空间中有合适的大小
        _vrSpaceCanvasRect = _canvas.GetComponent<RectTransform>();
        _vrSpaceCanvasRect.sizeDelta = new Vector2(_config.VRSubtitleWidth * 1000, 300);
        _vrSubtitleContainer.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // 添加画布缩放器
        _canvasScaler = _canvas.gameObject.AddComponent<CanvasScaler>();
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = new Vector2(1920, 1080);
        _canvasScaler.matchWidthOrHeight = 0.5f;

        // 添加射线检测
        var raycaster = _canvas.gameObject.AddComponent<GraphicRaycaster>();
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None; // 不阻挡任何对象
        raycaster.ignoreReversedGraphics = false; // 不忽略反向图形

        // 创建背景面板
        var backgroundObj = new GameObject("Subtitle_JAT_SubtitleBackground");
        backgroundObj.transform.SetParent(_canvas.transform, false);
        _backgroundImage = backgroundObj.AddComponent<Image>();
        _backgroundImage.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景

        // 设置背景位置和大小
        var backgroundRect = _backgroundImage.rectTransform;
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 0);
        backgroundRect.pivot = new Vector2(0.5f, 0);
        backgroundRect.sizeDelta = new Vector2(0, 100);
        backgroundRect.anchoredPosition = new Vector2(0, 0);

        // 创建文本对象
        var textObj = new GameObject("Subtitle_JAT_SubtitleText");
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

        LogManager.Debug("VR subtitle container created");
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
        if (!JustAnotherTranslator.IsVrMode ||
            _config.VRSubtitleMode != JustAnotherTranslator.VRSubtitleModeEnum.InSpace)
            yield break;

        if (_vrHeadTransform is null || !_initialized || !gameObject.activeSelf)
            yield break;

        while (true)
        {
            // 计算目标位置（基于头部位置和配置的偏移）
            var headForward = _vrHeadTransform.forward;
            var headUp = _vrHeadTransform.up;
            var headRight = _vrHeadTransform.right;

            // 应用偏移角度（水平和垂直）
            var verticalRotation = Quaternion.AngleAxis(_config.VRSubtitleVerticalOffset, headRight);
            var horizontalRotation = Quaternion.AngleAxis(_config.VRSubtitleHorizontalOffset, headUp);
            var offsetDirection = horizontalRotation * verticalRotation * headForward;

            // 计算最终位置（头部位置 + 偏移方向 * 距离）
            var targetPosition = _vrHeadTransform.position + offsetDirection * _config.VRSubtitleDistance;

            // 平滑跟随
            _vrSubtitleContainer.transform.position = Vector3.Lerp(
                _vrSubtitleContainer.transform.position,
                targetPosition,
                Time.deltaTime * _followSmoothness
            );

            // 字幕始终面向用户
            _vrSubtitleContainer.transform.rotation = Quaternion.Lerp(
                _vrSubtitleContainer.transform.rotation,
                Quaternion.LookRotation(_vrSubtitleContainer.transform.position - _vrHeadTransform.position),
                Time.deltaTime * _followSmoothness
            );

            yield return null;
        }
    }
}