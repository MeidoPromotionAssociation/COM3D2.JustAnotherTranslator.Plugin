using System;
using System.Collections;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     字幕组件的基类，提供共享功能
/// </summary>
public abstract class BaseSubtitleComponent : MonoBehaviour, ISubtitleComponent
{
    // 动画协程
    protected Coroutine _animationCoroutine;

    // 字幕背景图像组件
    protected Image _backgroundImage;

    // 字幕画布组件
    protected Canvas _canvas;

    // 字幕画布缩放器组件
    protected CanvasScaler _canvasScaler;

    // 字幕配置
    protected SubtitleConfig _config;

    // 当前动画协程
    protected Coroutine _currentAnimation;

    // 是否初始化完成
    protected bool _initialized;

    // 字幕描边组件
    protected Outline _outline;

    // 说话者名称
    protected string _speakerName;

    // 字幕文本组件
    protected Text _text;

    // 说话者文本颜色
    protected string SpeakerColor;

    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    public virtual void Initialize(SubtitleConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // 创建UI
        CreateSubtitleUI();

        // 应用配置
        ApplyConfig();

        gameObject.SetActive(false);
        LogManager.Debug($"{GetType().Name} initialized");
    }

    /// <summary>
    ///     显示字幕
    /// </summary>
    /// <param name="text">要显示的文本</param>
    /// <param name="speakerName">说话者名称，可为空</param>
    /// <param name="duration">显示持续时间，0表示持续显示</param>
    public virtual void ShowSubtitle(string text, string speakerName, float duration)
    {
        // 如果有正在进行的动画，停止它
        if (_currentAnimation is not null)
        {
            StopCoroutine(_currentAnimation);
            _currentAnimation = null;
        }

        if (string.IsNullOrEmpty(SpeakerColor))
            SpeakerColor = ColorUtility.ToHtmlStringRGB(GetSpeakerColor(speakerName));

        // 设置文本内容
        SetText(text, speakerName, SpeakerColor, _config.EnableSpeakerName);

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
    public virtual void HideSubtitle()
    {
        // 如果有正在进行的动画，停止它
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
            _currentAnimation = null;
        }

        // 如果启用了动画效果
        if (_config.EnableAnimation && gameObject.activeSelf)
            _currentAnimation = StartCoroutine(FadeOut());
        else
            gameObject.SetActive(false);

        LogManager.Debug("Hiding subtitle");
    }

    /// <summary>
    ///     更新字幕配置
    /// </summary>
    /// <param name="config">新的字幕配置</param>
    public virtual void UpdateConfig(SubtitleConfig config)
    {
        if (config == null)
        {
            LogManager.Warning("字幕配置为空，无法更新/Subtitle config is null, cannot update");
            return;
        }

        _config = config;
        ApplyConfig();
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
    ///     获取当前字幕ID
    /// </summary>
    public string GetSubtitleId()
    {
        return GetGameObject().name;
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
    public virtual void Destroy()
    {
        StopAnimation();
        DestroySubtitleUI();
        Destroy(gameObject);
        LogManager.Debug($"{GetType().Name} destroyed");
    }

    /// <summary>
    ///     获取游戏对象
    /// </summary>
    /// <returns>字幕游戏对象</returns>
    public GameObject GetGameObject()
    {
        return gameObject;
    }


    /// <summary>
    ///     设置文本内容
    /// </summary>
    /// <param name="text">要显示的文本</param>
    /// <param name="speakerName">说话者名称，可为空</param>
    /// <param name="speakerColor">说话者颜色，可为空</param>
    /// <param name="enableSpeakerName">是否启用说话者名称</param>
    private void SetText(string text, string speakerName, string speakerColor, bool enableSpeakerName)
    {
        var displayText = text;
        // 设置文本，无需处理 XUAT 互操作以及翻译，因为获取时已经被翻译了
        // 但人名需要被翻译
        if (!string.IsNullOrEmpty(speakerName) && enableSpeakerName)
        {
            TextTranslator.GetTranslateText(speakerName, out var translatedSpeakerName);
            displayText = $"<color=#{speakerColor}>{translatedSpeakerName}</color>: {text}";
        }

        _text.text = displayText;

        // 动态调整背景大小以适应文本高度
        if (_text is not null && !string.IsNullOrEmpty(_text.text) && _backgroundImage is not null)
        {
            // 获取文本内容的预计高度
            var textGenerator = _text.cachedTextGenerator;
            if (textGenerator.characterCountVisible == 0)
                _text.cachedTextGenerator.Populate(_text.text,
                    _text.GetGenerationSettings(_text.rectTransform.rect.size));

            // 计算实际行数与高度
            float textHeight = textGenerator.lineCount * _text.fontSize;

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
    ///     创建字幕UI
    /// </summary>
    protected abstract void CreateSubtitleUI();

    /// <summary>
    ///     应用配置到UI
    /// </summary>
    protected virtual void ApplyConfig()
    {
        // TODO 应用配置
        if (_config == null) LogManager.Warning("Subtitle config is null, cannot apply/字幕配置为空，无法应用");
    }

    /// <summary>
    ///     停止正在进行的动画
    /// </summary>
    protected virtual void StopAnimation()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
    }


    /// <summary>
    ///     销毁字幕UI
    /// </summary>
    protected virtual void DestroySubtitleUI()
    {
        if (_canvas != null)
        {
            Destroy(_canvas.gameObject);
            _canvas = null;
        }
    }

    /// <summary>
    ///     设置透明度
    /// </summary>
    /// <param name="alpha">透明度值（0-1）</param>
    protected virtual void SetAlpha(float alpha)
    {
        // 设置文本透明度
        var textColor = _text.color;
        textColor.a = alpha;
        _text.color = textColor;

        // 设置背景透明度
        var bgColor = _backgroundImage.color;
        bgColor.a = alpha * _config.BackgroundColor.a; // 保持背景的相对透明度
        _backgroundImage.color = bgColor;
    }


    /// <summary>
    ///     淡入动画
    /// </summary>
    protected virtual IEnumerator FadeIn()
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
    ///     淡出动画协程
    /// </summary>
    protected virtual IEnumerator FadeOut()
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
    protected virtual IEnumerator AutoHide(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideSubtitle();
    }


    /// <summary>
    ///     获取说话者的专属颜色
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    /// <returns>专属颜色</returns>
    protected virtual Color GetSpeakerColor(string speakerName)
    {
        // 使用哈希值生成颜色，确保相同名称总是获得相同颜色
        var random = new Random(speakerName.GetHashCode());

        // 生成偏亮的颜色
        var color = new Color(
            0.5f + (float)random.NextDouble() * 0.5f, // 0.5-1.0 范围
            0.5f + (float)random.NextDouble() * 0.5f,
            0.5f + (float)random.NextDouble() * 0.5f
        );

        LogManager.Debug($"Created Color R:{color.r:F2} G:{color.g:F2} B:{color.b:F2} for {speakerName}");

        return color;
    }
}