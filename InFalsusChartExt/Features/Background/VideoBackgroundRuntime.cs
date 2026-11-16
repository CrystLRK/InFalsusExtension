using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Video;

namespace InFalsusChartExt
{
    /// <summary>
    /// Minimal MP4 background bridge. It keeps one VideoPlayer alive and uses
    /// its RenderTexture as the chart background material texture. The object is
    /// deliberately persistent; recreating video/render objects during chart
    /// transitions is more dangerous than stopping and reusing this one.
    /// </summary>
    internal static class VideoBackgroundRuntime
    {
        private static GameObject _object;
        private static VideoPlayer _player;
        private static RenderTexture _texture;
        private static string _path;
        private static int _logs;
        private static int _errors;

        internal static RenderTexture CurrentTexture => _texture;

        internal static Texture Ensure(string path)
        {
            bool createdResources = false;
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                    return null;
                if (_object == null)
                {
                    _object = new GameObject("IFCE_BackgroundVideo");
                    createdResources = true;
                    UnityEngine.Object.DontDestroyOnLoad(_object);
                    _player = _object.AddComponent<VideoPlayer>();
                    _texture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32)
                    {
                        name = "IFCE_BackgroundVideoTexture",
                        useMipMap = false,
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                    };
                    _texture.Create();
                    _player.source = VideoSource.Url;
                    _player.renderMode = VideoRenderMode.RenderTexture;
                    _player.targetTexture = _texture;
                    _player.playOnAwake = false;
                    _player.isLooping = true;
                    _player.audioOutputMode = VideoAudioOutputMode.None;
                }
                if (!string.Equals(_path, path, StringComparison.OrdinalIgnoreCase))
                {
                    _player.Stop();
                    _player.url = new Uri(path).AbsoluteUri;
                    _player.Prepare();
                    _player.Play();
                    _path = path;
                    if (_logs++ < 8)
                        ChartExtMod.Log.Msg("[ChartExt] background video selected: " + path);
                }
                else if (!_player.isPlaying)
                {
                    _player.Play();
                }
                return _texture;
            }
            catch (Exception e)
            {
                if (createdResources)
                {
                    Dispose();
                }
                else
                {
                    Stop();
                }
                if (_errors++ < 10)
                    ChartExtMod.Log.Error("[ChartExt] background video failed: " + e);
                return null;
            }
        }

        internal static void Stop()
        {
            try
            {
                if (_player != null) _player.Stop();
                _path = null;
            }
            catch { }
        }

        internal static void Dispose()
        {
            Stop();
            try
            {
                if (_object != null)
                    UnityEngine.Object.Destroy(_object);
            }
            catch { }
            try
            {
                if (_texture != null)
                    UnityEngine.Object.Destroy(_texture);
            }
            catch { }
            _object = null;
            _player = null;
            _texture = null;
            _path = null;
        }
    }
}
