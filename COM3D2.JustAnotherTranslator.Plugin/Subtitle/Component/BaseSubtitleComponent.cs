using System;
using System.Collections;
using COM3D2.JustAnotherTranslator.Plugin.Manger;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     字幕组件的基类，提供共享功能
/// </summary>
public abstract class BaseSubtitleComponent : MonoBehaviour, ISubtitleComponent
{
    // 销毁幂等保护
    private bool _isDestroyed;

    // 自动隐藏协程
    protected Coroutine AnimationAutoHideCoroutine;

    // 动画协程
    protected Coroutine AnimationCoroutine;

    // 字幕背景图像组件
    protected Image BackgroundImageComponents;

    // 字幕画布组件
    protected Canvas CanvasComponents;

    // 画布组组件
    protected CanvasGroup CanvasGroupComponents;

    // 字幕画布缩放器组件
    protected CanvasScaler CanvasScalerComponents;

    // 字幕配置
    protected SubtitleConfig Config;

    // 字幕描边组件
    protected Outline OutlineComponents;

    // 字幕文本组件
    protected Text TextComponent;

    /// <summary>
    ///     销毁字幕组件
    /// </summary>
    public virtual void OnDestroy()
    {
        try
        {
            SubtitleComponentManager.UnregisterById(GetSubtitleId());
            Destroy(); // 已幕等保护
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    /// <param name="subtitleId">字幕ID</param>
    public virtual void Init(SubtitleConfig config, string subtitleId)
    {
        if (config is null)
        {
            LogManager.Warning(
                "Subtitle config is null, subtitle component will not be initialized/字幕配置为空，字幕组件将不会被初始化");
            return;
        }

        try
        {
            Config = config;

            // 创建UI
            CreateSubtitleUI();

            // 应用配置
            ApplyConfig();

            if (gameObject == null)
                throw new NullReferenceException(
                    "GameObject is null after creating UI, cannot initialize base subtitle component/创建UI后GameObject为空，无法初始化基础字幕组件");

            gameObject.name = subtitleId;
            SetActive(false);
            SetAlpha(0f);
            LogManager.Debug($"base subtitle component {subtitleId} initialized");
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Failed to initialize base subtitle component/基础字幕组件初始化失败: {e.Message}");
        }
    }

    /// <summary>
    ///     显示字幕
    /// </summary>
    /// <param name="text">要显示的文本</param>
    /// <param name="speakerName">说话者名称，可为空</param>
    /// <param name="duration">显示持续时间，0表示持续显示</param>
    public virtual void ShowSubtitle(string text, string speakerName, float duration)
    {
        if (Config == null)
        {
            LogManager.Warning("Subtitle config is null, cannot show subtitle/字幕配置为空，无法显示字幕");
            return;
        }

        // 如果有正在进行的动画，停止它
        StopAnimation();

        speakerName ??= "";
        var speakerColor = ColorUtility.ToHtmlStringRGBA(GetSpeakerColor(speakerName));

        // 如果下一个说话者没有自定义颜色，但没有恢复普通值，则会串色
        RestoreDefaultSubtitleColors();

        // 设置文本内容
        SetText(text, speakerName, speakerColor, Config.EnableSpeakerName);

        // 应用每个说话人的自定义字幕颜色
        ApplyCustomSubtitleColors(speakerName);

        // 显示字幕
        SetActive(true);

        // 如果启用了动画效果
        if (Config.EnableAnimation)
        {
            // 开始淡入动画
            AnimationCoroutine = StartCoroutine(FadeIn());

            // 如果设置了持续时间，则在指定时间后淡出
            if (duration > 0)
                AnimationAutoHideCoroutine = StartCoroutine(AutoHide(duration));
        }
        else
        {
            // 直接显示
            SetAlpha(1);

            // 如果设置了持续时间，则在指定时间后隐藏
            if (duration > 0)
                AnimationAutoHideCoroutine = StartCoroutine(AutoHide(duration));
        }

        LogManager.Debug(
            $"Showing subtitle: {text}, SpeakerName: {speakerName}, SpeakerColor: {speakerColor}, Duration: {duration}");
    }

    /// <summary>
    ///     隐藏字幕
    /// </summary>
    public virtual void HideSubtitle(bool skipAnimation = false)
    {
        // 如果有正在进行的动画，停止它
        StopAnimation();

        if (skipAnimation)
        {
            SetAlpha(0f);
            SetActive(false);
            LogManager.Debug($"Hiding subtitle {gameObject.name}");
            return;
        }

        // 如果当前 Alpha 已经是 0，直接跳过
        if (!gameObject.activeSelf ||
            (CanvasGroupComponents is not null && CanvasGroupComponents.alpha <= 0.01f))
        {
            SetAlpha(0f);
            SetActive(false);
            return;
        }


        // 如果启用了动画效果
        if (Config.EnableAnimation && gameObject.activeSelf)
        {
            AnimationCoroutine = StartCoroutine(FadeOut());
        }
        else
        {
            SetAlpha(0f);
            SetActive(false);
        }

        LogManager.Debug($"Hiding subtitle {gameObject.name}");
    }


    /// <summary>
    ///     更新字幕配置
    /// </summary>
    /// <param name="config">新的字幕配置</param>
    public virtual void UpdateConfig(SubtitleConfig config)
    {
        if (config == null)
        {
            LogManager.Warning("Subtitle config is null, cannot update/字幕配置为空，无法更新");
            return;
        }

        Config = config;
        ApplyConfig();
    }

    /// <summary>
    ///     获取当前字幕配置
    /// </summary>
    /// <returns>当前字幕配置</returns>
    public SubtitleConfig GetConfig()
    {
        return Config;
    }

    /// <summary>
    ///     获取当前显示的文本
    /// </summary>
    /// <returns>当前显示的文本</returns>
    public string GetText()
    {
        return TextComponent?.text ?? string.Empty;
    }

    /// <summary>
    ///     设置字幕ID
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public bool SetSubtitleId(string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
                return false;
            gameObject.name = text;
            return true;
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to set subtitle id/设置字幕ID失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    ///     获取当前字幕ID
    /// </summary>
    public string GetSubtitleId()
    {
        return gameObject.name;
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
        if (_isDestroyed) return;
        _isDestroyed = true;

        StopAnimation();
        DestroySubtitleUI();
        if (gameObject != null) Destroy(gameObject);
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
    ///     获取当前垂直位置
    /// </summary>
    /// <returns>垂直位置</returns>
    public virtual float GetCurrentVerticalPosition()
    {
        return Config?.CurrentVerticalPosition ?? 0f;
    }

    /// <summary>
    ///     获取当前字幕高度
    /// </summary>
    /// <returns>高度</returns>
    public virtual float GetSubtitleHeight()
    {
        if (Config == null) return 0f;

        var height = Config.SubtitleHeight;

        return height;
    }


    /// <summary>
    ///     设置当前垂直位置
    /// </summary>
    /// <param name="position"></param>
    public virtual void SetVerticalPosition(float position)
    {
        if (Config != null)
        {
            Config.CurrentVerticalPosition = position;

            // 应用位置
            if (BackgroundImageComponents is not null)
                BackgroundImageComponents.rectTransform.anchoredPosition3D = new Vector3(
                    Config.HorizontalPosition, position, 0f);
            return;
        }

        LogManager.Warning("Subtitle config is null, cannot set vertical position/字幕配置为空，无法设置垂直位置");
    }

    /// <summary>
    ///     获取 CanvasScaler 参考分辨率高度。
    ///     不依赖 Canvas.scaleFactor（Init 时 CanvasScaler 尚未更新，scaleFactor 仍为默认值 1，
    ///     会导致用 Screen.height/scaleFactor 算出错误的可见高度）。
    /// </summary>
    /// <returns>参考高度（默认 1080）</returns>
    public virtual float GetReferenceHeight()
    {
        if (CanvasScalerComponents != null)
            return CanvasScalerComponents.referenceResolution.y;
        LogManager.Warning(
            "CanvasScalerComponents is null, using default reference height 1080/CanvasScaler 为空，使用默认参考高度 1080");
        return 1080f;
    }

    /// <summary>
    ///     获取 Canvas 的渲染模式
    /// </summary>
    /// <returns>渲染模式，默认 ScreenSpaceOverlay</returns>
    public virtual RenderMode GetCanvasRenderMode()
    {
        if (CanvasComponents != null)
            return CanvasComponents.renderMode;
        LogManager.Warning(
            "CanvasComponents is null, using default render mode ScreenSpaceOverlay/Canvas 为空，使用默认渲染模式 ScreenSpaceOverlay");
        return RenderMode.ScreenSpaceOverlay;
    }

    /// <summary>
    ///     将字幕颜色重置回默认
    /// </summary>
    protected virtual void RestoreDefaultSubtitleColors()
    {
        if (Config == null)
            return;

        if (TextComponent != null)
            TextComponent.color = Config.TextColor;

        if (BackgroundImageComponents != null)
            BackgroundImageComponents.color = Config.BackgroundColor;

        if (OutlineComponents != null)
        {
            OutlineComponents.enabled = Config.EnableOutline;

            if (Config.EnableOutline)
            {
                OutlineComponents.effectColor = Config.OutlineColor;
                OutlineComponents.effectDistance =
                    new Vector2(Config.OutlineWidth, Config.OutlineWidth);
            }
        }
    }

    /// <summary>
    ///     钳制垂直位置到可见范围 [0, 参考高度-字幕高度]
    ///     CanvasScaler 使用 ScaleWithScreenSize + matchWidthOrHeight=1，
    ///     所有坐标均在参考分辨率单位下，可见高度恒为参考高度，与实际分辨率无关。
    /// </summary>
    /// <param name="y">期望的垂直位置</param>
    /// <param name="subtitleHeight">字幕高度</param>
    /// <returns>钳制后的垂直位置</returns>
    protected virtual float ClampVerticalPosition(float y, float subtitleHeight)
    {
        var referenceHeight = GetReferenceHeight();
        var maxY = Mathf.Max(0f, referenceHeight - Mathf.Max(0f, subtitleHeight));
        return Mathf.Clamp(y, 0f, maxY);
    }

    /// <summary>
    ///     设置文本内容
    /// </summary>
    /// <param name="text">要显示的文本</param>
    /// <param name="speakerName">说话者名称，可为空</param>
    /// <param name="speakerColor">说话者颜色，可为空</param>
    /// <param name="enableSpeakerName">是否启用说话者名称</param>
    private void SetText(string text, string speakerName, string speakerColor,
        bool enableSpeakerName)
    {
        if (TextComponent == null)
            return;

        var displayText = text;
        // 但人名需要被翻译
        if (!string.IsNullOrEmpty(speakerName) && enableSpeakerName)
        {
            TextTranslateManger.GetTranslateText(speakerName, out var translatedSpeakerName);

            displayText = $"<color=#{speakerColor}>{translatedSpeakerName}</color>: {text}";

            // 记录以避免被 XUAT 翻译
            TextTranslateManger.MarkTranslated(displayText);

            LogManager.Debug($"SetText displayText: {displayText}");
        }

        TextComponent.text = displayText;
    }

    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected abstract void CreateSubtitleUI();

    /// <summary>
    ///     应用配置到UI
    /// </summary>
    public virtual void ApplyConfig()
    {
        if (Config == null)
        {
            LogManager.Warning("Subtitle config is null, cannot apply/字幕配置为空，无法应用");
            return;
        }

        // 避免修改垂直位置
        // 将配置中的理想位置钳制到当前分辨率下可见范围
        var verticalPosition =
            ClampVerticalPosition(Config.VerticalPosition, Config.SubtitleHeight);
        Config.CurrentVerticalPosition = verticalPosition;

        // 应用背景配置
        if (BackgroundImageComponents is not null)
        {
            BackgroundImageComponents.color = Config.BackgroundColor;

            BackgroundImageComponents.rectTransform.sizeDelta = new Vector2(
                Config.SubtitleWidth, Config.SubtitleHeight);

            BackgroundImageComponents.rectTransform.anchoredPosition3D = new Vector3(
                Config.HorizontalPosition, verticalPosition, 0f);
        }

        // 应用文本组件配置
        if (TextComponent is not null)
        {
            if (Config.Font is not null)
                TextComponent.font = Config.Font;

            TextComponent.fontSize = Config.FontSize;
            TextComponent.color = Config.TextColor;
            TextComponent.alignment = Config.TextAlignment;

            TextComponent.rectTransform.sizeDelta = new Vector2(
                Config.SubtitleWidth, Config.SubtitleHeight);

            // 注意子组件可以跟随父级移动，因此只需要移动背景即可
            TextComponent.rectTransform.anchoredPosition3D = new Vector3(0f, 0f, 0f);
        }


        // 应用描边配置
        if (OutlineComponents is not null)
        {
            OutlineComponents.enabled = Config.EnableOutline;

            if (Config.EnableOutline)
            {
                OutlineComponents.effectColor = Config.OutlineColor;
                OutlineComponents.effectDistance =
                    new Vector2(Config.OutlineWidth, Config.OutlineWidth);
            }
        }

        LogManager.Debug($"Applied subtitle config to {gameObject.name}");
    }

    /// <summary>
    ///     停止正在进行的动画
    /// </summary>
    protected virtual void StopAnimation()
    {
        if (AnimationCoroutine != null)
        {
            StopCoroutine(AnimationCoroutine);
            AnimationCoroutine = null;
        }

        if (AnimationAutoHideCoroutine != null)
        {
            StopCoroutine(AnimationAutoHideCoroutine);
            AnimationAutoHideCoroutine = null;
        }
    }

    /// <summary>
    ///     设置透明度
    /// </summary>
    /// <param name="alpha">透明度值（0-1）</param>
    protected virtual void SetAlpha(float alpha)
    {
        if (CanvasGroupComponents is null) return;

        CanvasGroupComponents.alpha = Mathf.Clamp01(alpha);
    }

    /// <summary>
    ///     淡入动画
    /// </summary>
    protected virtual IEnumerator FadeIn()
    {
        var duration = Config != null ? Config.FadeInDuration : 0f;
        if (duration <= 0f)
        {
            SetAlpha(1f);
            AnimationCoroutine = null;
            yield break;
        }

        var time = 0f;
        var start = 0f;
        if (CanvasGroupComponents is not null)
            start = Mathf.Clamp01(CanvasGroupComponents.alpha);

        while (time < duration)
        {
            time += Time.deltaTime;
            var t = Mathf.Clamp01(time / duration);
            SetAlpha(Mathf.Lerp(start, 1f, t));
            yield return null;
        }

        SetAlpha(1f);
        AnimationCoroutine = null;
    }

    /// <summary>
    ///     淡出动画协程
    /// </summary>
    protected virtual IEnumerator FadeOut()
    {
        var duration = Config != null ? Config.FadeOutDuration : 0f;
        if (duration <= 0f)
        {
            SetAlpha(0f);
            AnimationCoroutine = null;
            SetActive(false);
            yield break;
        }

        var time = 0f;
        var start = 1f;
        if (CanvasGroupComponents is not null)
            start = Mathf.Clamp01(CanvasGroupComponents.alpha);

        while (time < duration)
        {
            time += Time.deltaTime;
            var t = Mathf.Clamp01(time / duration);
            SetAlpha(Mathf.Lerp(start, 0f, t));
            yield return null;
        }

        SetAlpha(0f);
        AnimationCoroutine = null;
        SetActive(false);
    }

    /// <summary>
    ///     自动隐藏协程
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    protected virtual IEnumerator AutoHide(float delay)
    {
        yield return new WaitForSeconds(delay);
        AnimationAutoHideCoroutine = null;
        HideSubtitle();
    }


    /// <summary>
    ///     获取说话者的专属颜色
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    /// <returns>专属颜色</returns>
    protected virtual Color GetSpeakerColor(string speakerName)
    {
        if (speakerName == null) speakerName = "";

        var color = SubtitleManager.GetSpeakerColor(speakerName, Config.SubtitleType);

        color.a = Config.TextColor.a;

        LogManager.Debug(
            $"Created Color R:{color.r:F2} G:{color.g:F2} B:{color.b:F2} A:{color.a:F2} for {speakerName}");

        return color;
    }

    /// <summary>
    ///     应用每个说话人的自定义字幕颜色（如果该说话人在当前字幕类型下启用了自定义颜色）
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    protected virtual void ApplyCustomSubtitleColors(string speakerName)
    {
        var entry = SubtitleManager.GetSubtitleColorEntry(speakerName, Config.SubtitleType);
        if (entry is not { Enabled: true }) return;

        // 覆盖文本颜色
        if (TextComponent != null &&
            ColorUtility.TryParseHtmlString(entry.TextColor, out var textColor))
        {
            textColor.a = entry.TextOpacity;
            TextComponent.color = textColor;
        }

        // 覆盖背景颜色
        if (BackgroundImageComponents != null &&
            ColorUtility.TryParseHtmlString(entry.BackgroundColor, out var bgColor))
        {
            bgColor.a = entry.BackgroundOpacity;
            BackgroundImageComponents.color = bgColor;
        }

        // 覆盖描边颜色
        if (OutlineComponents != null && Config.EnableOutline &&
            ColorUtility.TryParseHtmlString(entry.OutlineColor, out var outlineColor))
        {
            outlineColor.a = entry.OutlineOpacity;
            OutlineComponents.effectColor = outlineColor;
        }

        LogManager.Debug(
            $"Applied custom subtitle colors for {speakerName} in {Config.SubtitleType} mode");
    }

    /// <summary>
    ///     设置组件的激活状态
    /// </summary>
    /// <param name="active"></param>
    protected virtual void SetActive(bool active)
    {
        gameObject.SetActive(active);
        if (CanvasComponents is not null)
            CanvasComponents.gameObject.SetActive(active);
    }

    /// <summary>
    ///     销毁字幕UI
    /// </summary>
    protected virtual void DestroySubtitleUI()
    {
        try
        {
            if (CanvasComponents != null)
            {
                Destroy(CanvasComponents.gameObject);
                CanvasComponents = null;
            }

            BackgroundImageComponents = null;
            CanvasGroupComponents = null;
            CanvasScalerComponents = null;
            TextComponent = null;
            OutlineComponents = null;
        }
        catch (Exception e)
        {
            LogManager.Warning($"Failed to destroy subtitle UI/销毁字幕UI失败: {e.Message}");
        }
    }
}