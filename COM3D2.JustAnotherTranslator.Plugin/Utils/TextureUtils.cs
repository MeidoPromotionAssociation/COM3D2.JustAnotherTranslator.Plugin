using System;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

public static class TextureUtils
{
    /// <summary>
    ///     Gets a readable Texture2D from a potentially non-readable one.
    /// </summary>
    /// <param name="texture">The texture to make readable.</param>
    /// <returns>
    ///     A readable Texture2D. If the original was already readable, it's returned directly. Otherwise, a new,
    ///     temporary texture is created and returned. The caller is responsible for destroying the temporary texture.
    /// </returns>
    public static Texture2D GetReadableTexture(Texture2D texture)
    {
#if COM3D2_5_UNITY_2022
            if (texture.isReadable)
            {
                return texture;
            }
#else
        try
        {
            // If this succeeds, texture is readable.
            texture.GetPixel(0, 0);
            return texture;
        }
        catch (Exception)
        {
            // Texture is not readable, proceed to copy.
        }
#endif

        // Fallback for non-readable textures
        try
        {
            var tmp = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            // Blit the texture to the temporary render texture
            Graphics.Blit(texture, tmp);
            var previous = RenderTexture.active;
            RenderTexture.active = tmp;

            // Read the temporary render texture into a new readable texture
            var myTexture2D = new Texture2D(texture.width, texture.height);
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();

            // Cleanup
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            return myTexture2D;
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to create readable texture copy/创建可读纹理副本失败: {e.Message}");
            return null;
        }
    }
}