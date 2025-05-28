using System;
using System.Collections;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     通用字幕组件，用于在游戏中显示可配置的字幕
/// </summary>
public class SubtitleComponent : MonoBehaviour
{
    // 字幕跟随平滑度
    private readonly float _followSmoothness = 5.0f;

    // 字幕背景图像组件
    private Image _backgroundImage;

    // 字幕画布组件
    private Canvas _canvas;

    // 字幕画布缩放器组件
    private CanvasScaler _canvasScaler;

    // 字幕配置
    private SubtitleConfig _config;

    // 当前动画协程
    private Coroutine _currentAnimation;

    // 是否初始化完成
    private bool _initialized;

    // 字幕描边组件
    private Outline _outline;

    // 字幕文本组件
    private Text _textComponent;

    // VR头部位置参考，用于跟随头部运动
    private Transform _vrHeadTransform;

    // 世界空间VR字幕
    private RectTransform _vrSpaceCanvasRect;

    // VR悬浮字幕容器
    private GameObject _vrSubtitleContainer;

    /// <summary>
    ///     每帧更新，用于VR模式下跟随头部运动
    /// </summary>
    private void Update()
    {
        if (!JustAnotherTranslator.IsVrMode ||
            _config.VRSubtitleMode != JustAnotherTranslator.VRSubtitleModeEnum.InSpace)
            return;

        if (_vrHeadTransform is null || !_initialized || !gameObject.activeSelf)
            return;

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
        if (JustAnotherTranslator.IsVrMode)
            CreateVRSubtitleUI();
        else
            CreateSubtitleUI();

        // 应用配置
        ApplyConfig();

        // 如果是VR模式且启用了悬浮字幕，获取OvrMgr的EyeAnchor
        if (JustAnotherTranslator.IsVrMode &&
            _config.VRSubtitleMode is JustAnotherTranslator.VRSubtitleModeEnum.InSpace)
            // 延迟初始化VR相关组件，因为GameMain.Instance.OvrMgr可能还未初始化
            StartCoroutine(InitVRComponents());
        else
            _initialized = true;
    }


    /// <summary>
    ///     初始化VR空间字幕组件
    /// </summary>
    private IEnumerator InitVRComponents()
    {
        // 查找 OvrMgr 的 EyeAnchor
        if (GameMain.Instance is not null && GameMain.Instance.OvrMgr is not null)
        {
            _vrHeadTransform = GameMain.Instance.OvrMgr.EyeAnchor;
            if (_vrHeadTransform is not null)
            {
                LogManager.Debug("VR head transform (EyeAnchor) found, subtitle head tracking enabled");
                _initialized = true;
                yield break;
            }
        }

        if (_vrHeadTransform is null)
        {
            // 如果无法通过GameMain.Instance.OvrMgr获取，尝试直接查找OvrMgr
            var ovrMgr = FindObjectOfType<OvrMgr>();
            if (ovrMgr is not null && ovrMgr.EyeAnchor is not null)
            {
                _vrHeadTransform = ovrMgr.EyeAnchor;
                LogManager.Debug(
                    "VR head transform found through FindObjectOfType<OvrMgr>(), subtitle head tracking enabled");
                _initialized = true;
            }
            else
            {
                LogManager.Warning(
                    "VR head transform not found after multiple attempts, head tracking will not work/多次尝试后找不到VR头部变换，头部字幕跟踪将无法工作");
            }
        }
    }

    /// <summary>
    ///     创建字幕UI
    /// </summary>
    private void CreateSubtitleUI()
    {
        // 创建画布
        var canvasObj = new GameObject("JAT_SubtitleCanvas");
        canvasObj.transform.SetParent(transform, false);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32767; // 确保在最上层显示

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
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 0);
        backgroundRect.pivot = new Vector2(0.5f, 0);
        backgroundRect.sizeDelta = new Vector2(0, 100);
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

        LogManager.Debug("Subtitle UI created/字幕UI已创建");
    }


    /// <summary>
    ///     创建VR字幕UI
    /// </summary>
    private void CreateVRSubtitleUI()
    {
        LogManager.Debug($"VR mode detected, _config.VRSubtitleMode: {_config.VRSubtitleMode}");
        // 显示在平板电脑上
        if (_config.VRSubtitleMode == JustAnotherTranslator.VRSubtitleModeEnum.OnTablet)
        {
            // 在 VR 模式中原有 UI 会被放到一个悬浮平板电脑上，见 OvrTablet
            var canvasObj = new GameObject("JAT_SubtitleCanvas_VR_Tablet");

            var ovrTablet = FindObjectOfType<OvrTablet>();
            if (ovrTablet is not null)
            {
                var screenTransform = ovrTablet.transform.Find("Screen");
                if (screenTransform is null)
                {
                    LogManager.Warning(
                        "OvrTablet Screen not found, subtitles mar will not display on VR tablet/OvrTablet Screen 未找到，VR平板电脑上的字幕可能不会显示");
                    canvasObj.transform.SetParent(transform, false);
                }
                else
                {
                    canvasObj.transform.SetParent(screenTransform, false);
                    canvasObj.layer = screenTransform.gameObject.layer;
                }
            }

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 32767; // 确保在最上层显示

            _canvas.transform.localPosition = Vector3.zero;
            _canvas.transform.localRotation = Quaternion.identity;
            _canvas.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

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
            backgroundRect.anchorMin = new Vector2(0, 0); // 左下角
            backgroundRect.anchorMax = new Vector2(1, 0); // 右下角
            backgroundRect.pivot = new Vector2(0.5f, 0);
            backgroundRect.sizeDelta = new Vector2(0, 100);
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

            LogManager.Debug("VR Subtitle UI created");
        }
        // 显示在空间中（悬浮字幕）
        else if (_config.VRSubtitleMode == JustAnotherTranslator.VRSubtitleModeEnum.InSpace)
        {
            // 创建一个容器来承载悬浮字幕
            _vrSubtitleContainer = new GameObject("JAT_SubtitleContainer_VR_Space");
            _vrSubtitleContainer.transform.SetParent(transform, false);

            // 创建世界空间Canvas
            var canvasObj = new GameObject("JAT_SubtitleCanvas_VR_Space");
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
            var backgroundObj = new GameObject("JAT_SubtitleBackground");
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

            LogManager.Debug("VR subtitle container created");
        }
        else
        {
            LogManager.Warning("Unsupported VR subtitle mode/不支持的VR字幕模式");
        }
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

        // 设置文本内容
        SetText(text, speakerName, speakerColor, EnableSpeakerName);


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
    ///     设置文本内容
    /// </summary>
    /// <param name="text">字幕文本</param>
    /// <param name="speakerName">说话者名称</param>
    /// <param name="speakerColor">说话者颜色</param>
    /// <param name="EnableSpeakerName">是否启用说话者名称显示</param>
    private void SetText(string text, string speakerName, string speakerColor, bool EnableSpeakerName)
    {
        var displayText = text;
        // 设置文本，无需处理 XUAT 互操作以及翻译，因为获取时已经被翻译了
        // 但人名需要被翻译
        if (!string.IsNullOrEmpty(speakerName) && EnableSpeakerName)
        {
            TextTranslator.GetTranslateText(speakerName, out var translatedSpeakerName);
            displayText = $"<color=#{speakerColor}>{translatedSpeakerName}</color>: {text}";
        }

        _textComponent.text = displayText;

        // 动态调整背景大小以适应文本高度
        if (_textComponent != null && !string.IsNullOrEmpty(_textComponent.text) && _backgroundImage != null)
        {
            // 获取文本内容的预计高度
            var textGenerator = _textComponent.cachedTextGenerator;
            if (textGenerator.characterCountVisible == 0)
                _textComponent.cachedTextGenerator.Populate(_textComponent.text,
                    _textComponent.GetGenerationSettings(_textComponent.rectTransform.rect.size));

            // 计算实际行数与高度
            float textHeight = textGenerator.lineCount * _textComponent.fontSize;

            // 只有当文本高度超过原始背景高度时才调整背景
            if (textHeight + 10 > _config.BackgroundHeight)
            {
                // 调整背景尺寸
                var backgroundRect = _backgroundImage.rectTransform;
                backgroundRect.sizeDelta = new Vector2(backgroundRect.sizeDelta.x, textHeight + 10); // 文本高度 + 边距
            }
        }
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
        // 计算实际文本高度（
        if (_textComponent is not null && !string.IsNullOrEmpty(_textComponent.text))
        {
            // 获取文本内容的预计高度
            var textGenerator = _textComponent.cachedTextGenerator;
            if (textGenerator.characterCountVisible == 0)
                _textComponent.cachedTextGenerator.Populate(_textComponent.text,
                    _textComponent.GetGenerationSettings(_textComponent.rectTransform.rect.size));

            // 计算实际行数与高度
            float textHeight = textGenerator.lineCount * _textComponent.fontSize;

            // 返回背景高度和文本实际高度中的最大值
            return Mathf.Max(_config.BackgroundHeight, textHeight + 10); // 加上上下边距
        }

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

    // VR模式字幕类型
    public JustAnotherTranslator.VRSubtitleModeEnum VRSubtitleMode { get; set; } =
        JustAnotherTranslator.VRSubtitleModeEnum.InSpace;

    // VR悬浮字幕距离（米）
    public float VRSubtitleDistance { get; set; } = 2f;

    // VR悬浮字幕垂直偏移（度，相对于视线中心）
    public float VRSubtitleVerticalOffset { get; set; } = -15f;

    // VR悬浮字幕水平偏移（度，相对于视线中心）
    public float VRSubtitleHorizontalOffset { get; set; } = 0f;

    // VR悬浮字幕宽度（米）
    public float VRSubtitleWidth { get; set; } = 1f;
}