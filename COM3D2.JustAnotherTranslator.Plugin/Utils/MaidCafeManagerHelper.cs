using System.Reflection;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

// <summary>
//     MaidCafeManager的辅助类
//     用于安全检查 MaidCafeManager 是否可用
//     这样就不需要限定游戏版本 2.42 以上了
// </summary>
public static class MaidCafeManagerHelper
{
    private static bool? _isMaidCafeAvailable;
    private static PropertyInfo _isStreamingPartProperty;

    // 检查MaidCafeManager是否可用
    private static bool IsMaidCafeAvailable()
    {
        if (!_isMaidCafeAvailable.HasValue)
            try
            {
                var type = AccessTools.TypeByName("MaidCafe.MaidCafeManager");
                _isMaidCafeAvailable = type != null;
                if (_isMaidCafeAvailable.Value)
                    _isStreamingPartProperty = AccessTools.Property(type, "isStreamingPart");
            }
            catch
            {
                _isMaidCafeAvailable = false;
            }

        return _isMaidCafeAvailable.Value;
    }

    // 安全获取isStreamingPart的值
    public static bool IsStreamingPart()
    {
        if (!IsMaidCafeAvailable() || _isStreamingPartProperty == null)
            return false;

        try
        {
            return (bool)_isStreamingPartProperty.GetValue(null, null);
        }
        catch
        {
            return false;
        }
    }
}