using System;
using System.Collections;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     字幕组件的基类，提供共享功能
/// </summary>
public abstract class BaseSubtitleComponent : MonoBehaviour, ISubtitleComponent
{
    // 当前显示的文本
    private string _currentText = "";

    // 翻译后的说话者名称
    private string _translatedSpeakerName = "";

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

    // 当前动画协程
    protected Coroutine CurrentAnimation;

    // 字幕描边组件
    protected Outline OutlineComponents;

    // 说话者文本颜色
    protected string SpeakerColor = "";

    // 说话者名称
    protected string SpeakerName = "";

    // 字幕文本组件
    protected Text TextComponent;

    /// <summary>
    ///     销毁字幕组件
    /// </summary>
    public virtual void OnDestroy()
    {
        SubtitleComponentManager.DestroySubtitleComponent(this);
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
            gameObject.SetActive(false);
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
        // 如果有正在进行的动画，停止它
        if (CurrentAnimation is not null)
        {
            StopCoroutine(CurrentAnimation);
            CurrentAnimation = null;
        }

        if (string.IsNullOrEmpty(SpeakerName))
            if (!string.IsNullOrEmpty(speakerName))
                SpeakerName = speakerName;
            else
                SpeakerName = "";


        if (string.IsNullOrEmpty(SpeakerColor))
            SpeakerColor = ColorUtility.ToHtmlStringRGBA(GetSpeakerColor(speakerName));

        // 设置文本内容
        SetText(text, speakerName, SpeakerColor, Config.EnableSpeakerName);

        // 显示字幕
        gameObject.SetActive(true);

        // 如果启用了动画效果
        if (Config.EnableAnimation)
        {
            // 开始淡入动画
            CurrentAnimation = StartCoroutine(FadeIn());

            // 如果设置了持续时间，则在指定时间后淡出
            if (duration > 0) StartCoroutine(AutoHide(duration));
        }
        else
        {
            // 直接显示
            SetAlpha(Config.TextColor.a);

            // 如果设置了持续时间，则在指定时间后隐藏
            if (duration > 0) StartCoroutine(AutoHide(duration));
        }

        LogManager.Debug(
            $"Showing subtitle: {text}, SpeakerName: {speakerName}, SpeakerColor: {SpeakerColor}, Duration: {duration}");
    }

    /// <summary>
    ///     隐藏字幕
    /// </summary>
    public virtual void HideSubtitle(bool skipAnimation = false)
    {
        if (skipAnimation)
        {
            gameObject.SetActive(false);
            LogManager.Debug($"Hiding subtitle {gameObject.name}");
            return;
        }

        // 如果有正在进行的动画，停止它
        if (CurrentAnimation != null)
        {
            StopCoroutine(CurrentAnimation);
            CurrentAnimation = null;
        }

        // 如果启用了动画效果
        if (Config.EnableAnimation && gameObject.activeSelf)
            CurrentAnimation = StartCoroutine(FadeOut());
        else
            gameObject.SetActive(false);

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
    ///     获取当前说话者名称
    /// </summary>
    /// <returns>当前说话者名称</returns>
    public string GetSpeakerName()
    {
        return SpeakerName ?? string.Empty;
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
            ApplyNewPosition();
            return;
        }

        LogManager.Warning("Subtitle config is null, cannot set vertical position/字幕配置为空，无法设置垂直位置");
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
        var displayText = text;
        // 设置文本，无需处理 XUAT 互操作以及翻译，因为获取时已经被翻译了
        // 但人名需要被翻译
        if (!string.IsNullOrEmpty(speakerName) && enableSpeakerName)
        {
            if (string.IsNullOrEmpty(_translatedSpeakerName))
                TextTranslateManger.GetTranslateText(speakerName, out _translatedSpeakerName);

            displayText = $"<color=#{speakerColor}>{_translatedSpeakerName}</color>: {text}";
        }

        _currentText = text;
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
        var verticalPosition = Config.VerticalPosition;
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

            // 注意可以跟随父级移动，因此只需要移动背景即可
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
    ///     应用新的位置，包括 水平位置，垂直位置，背景高度，背景宽度
    ///     水平位置使用 Config.CurrentVerticalPosition
    /// </summary>
    public virtual void ApplyNewPosition()
    {
        if (Config == null)
        {
            LogManager.Warning("Subtitle config is null, cannot apply/字幕配置为空，无法应用");
            return;
        }

        // 应用背景配置
        if (BackgroundImageComponents is not null)
        {
            BackgroundImageComponents.rectTransform.sizeDelta = new Vector2(
                Config.SubtitleWidth, Config.SubtitleHeight);

            BackgroundImageComponents.rectTransform.anchoredPosition3D = new Vector3(
                Config.HorizontalPosition, Config.CurrentVerticalPosition, 0f);
        }

        // 应用文本组件配置
        if (TextComponent is not null)
            TextComponent.rectTransform.sizeDelta = new Vector2(
                Config.SubtitleWidth, Config.SubtitleHeight);
        // 注意可以跟随父级移动，因此只需要移动背景即可
        // TextComponent.rectTransform.anchoredPosition3D = new Vector3(
        //     Config.HorizontalPosition, Config.CurrentVerticalPosition, 0f);
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
    }

    /// <summary>
    ///     设置透明度
    /// </summary>
    /// <param name="alpha">透明度值（0-1）</param>
    protected virtual void SetAlpha(float alpha)
    {
        if (TextComponent is null) return;

        //设置主文本组件的基础透明度
        var textColor = TextComponent.color;
        textColor.a = Mathf.Clamp01(alpha);
        TextComponent.color = textColor;

        // 设置背景透明度
        if (BackgroundImageComponents is not null)
        {
            var bgColor = BackgroundImageComponents.color;
            // Config.BackgroundColor.a 作为背景的最大透明度
            bgColor.a = Mathf.Clamp01(alpha) * Config.BackgroundColor.a;
            BackgroundImageComponents.color = bgColor;
        }

        // TextComponent.color 无法影响 html 标签，因此需要单独处理
        if (Config.EnableSpeakerName && !string.IsNullOrEmpty(SpeakerColor) &&
            !string.IsNullOrEmpty(SpeakerName))
        {
            // 将alpha (0-1) 转换为两位十六进制字符串 (00-FF)
            var alphaByte = (byte)(Mathf.Clamp01(alpha) * 255f);
            var alphaHex = alphaByte.ToString("X2");

            // SpeakerColor 存储的是 RRGGBBAA, 替换最后两位
            if (SpeakerColor.Length < 8) SpeakerColor += "FF";

            SpeakerColor = SpeakerColor.Substring(0, SpeakerColor.Length - 2) + alphaHex;


            if (string.IsNullOrEmpty(_translatedSpeakerName))
                TextTranslateManger.GetTranslateText(SpeakerName, out _translatedSpeakerName);

            // 重构文本
            TextComponent.text =
                $"<color=#{SpeakerColor}>{_translatedSpeakerName}</color>: {_currentText}";
        }
    }

    /// <summary>
    ///     淡入动画
    /// </summary>
    protected virtual IEnumerator FadeIn()
    {
        float time = 0;
        SetAlpha(0);

        while (time < Config.FadeInDuration)
        {
            time += Time.deltaTime;
            var alpha = Mathf.Clamp01(time / Config.FadeInDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(Config.TextColor.a);
        CurrentAnimation = null;
    }

    /// <summary>
    ///     淡出动画协程
    /// </summary>
    protected virtual IEnumerator FadeOut()
    {
        float time = 0;
        SetAlpha(Config.TextColor.a);

        while (time < Config.FadeOutDuration)
        {
            time += Time.deltaTime;
            var alpha = 1 - Mathf.Clamp01(time / Config.FadeOutDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(0);
        gameObject.SetActive(false);
        CurrentAnimation = null;
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
        LogManager.Debug($"Creating Color for {speakerName}");

        if (speakerName == null) speakerName = "";

        // 使用哈希值生成颜色，确保相同名称总是获得相同颜色
        var random = new Random(speakerName.GetHashCode());

        // 生成偏亮的颜色
        var color = new Color(
            0.5f + (float)random.NextDouble() * 0.5f, // 0.5-1.0 范围
            0.5f + (float)random.NextDouble() * 0.5f,
            0.5f + (float)random.NextDouble() * 0.5f
        );

        color.a = Config.TextColor.a;

        LogManager.Debug(
            $"Created Color R:{color.r:F2} G:{color.g:F2} B:{color.b:F2} A:{color.a:F2} for {speakerName}");

        return color;
    }


    /// <summary>
    ///     销毁字幕UI
    /// </summary>
    protected virtual void DestroySubtitleUI()
    {
        if (CanvasComponents != null)
        {
            Destroy(CanvasComponents.gameObject);
            CanvasComponents = null;
        }
    }
}