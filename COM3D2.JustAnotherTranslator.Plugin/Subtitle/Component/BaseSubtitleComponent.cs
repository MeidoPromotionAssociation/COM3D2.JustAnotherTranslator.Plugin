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
    protected Coroutine AnimationCoroutine;

    // 字幕背景图像组件
    protected Image BackgroundImage;

    // 字幕画布组件
    protected Canvas Canvas;

    // 字幕画布缩放器组件
    protected CanvasScaler CanvasScaler;

    // 字幕配置
    protected SubtitleConfig Config;

    // 当前动画协程
    protected Coroutine CurrentAnimation;

    // 字幕描边组件
    protected Outline Outline;

    // 说话者文本颜色
    protected string SpeakerColor;

    // 说话者名称
    protected string SpeakerName;

    // 字幕文本组件
    protected Text TextComponent;

    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    public virtual void Init(SubtitleConfig config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));

        // 创建UI
        CreateSubtitleUI();

        // 应用配置
        ApplyConfig();

        gameObject.SetActive(false);
        LogManager.Debug($"{gameObject.name} initialized");
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
            SpeakerName = speakerName;

        if (string.IsNullOrEmpty(SpeakerColor))
            SpeakerColor = ColorUtility.ToHtmlStringRGB(GetSpeakerColor(speakerName));

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
            SetAlpha(1);

            // 如果设置了持续时间，则在指定时间后隐藏
            if (duration > 0) StartCoroutine(AutoHide(duration));
        }

        LogManager.Debug($"Showing subtitle: {text}");
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
            LogManager.Warning("字幕配置为空，无法更新/Subtitle config is null, cannot update");
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

        TextComponent.text = displayText;
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
        // 检查配置是否为空
        if (Config == null)
        {
            LogManager.Warning("Subtitle config is null, cannot apply/字幕配置为空，无法应用");
            return;
        }

        // 应用文本组件配置
        if (TextComponent is not null)
        {
            // 设置字体
            if (Config.Font is not null)
                TextComponent.font = Config.Font;

            TextComponent.fontSize = Config.FontSize;
            TextComponent.color = Config.TextColor;
            TextComponent.alignment = Config.TextAlignment;
        }

        // 应用背景配置
        if (BackgroundImage is not null)
        {
            // 设置背景颜色和透明度
            BackgroundImage.color = Config.BackgroundColor;

            // 设置背景大小
            var backgroundRect = BackgroundImage.rectTransform;

            // 计算高度 - 如果BackgroundHeight <= 1则作为百分比处理，否则作为绝对像素值
            float height;
            if (Config.BackgroundHeight <= 1.0f)
                height = Screen.height * Config.BackgroundHeight;
            else
                height = Config.BackgroundHeight;

            // 宽度设置 - 如果BackgroundWidth <= 1则作为百分比处理，否则作为绝对像素值
            var width = Config.BackgroundWidth;
            if (width <= 1.0f)
            {
                // 作为屏幕宽度的百分比，保持原来的锚点设置
                backgroundRect.anchorMin = new Vector2((1 - width) / 2, Config.VerticalPosition);
                backgroundRect.anchorMax = new Vector2((1 + width) / 2, Config.VerticalPosition);
                backgroundRect.sizeDelta = new Vector2(0, height);
            }
            else
            {
                // 绝对像素宽度，需要调整锚点为中心
                backgroundRect.anchorMin = new Vector2(0.5f, Config.VerticalPosition);
                backgroundRect.anchorMax = new Vector2(0.5f, Config.VerticalPosition);
                backgroundRect.sizeDelta = new Vector2(width, height);
            }
        }

        // 应用描边配置
        if (Outline is not null)
        {
            // 设置描边是否启用
            Outline.enabled = Config.EnableOutline;

            // 如果启用，设置描边颜色和宽度
            if (Config.EnableOutline)
            {
                Outline.effectColor = Config.OutlineColor;
                Outline.effectDistance = new Vector2(Config.OutlineWidth, Config.OutlineWidth);
            }
        }

        // 应用画布配置
        if (Canvas is not null)
            // 根据字幕类型设置画布属性
            if (Config.SubtitleType == JustAnotherTranslator.SubtitleTypeEnum.Base)
            {
                // 设置垂直位置
                var rectTransform = Canvas.GetComponent<RectTransform>();
                if (rectTransform is not null)
                {
                    // 0是底部，1是顶部，调整字幕的垂直位置
                    rectTransform.anchorMin = new Vector2(0.5f, Config.VerticalPosition);
                    rectTransform.anchorMax = new Vector2(0.5f, Config.VerticalPosition);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                }
            }

        // 应用画布缩放器配置
        if (CanvasScaler is not null)
        {
            // 设置UI缩放模式和参考分辨率
            CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            CanvasScaler.referenceResolution = new Vector2(1920, 1080);
            CanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            CanvasScaler.matchWidthOrHeight = 0.5f; // 0是宽度，1是高度，0.5是两者平衡
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
    }


    /// <summary>
    ///     销毁字幕UI
    /// </summary>
    protected virtual void DestroySubtitleUI()
    {
        if (Canvas != null)
        {
            Destroy(Canvas.gameObject);
            Canvas = null;
        }
    }

    /// <summary>
    ///     设置透明度
    /// </summary>
    /// <param name="alpha">透明度值（0-1）</param>
    protected virtual void SetAlpha(float alpha)
    {
        // 设置文本透明度
        var textColor = TextComponent.color;
        textColor.a = alpha;
        TextComponent.color = textColor;

        // 如果文本中包含带颜色的说话者名称，需要更新富文本中的颜色
        if (TextComponent.text.Contains("<color="))
        {
            // 获取当前文本内容
            var currentText = TextComponent.text;

            // 查找颜色标签的开始位置
            var colorTagStart = currentText.IndexOf("<color=", StringComparison.Ordinal);
            if (colorTagStart >= 0)
            {
                // 查找颜色值的开始和结束位置
                var colorValueStart = currentText.IndexOf('#', colorTagStart);
                var colorValueEnd = currentText.IndexOf('>', colorValueStart);

                if (colorValueStart >= 0 && colorValueEnd > colorValueStart)
                {
                    // 提取原始颜色值
                    var originalColorValue =
                        currentText.Substring(colorValueStart + 1, colorValueEnd - colorValueStart - 1);

                    // 如果颜色值是6位的RGB格式，转换为带透明度的RGBA格式
                    if (originalColorValue.Length == 6)
                    {
                        // 计算alpha值的十六进制表示（00-FF）
                        var alphaValue = (byte)(alpha * 255);
                        var alphaHex = alphaValue.ToString("X2");

                        // 创建新的颜色值，加入alpha通道
                        var newColorValue = originalColorValue + alphaHex;

                        // 替换原来的颜色值
                        currentText = currentText.Substring(0, colorValueStart + 1) + newColorValue +
                                      currentText.Substring(colorValueEnd);

                        // 更新文本内容
                        TextComponent.text = currentText;
                    }
                }
            }
        }

        // 设置背景透明度
        var bgColor = BackgroundImage.color;
        bgColor.a = alpha * Config.BackgroundColor.a; // 保持背景的相对透明度
        BackgroundImage.color = bgColor;
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

        SetAlpha(1);
        CurrentAnimation = null;
    }

    /// <summary>
    ///     淡出动画协程
    /// </summary>
    protected virtual IEnumerator FadeOut()
    {
        float time = 0;
        SetAlpha(1);

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