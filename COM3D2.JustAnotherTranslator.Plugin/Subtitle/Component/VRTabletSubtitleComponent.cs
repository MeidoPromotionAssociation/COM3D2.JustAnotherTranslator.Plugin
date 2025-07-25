using COM3D2.JustAnotherTranslator.Plugin.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     VR平板字幕组件，在 VR 模式中原有 UI 会被放到一个悬浮平板电脑上
/// </summary>
public class VRTabletSubtitleComponent : BaseSubtitleComponent
{
    protected Camera _uiCamera;
    protected int _uiLayer;

    /// <summary>
    ///     组件初始化
    /// </summary>
    protected void Awake()
    {
        FindUICameraAndLayer();
    }

    /// <summary>
    ///     查找 UI 相机
    /// </summary>
    protected void FindUICameraAndLayer()
    {
        if (GameMain.Instance.OvrMgr != null && GameMain.Instance.OvrMgr.SystemUICamera != null)
        {
            var vrUiCamera = GameMain.Instance.OvrMgr.SystemUICamera;
            var uiCameraComponent = vrUiCamera.GetComponent<Camera>();
            if (uiCameraComponent != null)
            {
                _uiCamera = uiCameraComponent;
                _uiLayer = vrUiCamera.gameObject.layer;
            }
            else
            {
                LogManager.Warning(
                    "Cannot find UI Camera, VR tablet subtitle component will not be enabled/找不到 UI 相机，VR 平板字幕组件无法启用");
            }
        }
    }


    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected override void CreateSubtitleUI()
    {
        // 在 VR 模式中原有 UI 会被放到一个悬浮平板电脑上
        // 通过创建一个 rtOVRUI Render Texture，然后把 UI Camera 重定向到这个 RT 上
        // 然后把 rtOVRUI 设置为为 Odogu_TabletPC（平板电脑）的材质
        // 因此我们需要使用相机渲染模式

        // 创建画布
        var canvasObj = new GameObject("JAT_Subtitle_VRTabletSubtitleComponent_Canvas");
        canvasObj.transform.SetParent(transform, false);
        CanvasComponents = canvasObj.AddComponent<Canvas>();
        CanvasComponents.renderMode = RenderMode.ScreenSpaceCamera; // 屏幕空间-相机
        CanvasComponents.sortingOrder = 32767; // 确保在最上层显示
        CanvasComponents.pixelPerfect = true;
        CanvasComponents.overrideSorting = true;

        if (_uiCamera != null)
        {
            CanvasComponents.worldCamera = _uiCamera;
            canvasObj.layer = _uiLayer;
        }
        else
        {
            LogManager.Warning(
                "Cannot find UI Camera, VR tablet subtitle component will not be enabled/找不到 UI 相机，VR 平板字幕组件无法启用");
            return;
        }

        // 添加画布缩放器
        CanvasScalerComponents = canvasObj.AddComponent<CanvasScaler>();
        CanvasScalerComponents.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        CanvasScalerComponents.referenceResolution = new Vector2(1920, 1080);
        CanvasScalerComponents.matchWidthOrHeight = 0.5f; // 0是宽度，1是高度，0.5是两者平衡
        CanvasScalerComponents.dynamicPixelsPerUnit = 100; // 每个单位的像素数量

        // 添加画布组
        CanvasGroupComponents = CanvasComponents.gameObject.AddComponent<CanvasGroup>();
        CanvasGroupComponents.alpha = 1f;
        CanvasGroupComponents.blocksRaycasts = false; // 不阻挡射线（不阻止点击）
        CanvasGroupComponents.interactable = false; // 不可交互

        // 创建背景面板
        var backgroundObj = new GameObject("JAT_Subtitle_VRTabletSubtitleComponent_Background");
        backgroundObj.transform.SetParent(canvasObj.transform, false);
        backgroundObj.layer = canvasObj.layer;
        BackgroundImageComponents = backgroundObj.AddComponent<Image>();
        BackgroundImageComponents.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景
        BackgroundImageComponents.raycastTarget = false; // 不拦截射线

        // 设置背景位置和大小
        var backgroundRect = BackgroundImageComponents.rectTransform;
        backgroundRect.anchoredPosition3D =
            new Vector3(0, 0, 0); // 位置，默认位置为 Canvas 左下角，X 为距离 Canvas 左边的距离，Y为距离 Canvas 下边的距离
        var size = new Vector2(1920, 30);
        backgroundRect.sizeDelta = size; // 大小，取决于 Canvas 的参考分辨率，这里设置为 1920 等于全屏宽
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(0, 0); // 锚点为Canvas左下角
        backgroundRect.pivot = new Vector2(0, 0); // 轴心0，左下角
        backgroundRect.rotation = Quaternion.identity; // 旋转0度
        backgroundRect.localScale = new Vector3(1, 1, 1); // 缩放1倍

        // 创建文本对象
        var textObj = new GameObject("JAT_Subtitle_VRTabletSubtitleComponent_Text");
        textObj.transform.SetParent(backgroundObj.transform, false);
        textObj.layer = backgroundObj.layer;
        TextComponent = textObj.AddComponent<Text>();
        TextComponent.raycastTarget = false; // 不拦截射线
        TextComponent.supportRichText = true; // 支持富文本

        // 设置文本位置和大小，与背景完全一致
        var textRect = TextComponent.rectTransform;
        textRect.anchoredPosition3D = new Vector3(0, 0, 0); // 注意可以跟随父级移动，因此只需要移动背景即可
        textRect.sizeDelta = size;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(0, 0);
        textRect.pivot = new Vector2(0, 0);
        textRect.rotation = Quaternion.identity;
        textRect.localScale = new Vector3(1, 1, 1);

        // 设置默认文本样式
        TextComponent.alignment = TextAnchor.MiddleCenter; // 居中对齐
        TextComponent.horizontalOverflow = HorizontalWrapMode.Overflow; // 垂直溢出时自动换行
        TextComponent.verticalOverflow = VerticalWrapMode.Truncate; // 水平溢出时截断

        // 添加描边组件
        OutlineComponents = TextComponent.gameObject.AddComponent<Outline>();
        OutlineComponents.effectColor = new Color(0, 0, 0, 0.5f); // 黑色描边颜色
        OutlineComponents.effectDistance = new Vector2(1, 1); // 描边宽度


        LogManager.Debug("VRTabletSubtitleComponent Subtitle UI created");
    }
}