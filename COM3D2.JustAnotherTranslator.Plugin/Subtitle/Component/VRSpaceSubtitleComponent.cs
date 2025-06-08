using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     VR空间字幕组件，在VR游戏空间中显示字幕，跟随玩家视角
/// </summary>
public class VRSpaceSubtitleComponent : BaseSubtitleComponent
{
    protected const float VRScaleFactor = 1000f;

    // 字幕跟随平滑度
    protected readonly float FollowSmoothness = 5.0f;

    // VR头部位置参考，用于跟随头部运动
    protected Transform VRHeadTransform;

    // 世界空间VR字幕
    protected RectTransform VrSpaceCanvasRect;

    // VR悬浮字幕容器
    protected GameObject VrSubtitleContainer;


    /// <summary>
    ///     跟随头部更新
    /// </summary>
    private void Update()
    {
        if (!gameObject.activeSelf) return;

        if (VRHeadTransform is null)
        {
            InitVRComponents();
            if (VRHeadTransform is null)
            {
                LogManager.Warning(
                    "VRHeadTransform failed to initialize, component disabled /VRHeadTransform 多次初始化失败，组件已被禁用。");
                enabled = false; // 禁用组件以避免后续错误
                return;
            }
        }

        // 缓存 VRHeadTransform 的属性
        var headTransform = VRHeadTransform;
        var headPosition = headTransform.position;
        var headForward = headTransform.forward;
        var headUp = headTransform.up;
        var headRight = headTransform.right;

        // 缓存 VrSubtitleContainer 的 Transform 及其当前状态
        var containerTransform = VrSubtitleContainer.transform;
        var currentContainerPosition = containerTransform.position;

        // 计算目标位置（基于头部位置和配置的偏移）
        var verticalRotation = Quaternion.AngleAxis(Config.VRSubtitleVerticalOffset, headRight);
        var horizontalRotation = Quaternion.AngleAxis(Config.VRSubtitleHorizontalOffset, headUp);
        var offsetDirection = horizontalRotation * verticalRotation * headForward;

        var targetPosition = headPosition + offsetDirection * Config.VRSubtitleDistance;

        // 为平滑处理限制 deltaTime 的最大值，以防止因单帧时间过长导致的跳跃
        var cappedDeltaTime = Mathf.Min(Time.deltaTime, 0.1f); // 例如，最大允许0.1秒的dt

        // 使用帧率无关的平滑因子
        // 确保 FollowSmoothness 为正。如果 FollowSmoothness 为0，smoothFactor 将为0。
        var smoothFactor = FollowSmoothness > 0.0001f
            ? 1.0f - Mathf.Exp(-FollowSmoothness * cappedDeltaTime)
            : 0f;

        // 平滑更新位置
        var newContainerPosition = Vector3.Lerp(
            currentContainerPosition,
            targetPosition,
            smoothFactor
        );
        containerTransform.position = newContainerPosition;

        // 字幕始终面向玩家
        VrSubtitleContainer.transform.rotation = Quaternion.Lerp(
            VrSubtitleContainer.transform.rotation,
            Quaternion.LookRotation(currentContainerPosition - headPosition),
            Time.deltaTime * FollowSmoothness
        );
    }

    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    public override void Init(SubtitleConfig config)
    {
        Config = config;

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
        // ScreenSpaceOverlay（桌面模式）：
        //  单位：屏幕像素
        //    sizeDelta = (1920, 1080) 表示1920x1080像素
        //
        // WorldSpace（VR模式）：
        //  单位：Unity世界单位（米）
        //     sizeDelta = (1, 1) 表示1米x1米的真实物理尺寸
        //
        // 为了获得更清晰的效果，先创建大尺寸UI再缩放

        // 创建一个容器来承载悬浮字幕
        VrSubtitleContainer = new GameObject("JAT_Subtitle_SubtitleContainer_VR_Space");
        VrSubtitleContainer.transform.SetParent(transform, false);

        // 创建世界空间Canvas
        var canvasObj = new GameObject("JAT_Subtitle_SubtitleCanvas_VR_Space");
        canvasObj.transform.SetParent(VrSubtitleContainer.transform, false);
        Canvas = canvasObj.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.WorldSpace;
        Canvas.sortingOrder = 32767;
        Canvas.overrideSorting = true;

        // 设置Canvas尺寸
        VrSpaceCanvasRect = Canvas.GetComponent<RectTransform>();

        VrSpaceCanvasRect.sizeDelta = new Vector2(
            Config.VRSubtitleBackgroundWidth * VRScaleFactor,
            Config.VRSubtitleBackgroundHeight * VRScaleFactor
        );
        VrSubtitleContainer.transform.localScale =
            new Vector3(1f / VRScaleFactor, 1f / VRScaleFactor, 1f / VRScaleFactor);

        // 添加画布缩放器
        CanvasScaler = Canvas.gameObject.AddComponent<CanvasScaler>();
        CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        CanvasScaler.referenceResolution = new Vector2(1920, 1080);
        CanvasScaler.matchWidthOrHeight = 0.5f;

        // 添加射线检测
        var raycaster = Canvas.gameObject.AddComponent<GraphicRaycaster>();
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        raycaster.ignoreReversedGraphics = false;

        // 创建背景面板
        var backgroundObj = new GameObject("JAT_Subtitle_SubtitleBackground");
        backgroundObj.transform.SetParent(Canvas.transform, false);
        BackgroundImage = backgroundObj.AddComponent<Image>();
        BackgroundImage.color = new Color(0, 0, 0, 0.5f);

        // 设置背景位置和大小 - 修正版本
        var backgroundRect = BackgroundImage.rectTransform;
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 0);
        backgroundRect.pivot = new Vector2(0.5f, 0);

        // 背景高度需要根据缩放因子调整
        // 原本100像素，在VR中应该对应合适的高度
        var backgroundHeight = Config.VRSubtitleBackgroundHeight * VRScaleFactor;
        backgroundRect.sizeDelta = new Vector2(0, backgroundHeight);
        backgroundRect.anchoredPosition = new Vector2(0, 0);

        // 创建文本对象
        var textObj = new GameObject("JAT_Subtitle_SubtitleText");
        textObj.transform.SetParent(backgroundRect, false);
        TextComponent = textObj.AddComponent<Text>();

        // 设置文本位置和大小 - 修正版本
        var textRect = TextComponent.rectTransform;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 0.5f);

        // 文本边距需要根据缩放因子调整
        // 原本边距-40, -20，在VR中需要相应放大
        var horizontalMargin = -40f * (VRScaleFactor / 1000f); // 根据缩放调整
        var verticalMargin = -20f * (VRScaleFactor / 1000f);
        textRect.sizeDelta = new Vector2(horizontalMargin, verticalMargin);
        textRect.anchoredPosition = new Vector2(0, 0);

        // 设置默认文本样式
        TextComponent.alignment = TextAnchor.MiddleCenter;
        TextComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        TextComponent.verticalOverflow = VerticalWrapMode.Overflow;

        // 设置文本和背景不拦截点击事件
        TextComponent.raycastTarget = false;
        BackgroundImage.raycastTarget = false;

        // 添加描边组件
        Outline = TextComponent.gameObject.AddComponent<Outline>();
        Outline.enabled = false;

        ApplyOverallScale();

        LogManager.Debug("VRSpaceSubtitleComponent Subtitle UI created");
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
            VrSpaceCanvasRect.sizeDelta = new Vector2(Config.CurrentVRSubtitleBackgroundWidth,
                Config.CurrentVRSubtitleBackgroundHeight);
        }

        ApplyOverallScale();
    }


    /// <summary>
    ///     应用配置 - 使用统一的缩放系统
    /// </summary>
    public override void ApplyConfig()
    {
        base.ApplyConfig();

        if (Config == null)
        {
            LogManager.Warning("VR Subtitle config is null, cannot apply config/VR 字幕配置为空，无法应用配置");
            return;
        }

        // 更新当前值
        Config.CurrentVRSubtitleBackgroundWidth = Config.VRSubtitleBackgroundWidth;
        Config.CurrentVRSubtitleBackgroundHeight = Config.VRSubtitleBackgroundHeight;

        // 应用缩放
        if (VrSpaceCanvasRect is not null)
            // 更新Canvas尺寸
            VrSpaceCanvasRect.sizeDelta = new Vector2(
                Config.CurrentVRSubtitleBackgroundWidth * VRScaleFactor,
                Config.CurrentVRSubtitleBackgroundHeight * VRScaleFactor
            );

        if (BackgroundImage is not null)
        {
            // 更新背景高度
            var backgroundRect = BackgroundImage.rectTransform;
            var backgroundHeight = Config.CurrentVRSubtitleBackgroundHeight * VRScaleFactor;
            backgroundRect.sizeDelta = new Vector2(0, backgroundHeight);
        }

        if (TextComponent is not null)
        {
            // 更新文本边距（可以根据背景尺寸动态调整）
            var textRect = TextComponent.rectTransform;
            var horizontalMargin = -40f * (VRScaleFactor / 1000f);
            var verticalMargin = -20f * (VRScaleFactor / 1000f);
            textRect.sizeDelta = new Vector2(horizontalMargin, verticalMargin);

            // 根据VR环境调整字体大小
            // var scaledFontSize = Mathf.RoundToInt(Config.FontSize * (VRScaleFactor / 1000f));
            // TextComponent.fontSize = scaledFontSize;
        }

        ApplyOverallScale();

        // 应用其他样式
        if (TextComponent is not null)
        {
            TextComponent.font = Config.Font;
            TextComponent.color = Config.TextColor;
            TextComponent.alignment = Config.TextAlignment;
        }

        if (BackgroundImage is not null) BackgroundImage.color = Config.BackgroundColor;

        if (Outline is not null)
        {
            Outline.enabled = Config.EnableOutline;
            Outline.effectColor = Config.OutlineColor;
            // 描边粗细也需要缩放
            var scaledOutlineWidth = Config.OutlineWidth * (1000f / 1000f);
            Outline.effectDistance = new Vector2(scaledOutlineWidth, scaledOutlineWidth);
        }

        LogManager.Debug("Applied VR-specific subtitle config with proper UI scaling");
    }


    /// <summary>
    ///     应用整体缩放
    /// </summary>
    private void ApplyOverallScale()
    {
        if (VrSubtitleContainer is null || Config == null)
            return;

        // 应用缩放
        //VrSubtitleContainer.transform.localScale = VrSubtitleContainer.transform.localScale * Config.VRSubtitleScale;

        LogManager.Debug($"Applied overall VR subtitle scale: {Config.VRSubtitleScale}");
    }


    /// <summary>
    ///     隐藏字幕
    /// </summary>
    public override void HideSubtitle(bool skipAnimation = false)
    {
        VrSubtitleContainer.SetActive(false);
        base.HideSubtitle(skipAnimation);
    }

    /// <summary>
    ///     销毁字幕组件
    /// </summary>
    public override void Destroy()
    {
        base.Destroy();
    }
}