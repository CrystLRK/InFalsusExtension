using System;
using UnityEngine;
using UnityEngine.UI;

namespace InFalsusChartExt
{
    /// <summary>
    /// Minimal runtime Canvas UI used above the game's own UI. IMGUI can be
    /// covered by the game's ScreenSpaceOverlay canvases in this build.
    /// </summary>
    internal static class RuntimeCanvasUi
    {
        internal static Canvas CreateCanvas(
            string name,
            int sortingOrder)
        {
            var root = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform rect = EnsureRect(root);
            rect.sizeDelta = new Vector2(
                Math.Max(1, Screen.width),
                Math.Max(1, Screen.height));
            rect.localScale = Vector3.one;
            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = false;
            return canvas;
        }

        internal static RectTransform CreatePanel(
            Transform parent,
            Vector2 size,
            Vector2 position,
            Color color,
            Vector2? anchor = null,
            Vector2? pivot = null)
        {
            var go = new GameObject("Panel");
            var image = go.AddComponent<Image>();
            var rect = EnsureRect(go);
            rect.SetParent(parent, false);
            Vector2 at = anchor ?? new Vector2(0f, 1f);
            rect.anchorMin = at;
            rect.anchorMax = at;
            rect.pivot = pivot ?? at;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            image.color = color;
            image.raycastTarget = true;
            return rect;
        }

        internal static RectTransform CreateFullScreenPanel(
            Transform parent,
            Color color)
        {
            var go = new GameObject("FullScreen");
            var image = go.AddComponent<Image>();
            var rect = EnsureRect(go);
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            image.color = color;
            image.raycastTarget = true;
            return rect;
        }

        internal static Text CreateCenteredText(
            Transform parent,
            string value,
            int fontSize,
            Vector2 size,
            float y,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Text");
            var text = go.AddComponent<Text>();
            var rect = EnsureRect(go);
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0f, y);
            text.font = LoadFont(fontSize);
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = value ?? string.Empty;
            text.raycastTarget = false;
            return text;
        }

        internal static Text CreateText(
            Transform parent,
            string value,
            int fontSize,
            Vector2 size,
            Vector2 position,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Text");
            var text = go.AddComponent<Text>();
            var rect = EnsureRect(go);
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            text.font = LoadFont(fontSize);
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = value ?? string.Empty;
            text.raycastTarget = false;
            return text;
        }

        internal static CanvasButton CreateButton(
            Transform parent,
            string label,
            Vector2 size,
            Vector2 position,
            Action onClick)
        {
            var go = new GameObject("Button");
            var image = go.AddComponent<Image>();
            var rect = EnsureRect(go);
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            image.color = new Color(
                0.16f, 0.16f, 0.16f, 0.96f);

            Text text = CreateText(
                go.transform,
                label,
                16,
                size,
                Vector2.zero,
                TextAnchor.MiddleCenter);
            RectTransform textRect =
                text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            return new CanvasButton
            {
                Rect = rect,
                Label = text,
                OnClick = onClick,
            };
        }

        private static Font LoadFont(int size)
        {
            try
            {
                Font font = Font.CreateDynamicFontFromOSFont(
                    "Arial", size);
                if (font is not null)
                    return font;
            }
            catch
            {
            }
            foreach (string name in new[]
                     {
                         "LegacyRuntime.ttf",
                         "Arial.ttf",
                     })
            {
                try
                {
                    Font font =
                        Resources.GetBuiltinResource<Font>(name);
                    if (font is not null)
                        return font;
                }
                catch
                {
                }
            }
            return null;
        }

        private static RectTransform EnsureRect(GameObject go)
        {
            RectTransform rect =
                go.GetComponent<RectTransform>();
            if (rect is null)
                rect = go.AddComponent<RectTransform>();
            return rect;
        }

        internal sealed class CanvasButton
        {
            internal RectTransform Rect;
            internal Text Label;
            internal Action OnClick;
        }
    }
}
