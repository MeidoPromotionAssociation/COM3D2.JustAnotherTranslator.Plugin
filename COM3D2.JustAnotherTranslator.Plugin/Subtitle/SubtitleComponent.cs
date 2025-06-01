using System;
using System.Collections;
using COM3D2.JustAnotherTranslator.Plugin.Subtitle.ConfigStrategy;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     字幕组件，负责显示游戏字幕
/// </summary>
public class SubtitleComponent : MonoBehaviour, ISubtitleComponent
{
    // 字幕配置
    private SubtitleConfig _config;
    private ISubtitleConfigStrategy _configStrategy;

    // 说话者名称
    private string _speakerName;

    // Unity UI组件
    private Canvas _canvas;
    private CanvasScaler _canvasScaler;
    private CanvasGroup _canvasGroup;
    private RectTransform _backgroundRect;
    private Image _backgroundImage;
    private Text _text;
    private RectTransform _textRect;
    private Outline _outline;

    // VR相关
    private Transform _vrHeadTransform;
    private Transform _vrSubtitleContainer;

    // 动画相关
    private Coroutine _animationCoroutine;

    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    public void Initialize(SubtitleConfig config)
    {
        _config = config ?? SubtitleConfigManager.Instance.GetCurrentConfig();
        _configStrategy = SubtitleConfigStrategyFactory.CreateStrategy(_config);
        _configStrategy.CreateSubtitleUI(this);
        _configStrategy.ApplyConfig(this);

        // 订阅配置变更事件
        SubtitleConfigManager.Instance.ConfigChanged += OnConfigChanged;

        gameObject.SetActive(false);
        LogManager.Debug("字幕组件已初始化/Subtitle component initialized");
    }

    /// <summary>
    ///     当配置变更时调用
    /// </summary>
    /// <param name="newConfig">新配置</param>
    private void OnConfigChanged(SubtitleConfig newConfig)
    {
        UpdateConfig(newConfig);
    }

    /// <summary>
    ///     初始化VR组件
    /// </summary>
    /// <param name="vrHeadTransform">VR头部变换组件</param>
    public void InitVRComponents(Transform vrHeadTransform)
    {
        _vrHeadTransform = vrHeadTransform;
        LogManager.Debug("VR组件已初始化/VR components initialized");
    }

    /// <summary>
    ///     显示字幕
    /// </summary>
    /// <param name="text">要显示的文本</param>
    /// <param name="speakerName">说话者名称，可为空</param>
    /// <param name="duration">显示持续时间，0表示持续显示</param>
    public void ShowSubtitle(string text, string speakerName, float duration)
    {
        if (_configStrategy is null)
        {
            LogManager.Error("字幕配置策略为空，无法显示字幕/Subtitle config strategy is null, cannot show subtitle");
            return;
        }

        _configStrategy.HandleShowSubtitle(this, text, speakerName, duration);
    }

    /// <summary>
    ///     隐藏字幕
    /// </summary>
    public void HideSubtitle()
    {
        if (_configStrategy is null)
        {
            LogManager.Error("字幕配置策略为空，无法隐藏字幕/Subtitle config strategy is null, cannot hide subtitle");
            return;
        }

        _configStrategy.HandleHideSubtitle(this);
    }

    /// <summary>
    ///     更新字幕配置
    /// </summary>
    /// <param name="config">新的字幕配置</param>
    public void UpdateConfig(SubtitleConfig config)
    {
        if (config is null)
        {
            LogManager.Warning("字幕配置为空，无法应用/Subtitle config is null, cannot apply");
            return;
        }

        var currentStrategy = _configStrategy;
        var newStrategy = SubtitleConfigStrategyFactory.CreateStrategy(config);

        if (currentStrategy.GetType() != newStrategy.GetType())
        {
            var currentText = GetText();
            var currentSpeakerName = _speakerName ?? string.Empty;

            DestroySubtitleUI();

            _config = config;
            _configStrategy = newStrategy;

            _configStrategy.CreateSubtitleUI(this);
            _configStrategy.ApplyConfig(this);

            if (!string.IsNullOrEmpty(currentText))
                ShowSubtitle(currentText, currentSpeakerName, 0f);
        }
        else
        {
            _config = config;
            _configStrategy.UpdateConfig(this, config);
        }
    }

    /// <summary>
    ///     获取当前字幕配置
    /// </summary>
    /// <returns>当前字幕配置</returns>
    public SubtitleConfig GetConfig()
    {
        return _config;
    }

    /// <summary>
    ///     获取当前显示的文本
    /// </summary>
    /// <returns>当前显示的文本</returns>
    public string GetText()
    {
        return _text?.text ?? string.Empty;
    }

    /// <summary>
    ///     获取当前说话者名称
    /// </summary>
    /// <returns>当前说话者名称</returns>
    public string GetSpeakerName()
    {
        return _speakerName ?? string.Empty;
    }

    /// <summary>
    ///     检查字幕是否可见
    /// </summary>
    /// <returns>字幕是否可见</returns>
    public bool IsVisible()
    {
        return gameObject.activeSelf;
    }

    /// <summary>
    ///     销毁字幕组件
    /// </summary>
    public void Destroy()
    {
        // 取消订阅配置变更事件
        SubtitleConfigManager.Instance.ConfigChanged -= OnConfigChanged;
        
        DestroySubtitleUI();
        Destroy(gameObject);
        LogManager.Debug("字幕组件已销毁/Subtitle component destroyed");
    }

    /// <summary>
    ///     销毁字幕UI
    /// </summary>
    private void DestroySubtitleUI()
    {
        if (_canvas != null)
        {
            Destroy(_canvas.gameObject);
            _canvas = null;
        }

        if (_vrSubtitleContainer != null)
        {
            Destroy(_vrSubtitleContainer.gameObject);
            _vrSubtitleContainer = null;
        }

        _canvasScaler = null;
        _canvasGroup = null;
        _backgroundRect = null;
        _backgroundImage = null;
        _text = null;
        _textRect = null;
        _outline = null;

        LogManager.Debug("字幕UI已销毁/Subtitle UI destroyed");
    }

    /// <summary>
    ///     淡入淡出Canvas透明度
    /// </summary>
    /// <param name="fromAlpha">初始透明度</param>
    /// <param name="toAlpha">目标透明度</param>
    /// <param name="duration">动画持续时间</param>
    /// <param name="onComplete">动画完成回调</param>
    /// <returns>协程</returns>
    public Coroutine StartFade(float fromAlpha, float toAlpha, float duration, Action onComplete = null)
    {
        // 停止正在运行的动画
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        // 启动新动画
        _animationCoroutine = StartCoroutine(FadeCanvasAlpha(fromAlpha, toAlpha, duration, onComplete));
        return _animationCoroutine;
    }

    /// <summary>
    ///     淡入淡出Canvas透明度协程
    /// </summary>
    private IEnumerator FadeCanvasAlpha(float fromAlpha, float toAlpha, float duration, Action onComplete = null)
    {
        if (_canvasGroup is null)
        {
            LogManager.Error("Canvas组为空，无法执行淡入淡出动画/Canvas group is null, cannot fade");
            yield break;
        }

        _canvasGroup.alpha = fromAlpha;
        var timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            _canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        _canvasGroup.alpha = toAlpha;
        onComplete?.Invoke();
        _animationCoroutine = null;
    }

    #region 访问器方法

    /// <summary>
    ///     设置字幕文本
    /// </summary>
    /// <param name="text">文本内容</param>
    public void SetText(string text)
    {
        if (_text != null)
            _text.text = text;
    }

    /// <summary>
    ///     设置说话者名称
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    public void SetSpeakerName(string speakerName)
    {
        _speakerName = speakerName;
    }

    /// <summary>
    ///     设置组件活动状态
    /// </summary>
    /// <param name="active">是否活动</param>
    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    /// <summary>
    ///     设置Canvas组透明度
    /// </summary>
    /// <param name="alpha">透明度值</param>
    public void SetCanvasGroupAlpha(float alpha)
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }

    /// <summary>
    ///     获取Canvas组透明度
    /// </summary>
    /// <returns>透明度值</returns>
    public float GetCanvasGroupAlpha()
    {
        return _canvasGroup?.alpha ?? 0f;
    }

    /// <summary>
    ///     设置背景尺寸
    /// </summary>
    /// <param name="size">尺寸</param>
    public void SetBackgroundSize(Vector2 size)
    {
        if (_backgroundRect != null)
            _backgroundRect.sizeDelta = size;
    }

    /// <summary>
    ///     获取文本组件
    /// </summary>
    /// <returns>文本组件</returns>
    public Text GetTextComponent()
    {
        return _text;
    }

    /// <summary>
    ///     设置文本组件
    /// </summary>
    /// <param name="text">文本组件</param>
    public void SetTextComponent(Text text)
    {
        _text = text;
    }

    /// <summary>
    ///     获取Canvas组件
    /// </summary>
    /// <returns>Canvas组件</returns>
    public Canvas GetCanvas()
    {
        return _canvas;
    }

    /// <summary>
    ///     设置Canvas组件
    /// </summary>
    /// <param name="canvas">Canvas组件</param>
    public void SetCanvas(Canvas canvas)
    {
        _canvas = canvas;
    }

    /// <summary>
    ///     获取Canvas缩放器
    /// </summary>
    /// <returns>Canvas缩放器</returns>
    public CanvasScaler GetCanvasScaler()
    {
        return _canvasScaler;
    }

    /// <summary>
    ///     设置Canvas缩放器
    /// </summary>
    /// <param name="canvasScaler">Canvas缩放器</param>
    public void SetCanvasScaler(CanvasScaler canvasScaler)
    {
        _canvasScaler = canvasScaler;
    }

    /// <summary>
    ///     获取Canvas组
    /// </summary>
    /// <returns>Canvas组</returns>
    public CanvasGroup GetCanvasGroup()
    {
        return _canvasGroup;
    }

    /// <summary>
    ///     设置Canvas组
    /// </summary>
    /// <param name="canvasGroup">Canvas组</param>
    public void SetCanvasGroup(CanvasGroup canvasGroup)
    {
        _canvasGroup = canvasGroup;
    }

    /// <summary>
    ///     获取背景矩形
    /// </summary>
    /// <returns>背景矩形</returns>
    public RectTransform GetBackgroundRect()
    {
        return _backgroundRect;
    }

    /// <summary>
    ///     设置背景矩形
    /// </summary>
    /// <param name="backgroundRect">背景矩形</param>
    public void SetBackgroundRect(RectTransform backgroundRect)
    {
        _backgroundRect = backgroundRect;
    }

    /// <summary>
    ///     获取背景图像
    /// </summary>
    /// <returns>背景图像</returns>
    public Image GetBackgroundImage()
    {
        return _backgroundImage;
    }

    /// <summary>
    ///     设置背景图像
    /// </summary>
    /// <param name="backgroundImage">背景图像</param>
    public void SetBackgroundImage(Image backgroundImage)
    {
        _backgroundImage = backgroundImage;
    }

    /// <summary>
    ///     获取文本矩形
    /// </summary>
    /// <returns>文本矩形</returns>
    public RectTransform GetTextRect()
    {
        return _textRect;
    }

    /// <summary>
    ///     设置文本矩形
    /// </summary>
    /// <param name="textRect">文本矩形</param>
    public void SetTextRect(RectTransform textRect)
    {
        _textRect = textRect;
    }

    /// <summary>
    ///     获取描边组件
    /// </summary>
    /// <returns>描边组件</returns>
    public Outline GetOutline()
    {
        return _outline;
    }

    /// <summary>
    ///     设置描边组件
    /// </summary>
    /// <param name="outline">描边组件</param>
    public void SetOutline(Outline outline)
    {
        _outline = outline;
    }

    /// <summary>
    ///     获取VR头部变换
    /// </summary>
    /// <returns>VR头部变换</returns>
    public Transform GetVRHeadTransform()
    {
        return _vrHeadTransform;
    }

    /// <summary>
    ///     获取VR字幕容器
    /// </summary>
    /// <returns>VR字幕容器</returns>
    public Transform GetVRSubtitleContainer()
    {
        return _vrSubtitleContainer;
    }

    /// <summary>
    ///     设置VR字幕容器
    /// </summary>
    /// <param name="vrSubtitleContainer">VR字幕容器</param>
    public void SetVRSubtitleContainer(Transform vrSubtitleContainer)
    {
        _vrSubtitleContainer = vrSubtitleContainer;
    }

    #endregion
}