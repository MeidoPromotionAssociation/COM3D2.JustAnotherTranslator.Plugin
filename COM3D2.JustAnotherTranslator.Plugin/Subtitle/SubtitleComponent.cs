using System;
using System.Collections;
using COM3D2.JustAnotherTranslator.Plugin.Subtitle.ConfigStrategy;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     通用字幕组件，用于在游戏中显示可配置的字幕
/// </summary>
public class SubtitleComponent : MonoBehaviour
{
    // 动画相关
    internal Coroutine _animationCoroutine;
    internal Image _backgroundImage;
    internal RectTransform _backgroundRect;

    // UI组件
    internal Canvas _canvas;
    internal CanvasGroup _canvasGroup;

    internal CanvasScaler _canvasScaler;

    // 配置相关
    internal SubtitleConfig _config;
    internal ISubtitleConfigStrategy _configStrategy;
    internal float _followSmoothness = 5f;
    internal Outline _outline;
    internal string _speakerName;
    internal Text _text;
    internal RectTransform _textRect;

    // VR特有组件
    internal Transform _vrHeadTransform;
    internal bool _VRinitialized;
    internal Transform _vrSubtitleContainer;

    /// <summary>
    ///     每帧更新，用于VR模式下跟随头部运动
    /// </summary>
    private void Update()
    {
        if (!JustAnotherTranslator.IsVrMode ||
            _config.VRSubtitleMode != JustAnotherTranslator.VRSubtitleModeEnum.InSpace)
            return;

        if (_vrHeadTransform is null || !_VRinitialized || !gameObject.activeSelf)
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
    private void Init()
    {
        _configStrategy = SubtitleConfigStrategyFactory.CreateStrategy(_config);
        _configStrategy.CreateSubtitleUI(this);
        ApplyConfig();

        // 如果在VR模式下，初始化VR组件
        if (JustAnotherTranslator.IsVrMode)
            InitVRComponents();
        else
            _VRinitialized = true;
    }

    /// <summary>
    ///     初始化VR组件
    /// </summary>
    private void InitVRComponents()
    {
        // 查找 OvrMgr 的 EyeAnchor
        if (GameMain.Instance is not null && GameMain.Instance.OvrMgr is not null)
        {
            _vrHeadTransform = GameMain.Instance.OvrMgr.EyeAnchor;
            if (_vrHeadTransform is not null)
            {
                LogManager.Debug("VR head transform (EyeAnchor) found, subtitle head tracking enabled");
                _VRinitialized = true;
                return;
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
                _VRinitialized = true;
            }
            else
            {
                LogManager.Warning(
                    "VR head transform not found, head tracking will not work/找不到VR头部变换，头部字幕跟踪将无法工作");
            }
        }
    }

    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    public void Initialize(SubtitleConfig config)
    {
        _config = config ?? new SubtitleConfig();
        Init();
    }

    /// <summary>
    ///     应用字幕配置
    /// </summary>
    public void ApplyConfig()
    {
        if (_configStrategy != null) _configStrategy.ApplyConfig(this);
    }

    /// <summary>
    ///     更新字幕配置
    /// </summary>
    /// <param name="config">新的配置</param>
    public void UpdateConfig(SubtitleConfig config)
    {
        if (config is null)
        {
            LogManager.Warning("Subtitle config is null, cannot apply/字幕配置为空，无法应用");
            return;
        }

        var currentStrategy = _configStrategy;
        var newStrategy = SubtitleConfigStrategyFactory.CreateStrategy(config);

        // 如果策略类型改变，需要重建 UI
        if (currentStrategy.GetType() != newStrategy.GetType())
        {
            // 先保存当前字幕内容
            var currentText = _text?.text ?? string.Empty;
            var currentSpeakerName = _speakerName ?? string.Empty;

            // 销毁当前 UI
            DestroySubtitleUI();

            // 更新配置和策略
            _config = config;
            _configStrategy = newStrategy;

            // 创建新 UI
            _configStrategy.CreateSubtitleUI(this);
            ApplyConfig();

            // 如果有字幕内容，重新显示
            if (!string.IsNullOrEmpty(currentText))
                ShowSubtitle(currentText, currentSpeakerName, 0f);
        }
        else
        {
            // 策略类型相同，只需更新配置
            _configStrategy.UpdateConfig(this, config);
        }
    }

    /// <summary>
    ///     显示字幕
    /// </summary>
    public void ShowSubtitle(string text, string speakerName, float duration)
    {
        if (_configStrategy != null)
            _configStrategy.HandleShowSubtitle(this, text, speakerName, duration);
    }

    /// <summary>
    ///     隐藏字幕
    /// </summary>
    public void HideSubtitle()
    {
        if (_configStrategy != null)
            _configStrategy.HandleHideSubtitle(this);
    }

    /// <summary>
    ///     销毁字幕UI组件
    /// </summary>
    private void DestroySubtitleUI()
    {
        // 停止动画
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        // 清理子对象
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // 重置引用
        _canvas = null;
        _canvasScaler = null;
        _canvasGroup = null;
        _backgroundImage = null;
        _backgroundRect = null;
        _text = null;
        _textRect = null;
        _outline = null;
    }

    /// <summary>
    ///     渐变画布透明度
    /// </summary>
    internal IEnumerator FadeCanvasAlpha(float startAlpha, float endAlpha, float duration, Action onComplete = null)
    {
        if (_canvasGroup is null || duration <= 0)
        {
            if (_canvasGroup is not null)
                _canvasGroup.alpha = endAlpha;

            onComplete?.Invoke();
            yield break;
        }

        _canvasGroup.alpha = startAlpha;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _canvasGroup.alpha = endAlpha;
        onComplete?.Invoke();
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
        if (_text is not null && !string.IsNullOrEmpty(_text.text))
        {
            // 获取文本内容的预计高度
            var textGenerator = _text.cachedTextGenerator;
            if (textGenerator.characterCountVisible == 0)
                _text.cachedTextGenerator.Populate(_text.text,
                    _text.GetGenerationSettings(_text.rectTransform.rect.size));

            // 计算实际行数与高度
            float textHeight = textGenerator.lineCount * _text.fontSize;

            // 返回背景高度和文本实际高度中的最大值
            return Mathf.Max(_config.BackgroundHeight, textHeight + 10); // 加上上下边距
        }

        return _config.BackgroundHeight;
    }
}