using System.Collections;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     VR平板字幕组件，在 VR 模式中原有 UI 会被放到一个悬浮平板电脑上
/// </summary>
public class VRTabletSubtitleComponent : BaseSubtitleComponent
{
    /// 初始化协程
    private string _initCoroutineID;

    /// VR字幕的容器
    protected Transform VrSubtitleContainerTransform;

    /// 平板电脑物体
    protected Transform VRTabletTransform;


    /// <summary>
    ///     每帧更新
    /// </summary>
    protected void LateUpdate()
    {
        // 跟随平板电脑可见性
        var isVisible = GameMain.Instance?.OvrMgr?.OvrCamera?.m_bUiToggle;
        if (isVisible == null) return;
        gameObject.SetActive(isVisible.Value); // VrSubtitleContainer 并不是这个 gameObject 的子物体
        if (VrSubtitleContainerTransform is not null)
            VrSubtitleContainerTransform.gameObject.SetActive(isVisible.Value);
    }

    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected override void CreateSubtitleUI()
    {
        if (!string.IsNullOrEmpty(_initCoroutineID) &&
            CoroutineManager.IsRunning(_initCoroutineID)) return;
        _initCoroutineID = CoroutineManager.LaunchCoroutine(CreateUIWhenReady());
    }

    /// <summary>
    ///     找到平板电脑并创建字幕UI
    /// </summary>
    /// <returns></returns>
    private IEnumerator CreateUIWhenReady()
    {
        // 等待 OvrMgr / OvrTablet 初始化完成
        while (GameMain.Instance?.OvrMgr?.OvrCamera?.m_goOvrUiTablet == null)
            yield return null;

        var tablet = GameMain.Instance.OvrMgr.OvrCamera.m_goOvrUiTablet;
        VRTabletTransform = tablet.transform;
        if (VRTabletTransform == null)
        {
            LogManager.Warning(
                "Failed to find VR tablet object, subtitle will not be displayed, please report this issue/没有找到平板电脑物体，字幕将无法显示，请报告此问题");
            yield break;
        }

        // 创建一个容器，用来移动位置，直接移动 Canvas 效果不佳
        var containerObj = new GameObject("JAT_Subtitle_VRTabletSubtitleComponent_Container");
        VrSubtitleContainerTransform = containerObj.transform;
        VrSubtitleContainerTransform.SetParent(VRTabletTransform, false);
        VrSubtitleContainerTransform.localPosition = new Vector3(0, 0f, -1f); // -1f 约为平板中心
        VrSubtitleContainerTransform.localRotation = Quaternion.Euler(90, 0, 0); // 与平板旋转一致以贴合
        VrSubtitleContainerTransform.localScale = new Vector3(1, 1, 1);

        // 创建世界空间Canvas
        var canvasObj = new GameObject("JAT_Subtitle_VRTabletSubtitleComponent_Canvas");
        canvasObj.transform.SetParent(containerObj.transform, false);
        CanvasComponents = canvasObj.AddComponent<Canvas>();
        CanvasComponents.renderMode = RenderMode.WorldSpace;
        CanvasComponents.sortingOrder = 32767;
        CanvasComponents.overrideSorting = true;

        // 设置Canvas尺寸
        var vrSpaceCanvasRect = CanvasComponents.GetComponent<RectTransform>();
        vrSpaceCanvasRect.anchoredPosition3D = new Vector3(0, 0, 0);
        vrSpaceCanvasRect.sizeDelta = new Vector2(1, 1); // 画布尺寸，不重要
        vrSpaceCanvasRect.anchorMin = new Vector2(0.5f, 0.5f);
        vrSpaceCanvasRect.anchorMax = new Vector2(0.5f, 0.5f); // 锚点，中心
        vrSpaceCanvasRect.pivot = new Vector2(0.5f, 0.5f); // 轴心，中心
        vrSpaceCanvasRect.localPosition = new Vector3(0, 0, 0);
        vrSpaceCanvasRect.localRotation = Quaternion.identity; // 旋转0度
        vrSpaceCanvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f); // 此时 1000 尺寸单位 = 1米

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
        var backgroundObj = new GameObject("JAT_Subtitle_VRTabletSubtitleComponent_Background");
        backgroundObj.transform.SetParent(CanvasComponents.transform, false);
        BackgroundImageComponents = backgroundObj.AddComponent<Image>();
        BackgroundImageComponents.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景
        BackgroundImageComponents.raycastTarget = false; // 不拦截射线

        // 设置背景位置和大小
        var backgroundRect = BackgroundImageComponents.rectTransform;
        backgroundRect.anchoredPosition3D = new Vector3(0, 0, 0); // 画面中心
        var size = new Vector2(560, 10); // 560约为虚拟平板电脑宽度
        backgroundRect.sizeDelta = size;
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f); // 锚点为Canvas中心
        backgroundRect.pivot = new Vector2(0.5f, 0.5f); // 轴心0，中心
        backgroundRect.localPosition = new Vector3(0, 0, 0);
        backgroundRect.localRotation = Quaternion.identity; // 旋转0度
        backgroundRect.localScale = new Vector3(1, 1, 1); // 缩放1倍

        // 创建文本对象
        var textObj = new GameObject("JAT_Subtitle_VRTabletSubtitleComponent_Text");
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
        textRect.localPosition = new Vector3(0, 0, 0);
        textRect.localRotation = Quaternion.identity;
        textRect.localScale = new Vector3(12, 12, 1); // 12 倍缩放比较正常

        // 设置默认文本样式
        TextComponent.alignment = TextAnchor.MiddleCenter; // 居中对齐
        TextComponent.horizontalOverflow = HorizontalWrapMode.Overflow; // 水平溢出时允许超出边界
        TextComponent.verticalOverflow = VerticalWrapMode.Truncate; // 垂直溢出时截断

        // 添加描边组件
        OutlineComponents = TextComponent.gameObject.AddComponent<Outline>();
        OutlineComponents.effectColor = new Color(0, 0, 0, 0.5f); // 黑色描边颜色
        OutlineComponents.effectDistance = new Vector2(1, 1); // 描边宽度

        LogManager.Debug("VRTabletSubtitleComponent Subtitle UI created");
    }

    /// <summary>
    ///     设置当前垂直位置
    /// </summary>
    /// <param name="position"></param>
    public override void SetVerticalPosition(float position)
    {
        if (Config != null)
        {
            Config.CurrentVerticalPosition = position;

            // 应用位置
            if (VrSubtitleContainerTransform != null)
                VrSubtitleContainerTransform.transform.localPosition = new Vector3(
                    Config.VRTabletSubtitleHorizontalPosition, 0, position); // 没写错，就是 Z 是垂直

            return;
        }

        LogManager.Warning("Subtitle config is null, cannot set vertical position/字幕配置为空，无法设置垂直位置");
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

        // 避免修改垂直位置
        var verticalPosition = Config.VRTabletSubtitleVerticalPosition;
        Config.CurrentVerticalPosition = verticalPosition;

        var size = new Vector2(
            Config.VRTabletSubtitleWidth, Config.VRTabletSubtitleHeight);

        // 应用位置
        if (VrSubtitleContainerTransform != null)
            VrSubtitleContainerTransform.transform.localPosition = new Vector3(
                Config.VRTabletSubtitleHorizontalPosition, 0,
                Config.CurrentVerticalPosition); // 没写错，就是 Z 是垂直

        // 应用Canvas配置
        if (CanvasComponents != null)
            CanvasComponents.pixelPerfect = Config.VRTabletSubtitlePixelPerfect;

        // 应用背景配置
        if (BackgroundImageComponents != null)
        {
            BackgroundImageComponents.color = Config.BackgroundColor;

            BackgroundImageComponents.rectTransform.sizeDelta = size;
        }

        // 应用文本组件配置
        if (TextComponent != null)
        {
            if (Config.Font != null)
                TextComponent.font = Config.Font;

            TextComponent.fontSize = Config.FontSize;
            TextComponent.color = Config.TextColor;
            TextComponent.alignment = Config.TextAlignment;
            TextComponent.rectTransform.localScale = new Vector3(
                Config.VRTabletSubtitleTextSizeMultiplier,
                Config.VRTabletSubtitleTextSizeMultiplier, 1f);

            TextComponent.rectTransform.sizeDelta = size;
        }

        // 应用描边配置
        if (OutlineComponents != null)
        {
            OutlineComponents.enabled = Config.EnableOutline;

            if (Config.EnableOutline)
            {
                OutlineComponents.effectColor = Config.OutlineColor;

                if (TextComponent != null)
                {
                    // 由于Text组件有大倍数缩放，描边需要相应缩小以保持视觉一致性
                    var textScale = TextComponent.transform.localScale.x;

                    if (Mathf.Abs(textScale) > 0.001f)
                    {
                        // 当 Canvas 处于 WorldSpace 且 dynamicPixelsPerUnit = 1000、Canvas.scale = 0.001 时这 1 像素 = 0.001 米
                        // 当 1mm 乘上 Text 的 12 倍缩放后，这 1 像素就是 0.012 米，这变得很大，因此需要除以缩放因子
                        // 实测 12 倍缩放时 0.03 观感较为正常，1 / (12 * 3) ≈ 0.0278 ≈ 0.03
                        // 不同头显设备可能需要不同的缩放因子
                        var adjustedOutlineWidth = Config.OutlineWidth /
                                                   (textScale * Config
                                                       .VRTabletSubtitleTextSizeMultiplier);
                        OutlineComponents.effectDistance =
                            new Vector2(adjustedOutlineWidth, adjustedOutlineWidth);
                    }
                    else
                    {
                        OutlineComponents.effectDistance =
                            new Vector2(Config.OutlineWidth, Config.OutlineWidth);
                    }
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
        if (VrSubtitleContainerTransform != null)
        {
            Destroy(VrSubtitleContainerTransform.gameObject);
            VrSubtitleContainerTransform = null;
        }

        base.DestroySubtitleUI();
    }
}