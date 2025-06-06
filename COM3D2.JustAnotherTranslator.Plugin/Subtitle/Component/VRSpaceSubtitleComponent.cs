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
    protected readonly float FollowSmoothness = 5.0f;

    // 跟随头部的协程
    private Coroutine _followHeadCoroutine;

    // VR头部位置参考，用于跟随头部运动
    protected Transform VRHeadTransform;

    // 世界空间VR字幕
    protected RectTransform VrSpaceCanvasRect;

    // VR悬浮字幕容器
    protected GameObject VrSubtitleContainer;

    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    public override void Init(SubtitleConfig config)
    {
        if (Config is null)
        {
            LogManager.Warning(
                "VR space Subtitle config is null, subtitle component will not be initialized/VR 空间字幕配置为空，字幕组件将不会被初始化");
            return;
        }

        // 初始化 VR 组件
        InitVRComponents();

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
    ///     初始化VR组件
    /// </summary>
    protected void InitVRComponents()
    {
        if (VRHeadTransform is not null)
            return;

        // 查找 OvrMgr 的 EyeAnchor
        if (GameMain.Instance is not null && GameMain.Instance.OvrMgr is not null)
        {
            VRHeadTransform = GameMain.Instance.OvrMgr.EyeAnchor;
            if (VRHeadTransform is not null)
            {
                LogManager.Debug(
                    "VR head transform (EyeAnchor) found, subtitle head tracking enabled");
                return;
            }
        }

        // 如果无法通过GameMain.Instance.OvrMgr获取，尝试直接查找OvrMgr
        var ovrMgr = FindObjectOfType<OvrMgr>();
        if (ovrMgr is not null && ovrMgr.EyeAnchor is not null)
        {
            VRHeadTransform = ovrMgr.EyeAnchor;
            LogManager.Debug(
                "VR head transform found through FindObjectOfType<OvrMgr>(), subtitle head tracking enabled");
        }
        else
        {
            LogManager.Warning(
                "找不到VR头部变换，头部字幕跟踪将无法工作/VR head transform not found, head tracking will not work");
        }
    }

    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected override void CreateSubtitleUI()
    {
        // 创建一个容器来承载悬浮字幕
        VrSubtitleContainer = new GameObject("JAT_Subtitle_SubtitleContainer_VR_Space");
        VrSubtitleContainer.transform.SetParent(transform, false);

        // 创建世界空间Canvas
        var canvasObj = new GameObject("JAT_Subtitle_SubtitleCanvas_VR_Space");
        canvasObj.transform.SetParent(VrSubtitleContainer.transform, false);
        Canvas = canvasObj.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.WorldSpace;
        Canvas.sortingOrder = 32767; // 确保在最上层显示
        Canvas.overrideSorting = true; // 确保覆盖所有其他排序

        // 设置Canvas尺寸，使其在世界空间中有合适的大小
        VrSpaceCanvasRect = Canvas.GetComponent<RectTransform>();
        VrSpaceCanvasRect.sizeDelta = new Vector2(Config.VRSubtitleBackgroundWidth * 1000, 300);
        VrSubtitleContainer.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // 添加画布缩放器
        CanvasScaler = Canvas.gameObject.AddComponent<CanvasScaler>();
        CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        CanvasScaler.referenceResolution = new Vector2(1920, 1080);
        CanvasScaler.matchWidthOrHeight = 0.5f;

        // 添加射线检测
        var raycaster = Canvas.gameObject.AddComponent<GraphicRaycaster>();
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None; // 不阻挡任何对象
        raycaster.ignoreReversedGraphics = false; // 不忽略反向图形

        // 创建背景面板
        var backgroundObj = new GameObject("JAT_Subtitle_SubtitleBackground");
        backgroundObj.transform.SetParent(Canvas.transform, false);
        BackgroundImage = backgroundObj.AddComponent<Image>();
        BackgroundImage.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景

        // 设置背景位置和大小
        var backgroundRect = BackgroundImage.rectTransform;
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 0);
        backgroundRect.pivot = new Vector2(0.5f, 0);
        backgroundRect.sizeDelta = new Vector2(0, 100);
        backgroundRect.anchoredPosition = new Vector2(0, 0);

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
        TextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        TextComponent.verticalOverflow = VerticalWrapMode.Truncate;

        // 设置文本和背景不拦截点击事件
        TextComponent.raycastTarget = false;
        BackgroundImage.raycastTarget = false;

        // 添加描边组件
        Outline = TextComponent.gameObject.AddComponent<Outline>();
        Outline.enabled = false;

        LogManager.Debug("VRSpaceSubtitleComponent Subtitle UI created");
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
            Config.VRSubtitleMode != JustAnotherTranslator.VRSubtitleModeEnum.InSpace ||
            !gameObject.activeSelf)
            yield break;

        var findCount = 0;
        while (VRHeadTransform is null)
        {
            if (findCount > 10)
            {
                LogManager.Debug("VR head transform not found after 10 seconds, subtitle head tracking disabled");
                yield break;
            }

            LogManager.Debug("Waiting for VR head transform...");
            InitVRComponents();
            findCount++;
            yield return new WaitForSeconds(1);
        }

        while (true)
        {
            // 计算目标位置（基于头部位置和配置的偏移）
            var headForward = VRHeadTransform.forward;
            var headUp = VRHeadTransform.up;
            var headRight = VRHeadTransform.right;

            // 应用偏移角度（水平和垂直）
            var verticalRotation = Quaternion.AngleAxis(Config.VRSubtitleVerticalOffset, headRight);
            var horizontalRotation = Quaternion.AngleAxis(Config.VRSubtitleHorizontalOffset, headUp);
            var offsetDirection = horizontalRotation * verticalRotation * headForward;

            // 计算最终位置（头部位置 + 偏移方向 * 距离）
            var targetPosition = VRHeadTransform.position + offsetDirection * Config.VRSubtitleDistance;

            // 平滑跟随
            VrSubtitleContainer.transform.position = Vector3.Lerp(
                VrSubtitleContainer.transform.position,
                targetPosition,
                Time.deltaTime * FollowSmoothness
            );

            // 字幕始终面向用户
            VrSubtitleContainer.transform.rotation = Quaternion.Lerp(
                VrSubtitleContainer.transform.rotation,
                Quaternion.LookRotation(VrSubtitleContainer.transform.position - VRHeadTransform.position),
                Time.deltaTime * FollowSmoothness
            );

            yield return null;
        }
    }

    /// <summary>
    ///     应用配置到UI
    /// </summary>
    public override void ApplyConfig()
    {
        // 先调用基类的ApplyConfig方法应用通用配置
        base.ApplyConfig();

        // 检查配置是否为空
        if (Config == null) return;

        var backgroundWidth = Config.VRSubtitleBackgroundWidth;
        Config.CurrentVRSubtitleBackgroundWidth = backgroundWidth;

        var backgroundHeight = Config.VRSubtitleBackgroundHeight;
        Config.CurrentVRSubtitleBackgroundHeight = backgroundHeight;

        if (VrSpaceCanvasRect is not null)
        {
            // 更新VR字幕画布尺寸
            VrSpaceCanvasRect.localScale = new Vector3(1, 1, 1);
            VrSpaceCanvasRect.sizeDelta = new Vector2(backgroundWidth, backgroundHeight);
        }

        // 如果正在跟踪头部且配置更改，重启跟随头部协程以应用新配置
        if (_followHeadCoroutine != null)
        {
            StopFollowHeadCoroutine();
            StartFollowHeadCoroutine();
        }

        LogManager.Debug($"Applied VR-specific subtitle config to {gameObject.name}");
    }


    /// <summary>
    ///     应用新的位置，包括背景大小
    /// </summary>
    public override void ApplyNewPosition()
    {
        base.ApplyNewPosition();


        if (VrSpaceCanvasRect is not null)
        {
            // 更新VR字幕画布尺寸
            VrSpaceCanvasRect.localScale = new Vector3(1, 1, 1);
            VrSpaceCanvasRect.sizeDelta = new Vector2(Config.CurrentVRSubtitleBackgroundWidth, Config.CurrentVRSubtitleBackgroundHeight);
        }

    }
}