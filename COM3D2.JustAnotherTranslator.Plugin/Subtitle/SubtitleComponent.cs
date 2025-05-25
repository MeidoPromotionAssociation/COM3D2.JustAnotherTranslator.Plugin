using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     通用字幕组件，用于在游戏中显示可配置的字幕
/// </summary>
public class SubtitleComponent : MonoBehaviour
{
    // 字幕背景图像组件
    private Image _backgroundImage;

    // 字幕画布组件
    private Canvas _canvas;

    // 字幕画布缩放器组件
    private CanvasScaler _canvasScaler;

    // 字幕配置
    private SubtitleConfig _config;

    // 当前协程
    private Coroutine _currentAnimation;

    // 字幕描边组件
    private Outline _outline;

    // 字幕文本组件
    private Text _textComponent;

    // VR主相机，用于跟随头部运动
    private Transform _vrCamera;

    // VR悬浮字幕容器
    private GameObject _vrSubtitleContainer;

    /// <summary>
    ///     每帧更新，用于VR模式下跟随头部运动
    /// </summary>
    private void Update()
    {
        if (!JustAnotherTranslator.IsVrMode)
            return;

        if (_vrCamera is null || _vrSubtitleContainer is null)
            return;

        // 只在VR悬浮字幕模式下执行
        if (_config.VRSubtitleMode == 1)
        {
            // 将字幕放置在相机前方固定距离处
            var cameraForward = _vrCamera.forward;
            var cameraRight = _vrCamera.right;
            var cameraUp = _vrCamera.up;

            // 应用垂直偏移（以角度为单位）
            var verticalRotation = Quaternion.AngleAxis(_config.VRSubtitleVerticalOffset, cameraRight);

            // 应用水平偏移（以角度为单位）
            var horizontalRotation = Quaternion.AngleAxis(_config.VRSubtitleHorizontalOffset, cameraUp);

            // 组合旋转
            var direction = horizontalRotation * verticalRotation * cameraForward;

            // 计算最终位置
            var targetPosition = _vrCamera.position + direction * _config.VRSubtitleDistance;

            // 应用到字幕容器
            _vrSubtitleContainer.transform.position = targetPosition;

            // 字幕始终正对讲者
            _vrSubtitleContainer.transform.rotation = Quaternion.LookRotation(direction, cameraUp);
        }
    }

    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    /// <param name="isVrMode">是否为VR模式</param>
    public void Initialize(SubtitleConfig config)
    {
        _config = config;

        // 创建字幕UI
        CreateSubtitleUI(JustAnotherTranslator.IsVrMode);

        // 应用配置
        ApplyConfig();

        // 如果是VR模式且启用了悬浮字幕，查找VR相机
        if (JustAnotherTranslator.IsVrMode && _config.VRSubtitleMode == 1)
        {
            // 尝试查找VR相机
            var cameraObject = GameObject.Find("Main Camera (head)");
            if (cameraObject is null)
            {
                LogManager.Warning(
                    "VR camera Main Camera (head) not found, head tracking will not work/找不到 VR 相机 Main Camera (head)，头部字幕跟踪将无法工作");
                return;
            }

            _vrCamera = cameraObject.transform;
            LogManager.Debug("VR camera found, subtitle head tracking enabled/找到VR相机，头部字幕跟踪已启用");
        }
    }

    /// <summary>
    ///     创建字幕UI
    /// </summary>
    private void CreateSubtitleUI(bool isVrMode)
    {
        // 创建画布
        if (!isVrMode)
        {
            var canvasObj = new GameObject("JAT_SubtitleCanvas");
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999; // 确保在最上层显示
        }
        else
        {
            // 在 VR 模式中原有 UI 会被放到一个悬浮平板电脑上
            // 所以需要挂载到 SystemUI Root 下，见 OvrTablet
            var canvasObj = new GameObject("JAT_SubtitleCanvas_VR");
            LogManager.Debug($"VR mode detected, _config.VRSubtitleMode: {_config.VRSubtitleMode}");
            {
                if (_config.VRSubtitleMode == 0) // 平板上的字幕
                {
                    var systemUI = GameObject.Find("SystemUI Root");
                    if (systemUI is null)
                    {
                        LogManager.Warning(
                            "SystemUI Root not found, subtitles will not display on VR tablet/SystemUI Root 未找到，VR平板上的字幕将不会显示");
                        canvasObj.transform.SetParent(transform, false);
                    }
                    else
                    {
                        canvasObj.transform.SetParent(systemUI.transform, false);
                        canvasObj.layer = systemUI.layer;
                    }

                    _canvas = canvasObj.AddComponent<Canvas>();
                    _canvas.renderMode = RenderMode.WorldSpace;
                    _canvas.sortingOrder = 9999; // 确保在最上层显示
                }
                else // 跟随头部的悬浮字幕
                {
                    canvasObj.transform.SetParent(transform, false);

                    _canvas = canvasObj.AddComponent<Canvas>();
                    _canvas.renderMode = RenderMode.WorldSpace;
                    _canvas.worldCamera = Camera.main;
                }
            }
        }

        // 添加画布缩放器
        _canvasScaler = _canvas.gameObject.AddComponent<CanvasScaler>();
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = new Vector2(1920, 1080);
        _canvasScaler.matchWidthOrHeight = 0.5f;

        // 创建背景面板
        var backgroundObj = new GameObject("JAT_SubtitleBackground");
        backgroundObj.transform.SetParent(_canvas.transform, false);
        _backgroundImage = backgroundObj.AddComponent<Image>();
        _backgroundImage.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景

        // 设置背景位置和大小
        var backgroundRect = _backgroundImage.rectTransform;

        if (isVrMode && _config.VRSubtitleMode == 1) // VR悬浮字幕模式
        {
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta =
                new Vector2(_config.VRSubtitleWidth * 1000, _config.BackgroundHeight); // 调整大小以适应VR视野
        }
        else // 普通字幕模式
        {
            backgroundRect.anchorMin = new Vector2(0, 0);
            backgroundRect.anchorMax = new Vector2(1, 0);
            backgroundRect.pivot = new Vector2(0.5f, 0);
            backgroundRect.sizeDelta = new Vector2(0, 100);
        }

        backgroundRect.anchoredPosition = new Vector2(0, 0);

        // 创建文本对象
        var textObj = new GameObject("JAT_SubtitleText");
        textObj.transform.SetParent(backgroundRect, false);
        _textComponent = textObj.AddComponent<Text>();

        // 设置文本位置和大小
        var textRect = _textComponent.rectTransform;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(-40, -20); // 左右上下各留出10像素的边距
        textRect.anchoredPosition = new Vector2(0, 0);

        // 设置默认文本样式
        _textComponent.alignment = TextAnchor.MiddleCenter;
        _textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        _textComponent.verticalOverflow = VerticalWrapMode.Overflow;

        // 设置文本和背景不拦截点击事件
        _textComponent.raycastTarget = false;
        _backgroundImage.raycastTarget = false;

        // 添加描边组件
        _outline = _textComponent.gameObject.AddComponent<Outline>();
        _outline.enabled = false;

        // 创建VR悬浮字幕容器
        if (isVrMode && _config.VRSubtitleMode == 1)
        {
            _vrSubtitleContainer = _canvas.gameObject;

            // 设置字幕位置，放在相机前方并偏移
            _vrSubtitleContainer.transform.localPosition = new Vector3(0f, _config.VRSubtitleVerticalOffset / 50f,
                _config.VRSubtitleDistance);
            _vrSubtitleContainer.transform.localRotation = Quaternion.identity;

            // 设置世界空间画布的大小
            _canvas.GetComponent<RectTransform>().sizeDelta =
                new Vector2(_config.VRSubtitleWidth * 1000, _config.BackgroundHeight * 2);

            LogManager.Debug("VR subtitle container created/VR悬浮字幕容器已创建");
        }

        LogManager.Debug("Subtitle UI created/字幕UI已创建");
    }

    /// <summary>
    ///     应用配置
    /// </summary>
    private void ApplyConfig()
    {
        if (_config == null)
        {
            LogManager.Warning("Subtitle config is null, cannot apply/字幕配置为空，无法应用");
            return;
        }

        // 应用文本样式
        _textComponent.font = _config.Font;
        _textComponent.fontSize = _config.FontSize;
        _textComponent.color = _config.TextColor;
        _textComponent.fontStyle = _config.FontStyle;

        // 应用背景样式
        _backgroundImage.color = _config.BackgroundColor;

        // 应用位置
        var backgroundRect = _backgroundImage.rectTransform;
        backgroundRect.anchorMin = new Vector2(0, _config.VerticalPosition);
        backgroundRect.anchorMax = new Vector2(1, _config.VerticalPosition);
        backgroundRect.sizeDelta = new Vector2(0, _config.BackgroundHeight);

        // 应用描边效果
        if (_config.EnableOutline)
        {
            _outline.enabled = true;
            _outline.effectColor = _config.OutlineColor;
            _outline.effectDistance = new Vector2(_config.OutlineWidth, _config.OutlineWidth);
        }
        else
        {
            _outline.enabled = false;
        }

        LogManager.Debug("Subtitle config applied");
    }

    /// <summary>
    ///     显示字幕
    /// </summary>
    /// <param name="text">字幕文本</param>
    /// <param name="duration">显示时长（秒），0表示一直显示直到手动隐藏</param>
    /// <param name="speakerName">说话者名称，会以不同颜色显示在文本前</param>
    /// <param name="speakerColor">说话者颜色，需要是 ColorUtility.ToHtmlStringRGB(color)</param>
    public void Show(string text, float duration, string speakerName, string speakerColor, bool EnableSpeakerName)
    {
        // 如果有正在进行的动画，停止它
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
            _currentAnimation = null;
        }

        var displayText = text;
        // 设置文本，无需处理 XUAT 互操作以及翻译，因为获取时以及被翻译了
        if (!string.IsNullOrEmpty(speakerName) && EnableSpeakerName)
            displayText = $"<color=#{speakerColor}>{speakerName}</color>: {text}";

        _textComponent.text = displayText;

        // 显示字幕
        gameObject.SetActive(true);

        // 如果启用了动画效果
        if (_config.EnableAnimation)
        {
            // 开始淡入动画
            _currentAnimation = StartCoroutine(FadeIn());

            // 如果设置了持续时间，则在指定时间后淡出
            if (duration > 0) StartCoroutine(AutoHide(duration));
        }
        else
        {
            // 直接显示
            SetAlpha(1);

            // 如果设置了持续时间，则在指定时间后隐藏
            if (duration > 0) StartCoroutine(AutoHide(duration));
        }

        LogManager.Debug($"Showing subtitle: {text}");
    }

    /// <summary>
    ///     隐藏字幕
    /// </summary>
    public void Hide()
    {
        // 如果有正在进行的动画，停止它
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
            _currentAnimation = null;
        }

        // 如果启用了动画效果
        if (_config.EnableAnimation && gameObject.activeSelf)
            // 开始淡出动画
            _currentAnimation = StartCoroutine(FadeOut());
        else
            // 直接隐藏
            gameObject.SetActive(false);

        LogManager.Debug("Hiding subtitle");
    }

    /// <summary>
    ///     设置透明度
    /// </summary>
    /// <param name="alpha">透明度值（0-1）</param>
    private void SetAlpha(float alpha)
    {
        // 设置文本透明度
        var textColor = _textComponent.color;
        textColor.a = alpha;
        _textComponent.color = textColor;

        // 设置背景透明度
        var bgColor = _backgroundImage.color;
        bgColor.a = alpha * _config.BackgroundColor.a; // 保持背景的相对透明度
        _backgroundImage.color = bgColor;
    }

    /// <summary>
    ///     淡入动画
    /// </summary>
    private IEnumerator FadeIn()
    {
        float time = 0;
        SetAlpha(0);

        while (time < _config.FadeInDuration)
        {
            time += Time.deltaTime;
            var alpha = Mathf.Clamp01(time / _config.FadeInDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(1);
        _currentAnimation = null;
    }

    /// <summary>
    ///     淡出动画
    /// </summary>
    private IEnumerator FadeOut()
    {
        float time = 0;
        SetAlpha(1);

        while (time < _config.FadeOutDuration)
        {
            time += Time.deltaTime;
            var alpha = 1 - Mathf.Clamp01(time / _config.FadeOutDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(0);
        gameObject.SetActive(false);
        _currentAnimation = null;
    }

    /// <summary>
    ///     自动隐藏协程
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    private IEnumerator AutoHide(float delay)
    {
        yield return new WaitForSeconds(delay);
        Hide();
    }

    /// <summary>
    ///     更新配置
    /// </summary>
    /// <param name="config">新的配置</param>
    public void UpdateConfig(SubtitleConfig config)
    {
        _config = config;
        ApplyConfig();
        LogManager.Debug("Subtitle config updated");
    }

    /// <summary>
    ///     获取字幕ID
    /// </summary>
    public string GetSubtitleId()
    {
        return gameObject.name;
    }

    /// <summary>
    ///     获取字幕垂直位置
    /// </summary>
    public float GetVerticalPosition()
    {
        return _config.VerticalPosition;
    }

    /// <summary>
    ///     获取字幕高度
    /// </summary>
    public float GetHeight()
    {
        return _config.BackgroundHeight;
    }
}

/// <summary>
///     字幕配置类
/// </summary>
[Serializable]
public class SubtitleConfig
{
    // 是否启用说话人名字显示
    public bool EnableSpeakerName = true;

    // 字体
    public Font Font { get; set; }

    // 字体大小
    public int FontSize { get; set; } = 24;

    // 字体样式
    public FontStyle FontStyle { get; set; } = FontStyle.Normal;

    // 文本颜色
    public Color TextColor { get; set; } = Color.white;

    // 背景颜色
    public Color BackgroundColor { get; set; } = new(0, 0, 0, 0.5f);

    // 垂直位置（0-1，0表示底部，1表示顶部）
    public float VerticalPosition { get; set; }

    // 背景高度
    public float BackgroundHeight { get; set; } = 100;

    // 是否启用动画效果
    public bool EnableAnimation { get; set; } = true;

    // 淡入时长（秒）
    public float FadeInDuration { get; set; } = 0.5f;

    // 淡出时长（秒）
    public float FadeOutDuration { get; set; } = 0.5f;

    // 是否启用描边
    public bool EnableOutline { get; set; } = false;

    // 描边颜色
    public Color OutlineColor { get; set; } = Color.black;

    // 描边粗细
    public float OutlineWidth { get; set; } = 1f;

    // VR模式字幕类型：0=平板上的字幕，1=跟随头部的悬浮字幕
    public int VRSubtitleMode { get; set; } = 0;

    // VR悬浮字幕距离（米）
    public float VRSubtitleDistance { get; set; } = 2f;

    // VR悬浮字幕垂直偏移（度，相对于视线中心）
    public float VRSubtitleVerticalOffset { get; set; } = -15f;

    // VR悬浮字幕水平偏移（度，相对于视线中心）
    public float VRSubtitleHorizontalOffset { get; set; } = 0f;

    // VR悬浮字幕宽度（米）
    public float VRSubtitleWidth { get; set; } = 1f;
}