﻿# JustAnotherTranslator Subtitle Configuration Guide

This guide is translated by AI.

If you have any questions, please refer to the Chinese version.

This document explains how to set up subtitle styles for the JustAnotherTranslator (JAT) plugin.

Contributions and fixes are welcome.

Repository:  https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin

Please first read the [README](https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin) to learn JAT basics, where the configuration files are located, and how to adjust settings in-game.

# Subtitle Style

Subtitles are implemented with Unity uGUI's Text component, which supports rich text.

See Unity docs: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/StyledText.html

When subtitles are shown, the translated text is passed to the Text component as-is. If you use rich-text tags in the text, the corresponding effects will be applied, though 100% compatibility is not guaranteed.

## Common Style

All the following style options can be configured separately for the four types: `Basic`, `ADV`, `Yotogi`, and `Lyrics`.

- `Font`: The font used by the subtitles. Enter the name of a font installed on your system. If the font is not found, the default Arial will be used.
- `FontSize`: The font size of the subtitles.
- `TextAlignment`: Alignment of the text within the subtitle box. Options include Upper Left, Upper Center, Upper Right, Middle Left, Middle Center, Middle Right, Lower Left, Lower Center, Lower Right.
- `TextColor`: Text color in hexadecimal, e.g., `#FFFFFF` for white.
- `TextOpacity`: Text opacity, from 0 (fully transparent) to 1 (fully opaque).
- `BackgroundColor`: Color of the subtitle background box, in hexadecimal.
- `BackgroundOpacity`: Opacity of the subtitle background box.
- `EnableOutline`: Whether to enable an outline effect for the text.
- `OutlineColor`: Outline color in hexadecimal, e.g., `#000000` for black.
- `OutlineOpacity`: Outline opacity, from 0 (fully transparent) to 1 (fully opaque).
- `OutlineWidth`: Outline width. Unit: pixels (desktop mode). In VR mode it is automatically converted to a world-space size based on font scaling; no manual conversion is needed.
- `EnableAnimation`: Whether to enable fade-in/out animations for subtitles.
- `FadeInDuration`: Duration of the fade-in animation (seconds).
- `FadeOutDuration`: Duration of the fade-out animation (seconds).
- `EnableSpeakerName`: Whether to display the speaker's name in the subtitle.

These configurations also apply in VR.

### About speaker name colors

Generated automatically from the speaker's name. Different names get different colors. The same name always results in the same color.

# Subtitle Position

## Desktop Mode

Imagine a virtual screen (Canvas) covering your monitor, with a reference resolution of 1920x1080 and a 16:9 aspect ratio.

(The reference resolution does not affect visual quality; it is only for sizing and automatically scales with your screen resolution.)

- If your monitor is 16:9, the virtual screen resolution is always the reference resolution 1920x1080.
- If your monitor is ultrawide or otherwise non-16:9, to ensure the subtitles are visible by default, the reference resolution prioritizes matching height.
    - That is, the virtual screen height is fixed at 1080, and the reference width grows or shrinks based on the scale factor. Adjust the subtitle width as needed.
    - For example, if your resolution is 3440×1440:
    - Scale factor = screen height ÷ reference height = 1440 ÷ 1080 = 1.3333
    - Virtual screen width = screen width ÷ scale factor = 3440 ÷ 1.3333 = 2580

Subtitle size and position are relative to the virtual screen.

The subtitle is a rectangle whose pivot is at the bottom-left corner.

- `HorizontalPosition` sets the distance from the rectangle's left edge to the virtual screen's left edge. 0 is the far left. Because the reference width is 1920, the far right is 1920 (for non-16:9 screens it varies; see the previous section).
    - Setting 1920 will place the subtitle off-screen.
- `VerticalPosition` sets the distance from the rectangle's bottom edge to the virtual screen's bottom. 0 is the very bottom. Because the reference height is 1080, the top is 1080.
    - Setting 1080 would theoretically place it off-screen, but JAT clamps the height to `0` through `1080 - subtitle height`, so even if you set it beyond `1080 - subtitle height` it will be clamped to `1080 - subtitle height`.
- `SubtitleHeight` sets the height of the subtitle rectangle relative to the virtual screen resolution. Setting 30 means 30 virtual pixels tall; the actual display scales with the screen size.
- `SubtitleWidth` sets the width of the subtitle rectangle relative to the virtual screen resolution. Setting 1920 means 1920 virtual pixels wide; the actual display scales with the screen size.

## VR Tablet Mode

In VR mode, the original game UI is placed on a virtual tablet.

Imagine a transparent virtual screen overlaid on your view. It is not inside the tablet's screen but can move freely and extend beyond the tablet, while always following the tablet.

The subtitle is a rectangle whose pivot is at the center.

- `HorizontalPosition` sets the subtitle rectangle's center relative to the tablet's center. 0 is the tablet's exact center; larger values move it to the right. For example, setting -0.02 moves the center 0.02 meters to the left of the tablet center.
- `VerticalPosition` sets the subtitle rectangle's center relative to the tablet's center. 0 is the tablet's exact center; larger values move it upward. For example, setting 0.17 moves the center 0.17 meters above the tablet center.
- `SubtitleHeight` sets the height of the subtitle rectangle. 1000 units = 1 meter. For example, setting 10 means 0.01 m (1 cm) tall.
- `SubtitleWidth` sets the width of the subtitle rectangle. 1000 units = 1 meter. For example, setting 500 means 0.5 m wide.
- `TextSizeMultiplier`: Adjusts the subtitle size. Due to VR scaling, an extra multiplier is needed.
- `OutlineScaleFactor`: Adjusts the outline width. Due to VR scaling, an extra multiplier is needed. If ghosting occurs, try increasing this value.
- `PixelPerfect`: Whether to enable pixel snapping to reduce blurring (limited effect in WorldSpace).

## VR Space Subtitle Mode

Imagine a transparent virtual screen always centered in your line of sight.

You can configure that screen's distance from your eyes, vertical offset (degrees), and horizontal offset (degrees).

The subtitle is a rectangle whose pivot is at the center.

- `Distance`: How far the virtual screen is from your eyes, in meters.
- `VerticalOffset`: Vertical angular offset of the virtual screen relative to your eyes; positive values move it downward.
- `HorizontalOffset`: Horizontal angular offset of the virtual screen relative to your eyes; positive values move it to the right.
- `SubtitleHeight` sets the height of the subtitle rectangle. 1000 units = 1 meter. For example, setting 15 means 0.015 m (1.5 cm) tall.
- `SubtitleWidth` sets the width of the subtitle rectangle. 1000 units = 1 meter. For example, setting 510 means 0.51 m wide.
- `TextSizeMultiplier`: Adjusts the subtitle size. Due to VR scaling, an extra multiplier is needed.
- `OutlineScaleFactor`: Adjusts the outline width. Due to VR scaling, an extra multiplier is needed. If you experience ghosting, try increasing this value.
- `PixelPerfect`: Whether to enable pixel snapping to reduce blurring (limited effect in WorldSpace).
- `FollowSmoothness`: Sets how smoothly the space subtitle follows head movements; larger values track faster (e.g., 5 ≈ catches up to ~95% displacement in about 0.6 s).

# Subtitle Types

JAT categorizes subtitles into four types:

- Basic
- ADV
- Yotogi
- Lyrics

## Basic Subtitles

Used in most situations, e.g., the fishing mini-game, shooting mini-game, and dialogues in VR Karaoke mode.

(VR Karaoke mode refers to the mode with a tablet in front of you; it is unrelated to whether you actually use a VR device.)

## ADV Subtitles

Subtitles for daily conversations; usually not shown.

## Yotogi Subtitles

Used during Yotogi (the skill selection scenes).

## Lyrics Subtitles

Used only in official Dance mode and dances in VR Karaoke mode.

# When subtitles appear

Please see the `TranslationGuide`.