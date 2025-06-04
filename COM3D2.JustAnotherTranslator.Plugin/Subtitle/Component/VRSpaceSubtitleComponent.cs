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
        // base.Init(config) 包含 Config = config ?? ...
        // 但这里直接使用传入的config进行早期检查和日志记录，
        // 然后再调用base.Init()，它会再次赋值Config。
        // 或者，我们可以先调用 base.Init(config) 然后使用 this.Config。
        // 为了与现有逻辑保持一致，我们先检查传入的 config。
        if (config == null) // Check incoming config directly
        {
            LogManager.Warning(
                "VR space Subtitle config is null during Init, subtitle component will not be fully initialized.");
            // Even if config is null, we might want to call base.Init with it
            // to let the base class handle the null appropriately (e.g., throw or use a default).
            // However, the original code had 'Config is null' which refers to the class member,
            // which wouldn't be set yet if base.Init isn't called first.
            // Let's assume the intention was to check the parameter.
            base.Init(config); // Call base.Init regardless, it handles null config.
            return;
        }

        base.Init(config); // This will set this.Config

        // 初始化 VR 组件
        InitVRComponents();

        // 创建UI - CreateSubtitleUI might use this.Config, so it's fine after base.Init()
        // CreateSubtitleUI(); // CreateSubtitleUI is called by base.Init() if we follow ScreenSubtitleComponent structure
                             // However, BaseSubtitleComponent.Init calls CreateSubtitleUI and ApplyConfig.
                             // Let's ensure this.Config is set before CreateSubtitleUI is implicitly or explicitly called.
                             // BaseSubtitleComponent.Init calls:
                             // this.Config = config;
                             // CreateSubtitleUI();
                             // ApplyConfig();
                             // So, CreateSubtitleUI() and ApplyConfig() here would be redundant if base.Init does it.
                             // Let's remove them here as base.Init already handles it.

        // 启动跟随头部的协程
        StartFollowHeadCoroutine();

        gameObject.SetActive(false); // This is also in base.Init
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
        // Config might be null here if base.Init hasn't been called or didn't set it yet.
        // However, BaseSubtitleComponent.Init sets Config THEN calls CreateSubtitleUI. So Config should be valid.
        if (Config != null)
        {
            VrSpaceCanvasRect.sizeDelta = new Vector2(Config.VRSubtitleWidth * 1000, Config.VRSubtitleHeight * 1000); // MODIFIED
        }
        else
        {
            // Fallback or error if Config is unexpectedly null
            VrSpaceCanvasRect.sizeDelta = new Vector2(0.2f * 1000, 0.1f * 1000); // Default fallback size
            LogManager.Warning("Config was null during VRSpaceSubtitleComponent.CreateSubtitleUI. Using default size.");
        }
        VrSubtitleContainer.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);


        // 添加画布缩放器
        CanvasScaler = Canvas.gameObject.AddComponent<CanvasScaler>();
        // For WorldSpace Canvas, CanvasScaler settings like referenceResolution might not be as critical
        // as for ScreenSpace, but they can influence how things are measured if certain UI elements rely on them.
        // Keeping them for consistency or if any child elements behave differently based on scaler settings.
        CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; // More typical for WorldSpace
        CanvasScaler.scaleFactor = 1.0f;
        // CanvasScaler.referenceResolution = new Vector2(1920, 1080); // Less relevant for WorldSpace pixel size
        // CanvasScaler.matchWidthOrHeight = 0.5f;

        // 添加射线检测
        var raycaster = Canvas.gameObject.AddComponent<GraphicRaycaster>();
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None; // 不阻挡任何对象
        raycaster.ignoreReversedGraphics = false; // 不忽略反向图形

        // 创建背景面板
        var backgroundObj = new GameObject("JAT_Subtitle_SubtitleBackground");
        backgroundObj.transform.SetParent(Canvas.transform, false);
        BackgroundImage = backgroundObj.AddComponent<Image>();
        // BackgroundImage.color will be set by base.ApplyConfig()

        // 设置背景位置和大小 (stretch to fill canvas)
        var backgroundRect = BackgroundImage.rectTransform;
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 1); // Stretch to fill parent (Canvas)
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.sizeDelta = Vector2.zero; // No size delta means it fully uses anchor bounds
        backgroundRect.anchoredPosition = Vector2.zero;

        // 创建文本对象
        var textObj = new GameObject("JAT_Subtitle_SubtitleText");
        textObj.transform.SetParent(backgroundRect, false); // Text is child of Background
        TextComponent = textObj.AddComponent<Text>();

        // 设置文本位置和大小 (stretch within background, with padding)
        var textRect = TextComponent.rectTransform;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        // Padding (e.g., 10 units on each side, these units are relative to the Canvas's sizeDelta)
        float padding = 10f * (Config?.FontSize / 24f ?? 1f) ; // Scale padding with font size a bit
        textRect.offsetMin = new Vector2(padding, padding); // Left, Bottom
        textRect.offsetMax = new Vector2(-padding, -padding); // Right, Top (negative for offset from top-right)
        // TextComponent properties (alignment, color, font, etc.) will be set by base.ApplyConfig()

        // 设置文本和背景不拦截点击事件
        TextComponent.raycastTarget = false;
        BackgroundImage.raycastTarget = false;

        // 添加描边组件
        Outline = TextComponent.gameObject.AddComponent<Outline>();
        // Outline properties will be set by base.ApplyConfig()
        Outline.enabled = false;

        LogManager.Debug("VRSpaceSubtitleComponent Subtitle UI created");
    }

    /// <summary>
    ///     应用配置到UI
    /// </summary>
    protected override void ApplyConfig()
    {
        // 先调用基类的ApplyConfig方法应用通用配置
        base.ApplyConfig();

        // 检查配置是否为空
        if (Config == null)
        {
            LogManager.Warning("Config is null in VRSpaceSubtitleComponent.ApplyConfig. Cannot apply VR-specific settings.");
            return;
        }

        if (VrSpaceCanvasRect != null)
        {
            // 更新VR字幕画布尺寸
            VrSpaceCanvasRect.sizeDelta = new Vector2(Config.VRSubtitleWidth * 1000, Config.VRSubtitleHeight * 1000); // MODIFIED
        }

        // Update background rect to stretch if it wasn't set like that initially
        // (This is now handled in CreateSubtitleUI to always stretch)
        // if (BackgroundImage != null)
        // {
        //     var backgroundRect = BackgroundImage.rectTransform;
        //     backgroundRect.anchorMin = new Vector2(0, 0);
        //     backgroundRect.anchorMax = new Vector2(1, 1);
        //     backgroundRect.sizeDelta = Vector2.zero;
        // }


        // 如果正在跟踪头部且配置更改，重启跟随头部协程以应用新配置
        // (e.g. distance, offsets might have changed)
        if (gameObject.activeInHierarchy && _followHeadCoroutine != null) // Only restart if active
        {
            StopFollowHeadCoroutine();
            StartFollowHeadCoroutine();
        }
        else if (gameObject.activeInHierarchy && _followHeadCoroutine == null && VRHeadTransform != null)
        {
            // If it became active and coroutine wasn't running (e.g. after being disabled)
            StartFollowHeadCoroutine();
        }

        LogManager.Debug($"Applied VR-specific subtitle config to {gameObject.name}");
    }

    /// <summary>
    ///     销毁字幕组件
    /// </summary>
    public override void Destroy()
    {
        StopFollowHeadCoroutine();
        base.Destroy();
    }

    public override void ShowSubtitle(string text, string speakerName, float duration)
    {
        base.ShowSubtitle(text, speakerName, duration);
        if (gameObject.activeSelf && _followHeadCoroutine == null && VRHeadTransform != null)
        {
            // Ensure follow coroutine starts if it wasn't running and component is now active
            StartFollowHeadCoroutine();
        }
    }

    public override void HideSubtitle(bool skipAnimation = false)
    {
        // Stop head following when subtitle is hidden to save resources
        // The coroutine itself checks for gameObject.activeSelf, but explicit stop is cleaner.
        // StopFollowHeadCoroutine(); // Keep it running but invisible, or stop?
                                  // If it stops, ShowSubtitle needs to restart it.
                                  // The coroutine has `if (!gameObject.activeSelf) yield break;`
                                  // but that only stops a *new* run. An existing run continues.
                                  // Let's ensure it stops if the gameobject becomes inactive.
        base.HideSubtitle(skipAnimation);
        // No, let the coroutine manage itself based on gameObject.activeSelf or explicit stop in Destroy.
        // If we stop it here, it needs to be restarted in ShowSubtitle.
        // The current FollowHeadCoroutine loop continues until object is destroyed.
        // It internally checks gameObject.activeSelf at the start of the enumerator,
        // but not inside the while(true) loop. This might be an issue.
        // Let's refine FollowHeadCoroutine to yield break if !gameObject.activeSelf.
    }


    /// <summary>
    ///     启动跟随头部的协程
    /// </summary>
    private void StartFollowHeadCoroutine()
    {
        if (!gameObject.activeInHierarchy) return; // Don't start if not active

        StopFollowHeadCoroutine(); // Stop any existing one
        if (VRHeadTransform != null) // Only start if we have a head to track
        {
            _followHeadCoroutine = StartCoroutine(FollowHeadCoroutine());
        }
        else
        {
            LogManager.Debug("VRHeadTransform is null, cannot start FollowHeadCoroutine.");
        }
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
        // Initial check if VR mode is appropriate
        if (!JustAnotherTranslator.IsVrMode ||
            Config.VRSubtitleMode != JustAnotherTranslator.VRSubtitleModeEnum.InSpace)
        {
            LogManager.Debug("FollowHeadCoroutine: Exiting because not in VR space mode.");
            yield break;
        }

        var findCount = 0;
        while (VRHeadTransform == null) // Use "is null" for Unity objects
        {
            if (!gameObject.activeInHierarchy) // Stop trying if object becomes inactive
            {
                LogManager.Debug("FollowHeadCoroutine: GameObject became inactive while waiting for VRHeadTransform.");
                yield break;
            }
            if (findCount > 10) // Try for about 10 seconds
            {
                LogManager.Warning("VR head transform not found after 10 attempts, subtitle head tracking disabled for this component.");
                yield break;
            }

            LogManager.Debug("FollowHeadCoroutine: Waiting for VR head transform...");
            InitVRComponents(); // Try to initialize it again
            findCount++;
            yield return new WaitForSeconds(1); // Wait a second before retrying
        }

        LogManager.Debug("FollowHeadCoroutine: VRHeadTransform found. Starting tracking loop.");

        while (true)
        {
            if (!gameObject.activeInHierarchy || VRHeadTransform == null) // Continuously check if still valid to run
            {
                LogManager.Debug("FollowHeadCoroutine: GameObject became inactive or VRHeadTransform lost. Stopping tracking loop.");
                yield break; // Exit coroutine if object is deactivated or head transform is lost
            }

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
            if (VrSubtitleContainer != null)
            {
                VrSubtitleContainer.transform.position = Vector3.Lerp(
                    VrSubtitleContainer.transform.position,
                    targetPosition,
                    Time.deltaTime * FollowSmoothness
                );

                // 字幕始终面向用户
                VrSubtitleContainer.transform.rotation = Quaternion.Lerp(
                    VrSubtitleContainer.transform.rotation,
                    Quaternion.LookRotation(VrSubtitleContainer.transform.position - VRHeadTransform.position, headUp), // Ensure up vector is correct
                    Time.deltaTime * FollowSmoothness
                );
            }
            else
            {
                LogManager.Warning("FollowHeadCoroutine: VrSubtitleContainer is null. Stopping tracking loop.");
                yield break;
            }
            yield return null;
        }
    }
}