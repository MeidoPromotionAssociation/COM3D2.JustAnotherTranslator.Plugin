using System;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     VR空间字幕组件，在VR游戏空间中显示字幕，跟随玩家视角
/// </summary>
public class VRSpaceSubtitleComponent : BaseSubtitleComponent
{
    private const int MaxInitRetries = 5;

    // 头部跟踪阈值，避免微小移动引起的抖动
    private const float PositionThreshold = 0.001f;

    private const float RotationThreshold = 0.1f;

    // 字幕跟随平滑度
    // TODO 可配置
    protected readonly float FollowSmoothness = 5.0f;

    // 初始化失败计数器
    private int _initFailureCount;

    // 缓存变量，避免重复计算
    private Vector3 _lastHeadPosition = Vector3.zero;
    private Quaternion _lastHeadRotation = Quaternion.identity;
    private Vector3 _targetPosition = Vector3.zero;
    private Quaternion _targetRotation = Quaternion.identity;

    // VR头部位置参考，用于跟随头部运动
    protected Transform VRHeadTransform;

    // 世界空间VR字幕
    protected RectTransform VrSpaceCanvasRect;

    // 世界空间VR字幕的容器
    protected GameObject VrSubtitleContainer;


    /// <summary>
    ///     跟随头部更新
    /// </summary>
    private void Update()
    {
        if (!gameObject.activeSelf) return;

        // 尝试初始化VR组件（带重试机制）
        if (VRHeadTransform is null)
        {
            if (_initFailureCount < MaxInitRetries)
            {
                FindVRHeadComponents();
                _initFailureCount++;
            }
            else
            {
                LogManager.Warning(
                    "VRHeadTransform failed to initialize after maximum retries, component disabled/VRHeadTransform 达到最大重试次数仍初始化失败，组件已被禁用。");
                enabled = false;
                return;
            }
        }

        if (VRHeadTransform is null || VrSubtitleContainer is null) return;

        UpdateSubtitlePosition();
    }

    /// <summary>
    ///     更新字幕位置和旋转
    /// </summary>
    private void UpdateSubtitlePosition()
    {
        var headTransform = VRHeadTransform;
        var containerTransform = VrSubtitleContainer.transform;

        var currentHeadPosition = headTransform.position;
        var currentHeadRotation = headTransform.rotation;

        // 检查头部是否有显著移动，避免不必要的计算
        var positionChanged = Vector3.Distance(currentHeadPosition, _lastHeadPosition) >
                              PositionThreshold;
        var rotationChanged = Quaternion.Angle(currentHeadRotation, _lastHeadRotation) >
                              RotationThreshold;

        if (positionChanged || rotationChanged)
        {
            // 缓存头部变换信息
            var headForward = headTransform.forward;
            var headUp = headTransform.up;
            var headRight = headTransform.right;

            // 修正后的位置计算
            // 1. 首先计算基础前方位置
            var basePosition = currentHeadPosition + headForward * Config.VRSubtitleDistance;

            // 2. 应用垂直偏移（负值向下，正值向上）- 固定偏移量，不受距离影响
            var verticalOffset = headUp * (Config.VRSubtitleVerticalOffset / 100.0f);

            // 3. 应用水平偏移（负值向左，正值向右）- 固定偏移量，不受距离影响  
            var horizontalOffset = headRight * (Config.VRSubtitleHorizontalOffset / 100.0f);

            _targetPosition = basePosition + verticalOffset + horizontalOffset;

            // 计算目标旋转（字幕面向玩家头部）
            // 将朝向向量反转，因为UI的正面在-Z轴，所以需要让变换的+Z轴远离玩家
            var directionToLook = (_targetPosition - currentHeadPosition).normalized;

            // 使用头部的up向量作为参考，确保字幕不会倾斜
            _targetRotation = Quaternion.LookRotation(directionToLook, headUp);

            // 更新缓存
            _lastHeadPosition = currentHeadPosition;
            _lastHeadRotation = currentHeadRotation;
        }

        // 应用平滑移动
        ApplySmoothMovement(containerTransform);
    }

    /// <summary>
    ///     应用平滑移动
    /// </summary>
    private void ApplySmoothMovement(Transform containerTransform)
    {
        // 为平滑处理限制 deltaTime 的最大值，防止跳跃
        var cappedDeltaTime = Mathf.Min(Time.deltaTime, 0.1f);

        // 使用帧率无关的平滑因子
        var smoothFactor = FollowSmoothness > 0.0001f
            ? 1.0f - Mathf.Exp(-FollowSmoothness * cappedDeltaTime)
            : 0f;

        // 平滑更新位置
        containerTransform.position = Vector3.Lerp(
            containerTransform.position,
            _targetPosition,
            smoothFactor
        );

        // 平滑更新旋转
        containerTransform.rotation = Quaternion.Lerp(
            containerTransform.rotation,
            _targetRotation,
            smoothFactor
        );
    }

    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    /// <param name="subtitleId">字幕ID</param>
    public override void Init(SubtitleConfig config, string subtitleId)
    {
        if (config is null)
        {
            LogManager.Warning(
                "VR space Subtitle config is null, subtitle component will not be initialized/VR 空间字幕配置为空，字幕组件将不会被初始化");
            return;
        }

        try
        {
            Config = config;

            // 查找 VR 头部
            FindVRHeadComponents();

            // 创建UI
            CreateSubtitleUI();

            // 应用配置
            ApplyConfig();

            if (gameObject == null)
                throw new NullReferenceException(
                    "GameObject is null after creating UI, cannot initialize VR space subtitle component/创建UI后GameObject为空，无法初始化VR空间字幕组件");
            gameObject.name = subtitleId;
            gameObject.SetActive(false);
            LogManager.Debug($"VR space subtitle component {subtitleId} initialized");
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Failed to initialize VR space subtitle component/VR空间字幕组件初始化失败: {e.Message}");
        }
    }

    /// <summary>
    ///     通过GameMain获取头部变换
    /// </summary>
    private bool TryGetHeadTransformFromGameMain()
    {
        try
        {
            if (GameMain.Instance?.OvrMgr?.EyeAnchor != null)
            {
                VRHeadTransform = GameMain.Instance.OvrMgr.EyeAnchor;
                return true;
            }
        }
        catch (Exception e)
        {
            LogManager.Debug($"Failed to get head transform from GameMain: {e.Message}");
        }

        return false;
    }

    /// <summary>
    ///     通过查找OvrMgr获取头部变换
    /// </summary>
    private bool TryGetHeadTransformFromOvrMgr()
    {
        try
        {
            var ovrMgr = FindObjectOfType<OvrMgr>();
            if (ovrMgr?.EyeAnchor != null)
            {
                VRHeadTransform = ovrMgr.EyeAnchor;
                return true;
            }
        }
        catch (Exception e)
        {
            LogManager.Debug($"Failed to get head transform from OvrMgr: {e.Message}");
        }

        return false;
    }

    /// <summary>
    ///     使用主摄像机作为头部变换的备选方案
    /// </summary>
    private bool TryGetHeadTransformFromCamera()
    {
        try
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                VRHeadTransform = mainCamera.transform;
                LogManager.Warning(
                    "Using main camera as VR head transform - this may not provide accurate VR tracking");
                return true;
            }
        }
        catch (Exception e)
        {
            LogManager.Debug($"Failed to get head transform from camera: {e.Message}");
        }

        return false;
    }


    /// <summary>
    ///     初始化VR头部组件
    /// </summary>
    protected void FindVRHeadComponents()
    {
        if (VRHeadTransform != null)
            return;

        // 方法1: 通过 GameMain.Instance.OvrMgr 获取
        if (TryGetHeadTransformFromGameMain())
        {
            LogManager.Debug(
                "VR head transform (EyeAnchor) found via GameMain, subtitle head tracking enabled");
            return;
        }

        // 方法2: 直接查找 OvrMgr
        if (TryGetHeadTransformFromOvrMgr())
        {
            LogManager.Debug(
                "VR head transform found via FindObjectOfType<OvrMgr>(), subtitle head tracking enabled");
            return;
        }

        // 方法3: 查找摄像机作为备选方案
        if (TryGetHeadTransformFromCamera())
        {
            LogManager.Debug("Using main camera as VR head transform fallback");
            return;
        }

        LogManager.Warning(
            "找不到VR头部变换，头部字幕跟踪将无法工作/VR head transform not found, head tracking will not work");
    }

    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected override void CreateSubtitleUI()
    {
        // 在 WorldSpace 模式下，字幕位置基本和 Canvas 大小无关
        // 因为可以超出 Canvas 显示范围
        // 尺寸此时约为 1000 单位 = 1 米

        // 创建一个容器，用来移动位置，直接移动 Canvas 效果不佳
        var containerObj = new GameObject("JAT_Subtitle_VRSpaceSubtitleComponent_Container");
        containerObj.transform.SetParent(transform, false);
        VrSubtitleContainer = containerObj;
        containerObj.transform.localPosition = new Vector3(0, 0, 0);
        containerObj.transform.localRotation = Quaternion.identity;
        containerObj.transform.localScale = new Vector3(1, 1, 1);

        // 创建世界空间Canvas
        var canvasObj = new GameObject("JAT_Subtitle_VRSpaceSubtitleComponent_Canvas");
        canvasObj.transform.SetParent(containerObj.transform, false);
        CanvasComponents = canvasObj.AddComponent<Canvas>();
        CanvasComponents.renderMode = RenderMode.WorldSpace;
        CanvasComponents.sortingOrder = 32767;
        CanvasComponents.overrideSorting = true;

        // 设置Canvas尺寸
        VrSpaceCanvasRect = CanvasComponents.GetComponent<RectTransform>();
        VrSpaceCanvasRect.anchoredPosition3D = new Vector3(0, 0, 0);
        VrSpaceCanvasRect.sizeDelta = new Vector2(1920, 1080); // 画布尺寸，仍然设置为 1920 x 1080
        VrSpaceCanvasRect.anchorMin = new Vector2(0.5f, 0.5f);
        VrSpaceCanvasRect.anchorMax = new Vector2(0.5f, 0.5f); // 锚点，中心
        VrSpaceCanvasRect.pivot = new Vector2(0.5f, 0.5f); // 轴心，中心
        VrSpaceCanvasRect.localRotation = Quaternion.identity; // 旋转0度
        VrSpaceCanvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f); // 此时 1000 尺寸单位 = 1米

        // 添加画布缩放器
        // 在 WorldSpace 模式下，CanvasScaler 无法进行缩放，只决定像素密度
        CanvasScalerComponents = CanvasComponents.gameObject.AddComponent<CanvasScaler>();
        CanvasScalerComponents.scaleFactor = 1f;
        // dynamicPixelsPerUnit 决定了1个世界单位等于多少像素，这会影响字体清晰度
        CanvasScalerComponents.dynamicPixelsPerUnit = 1000f;

        // 添加画布组
        CanvasGroupComponents = CanvasComponents.gameObject.AddComponent<CanvasGroup>();
        CanvasGroupComponents.alpha = 1f;
        CanvasGroupComponents.blocksRaycasts = false; // 不阻挡射线（不阻止点击）
        CanvasGroupComponents.interactable = false; // 不可交互

        // 创建背景面板
        var backgroundObj = new GameObject("JAT_Subtitle_VRSpaceSubtitleComponent_Background");
        backgroundObj.transform.SetParent(CanvasComponents.transform, false);
        BackgroundImageComponents = backgroundObj.AddComponent<Image>();
        BackgroundImageComponents.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景
        BackgroundImageComponents.raycastTarget = false; // 不拦截射线

        // 设置背景位置和大小，让它完全填充 Canvas
        var backgroundRect = BackgroundImageComponents.rectTransform;
        backgroundRect.anchoredPosition3D = new Vector3(0, 0, 0); // 画面中心
        var size = new Vector2(1920, 30);
        backgroundRect.sizeDelta = size;
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f); // 锚点为Canvas中心
        backgroundRect.pivot = new Vector2(0.5f, 0.5f); // 轴心0.5，中心
        backgroundRect.localRotation = Quaternion.identity; // 旋转0度
        backgroundRect.localScale = new Vector3(1, 1, 1); // 缩放1倍

        // 创建文本对象
        var textObj = new GameObject("JAT_Subtitle_VRSpaceSubtitleComponent_Text");
        textObj.transform.SetParent(backgroundObj.transform, false);
        TextComponent = textObj.AddComponent<Text>();
        TextComponent.raycastTarget = false; // 不拦截射线
        TextComponent.supportRichText = true; // 支持富文本

        // 设置文本位置和大小，与背景完全一致
        var textRect = TextComponent.rectTransform;
        textRect.anchoredPosition3D = new Vector3(0, 0, 0); // 注意可以跟随父级移动，因此只需要移动背景即可
        textRect.sizeDelta = size;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.localRotation = Quaternion.identity;
        textRect.localScale = new Vector3(40, 40, 1); // 40 倍缩放比较正常

        // 设置默认文本样式
        TextComponent.alignment = TextAnchor.MiddleCenter; // 居中对齐
        TextComponent.horizontalOverflow = HorizontalWrapMode.Overflow; // 垂直溢出时自动换行
        TextComponent.verticalOverflow = VerticalWrapMode.Truncate; // 水平溢出时截断

        // 添加描边组件
        OutlineComponents = TextComponent.gameObject.AddComponent<Outline>();
        OutlineComponents.effectColor = new Color(0, 0, 0, 0.5f); // 黑色描边颜色
        OutlineComponents.effectDistance = new Vector2(1, 1); // 描边宽度

        LogManager.Debug("VRSpaceSubtitleComponent Subtitle UI created");
    }

    /// <summary>
    ///     应用配置
    /// </summary>
    public override void ApplyConfig()
    {
        if (Config == null)
        {
            LogManager.Warning("VR Subtitle config is null, cannot apply config/VR 字幕配置为空，无法应用配置");
            return;
        }

        base.ApplyConfig();

        // 避免修改垂直位置
        // TODO 位置?
        var verticalPosition = Config.VerticalPosition;
        Config.CurrentVerticalPosition = verticalPosition;

        var size = new Vector2(
            Config.VRSapceSubtitleWidth, Config.VRSpaceSubtitleHeight);

        // 应用背景配置
        if (BackgroundImageComponents is not null)
        {
            BackgroundImageComponents.color = Config.BackgroundColor;

            BackgroundImageComponents.rectTransform.sizeDelta = size;
        }

        // 应用文本组件配置
        if (TextComponent is not null)
        {
            if (Config.Font is not null)
                TextComponent.font = Config.Font;

            TextComponent.fontSize = Config.FontSize;
            TextComponent.color = Config.TextColor;
            TextComponent.alignment = Config.TextAlignment;

            TextComponent.rectTransform.sizeDelta = size;
        }

        // 应用描边配置
        if (OutlineComponents is not null)
        {
            OutlineComponents.enabled = Config.EnableOutline;

            if (Config.EnableOutline)
            {
                OutlineComponents.effectColor = Config.OutlineColor;

                if (TextComponent is not null)
                {
                    // 根据Text组件的localScale来缩放描边距离，防止出现重影
                    var scaleFactor = TextComponent.transform.localScale.x;
                    if (Mathf.Abs(scaleFactor) > 0.001f)
                        OutlineComponents.effectDistance = new Vector2(
                            Config.OutlineWidth / scaleFactor,
                            Config.OutlineWidth / scaleFactor);
                    else
                        OutlineComponents.effectDistance =
                            new Vector2(Config.OutlineWidth, Config.OutlineWidth);
                }
            }
        }


        LogManager.Debug("Applied VR-specific subtitle config with proper UI scaling");
    }

    /// <summary>
    ///     销毁字幕
    /// </summary>
    protected override void DestroySubtitleUI()
    {
        if (VrSubtitleContainer != null)
        {
            Destroy(VrSubtitleContainer);
        }

        base.DestroySubtitleUI();
    }
}