using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     字幕组件的基类，提供共享功能
/// </summary>
public abstract class BaseSubtitleComponent : MonoBehaviour, ISubtitleComponent
{
    // 动画相关
    protected Coroutine _animationCoroutine;
    protected Image _backgroundImage;
    protected RectTransform _backgroundRect;

    // Unity UI组件
    protected Canvas _canvas;
    protected CanvasGroup _canvasGroup;

    protected CanvasScaler _canvasScaler;

    // 字幕配置
    protected SubtitleConfig _config;
    protected Outline _outline;

    // 说话者名称
    protected string _speakerName;
    protected Text _speakerText;
    protected RectTransform _speakerTextRect;
    protected Text _text;
    protected RectTransform _textRect;

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
        if (string.IsNullOrEmpty(text))
        {
            LogManager.Warning("字幕文本为空，不显示/Subtitle text is empty, not showing");
            return;
        }

        // 停止正在进行的动画
        StopAnimation();

        // 设置文本
        _text.text = text;
        _speakerName = speakerName;

        // 设置说话者名称
        if (_config.EnableSpeakerName && _speakerText != null && !string.IsNullOrEmpty(speakerName))
        {
            _speakerText.text = speakerName;
            _speakerText.gameObject.SetActive(true);
        }
        else if (_speakerText != null)
        {
            _speakerText.gameObject.SetActive(false);
        }

        // 显示字幕
        gameObject.SetActive(true);

        // 如果有持续时间，则启动隐藏协程
        if (duration > 0) _animationCoroutine = StartCoroutine(HideAfterDuration(duration));
    }

    /// <summary>
    ///     隐藏字幕
    /// </summary>
    public virtual void HideSubtitle()
    {
        // 停止正在进行的动画
        StopAnimation();

        // 直接隐藏
        gameObject.SetActive(false);
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
        LogManager.Debug($"{GetType().Name}已销毁/{GetType().Name} destroyed");
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
    ///     创建字幕UI
    /// </summary>
    protected abstract void CreateSubtitleUI();

    /// <summary>
    ///     应用配置到UI
    /// </summary>
    protected virtual void ApplyConfig()
    {
        if (_config == null)
        {
            LogManager.Warning("字幕配置为空，无法应用/Subtitle config is null, cannot apply");
            return;
        }

        // 设置字体大小
        if (_text != null)
        {
            _text.fontSize = _config.FontSize;
            _text.color = _config.TextColor;
        }

        // 设置说话者名称
        if (_speakerText != null)
        {
            _speakerText.gameObject.SetActive(_config.EnableSpeakerName);
            _speakerText.color = _config.SpeakerNameColor;
        }

        // 设置背景颜色
        if (_backgroundImage != null) _backgroundImage.color = _config.BackgroundColor;

        // 设置轮廓
        if (_outline != null)
        {
            _outline.effectColor = _config.OutlineColor;
            _outline.effectDistance = new Vector2(_config.OutlineWidth, _config.OutlineWidth);
        }
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
    ///     在指定时间后隐藏字幕
    /// </summary>
    /// <param name="duration">持续时间</param>
    /// <returns>协程</returns>
    protected IEnumerator HideAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        HideSubtitle();
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
}