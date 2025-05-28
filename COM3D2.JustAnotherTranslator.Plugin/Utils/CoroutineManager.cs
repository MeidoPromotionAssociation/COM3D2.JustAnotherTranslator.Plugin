using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

/// <summary>
///     协程管理器，用于在非MonoBehaviour类中启动协程
///     使用方法：
///     1. 调用CoroutineManager.LaunchCoroutine(IEnumerator coroutine)启动协程
///     2. 调用CoroutineManager.StopCoroutine(string id)停止指定标识符的协程
///     3. 调用CoroutineManager.StopAllCoroutines()停止所有正在运行的协程
///     注意：
///     1. 协程运行器组件会在第一次启动协程时自动创建
///     2. 协程ID是唯一的，用于停止指定的协程
/// </summary>
public static class CoroutineManager
{
    // 单例GameObject，用于运行所有协程
    private static GameObject _instance;

    // 协程运行器组件
    private static CoroutineRunner _runner;

    // 初始化
    private static void Initialize()
    {
        if (_instance is null)
        {
            _instance = new GameObject("JATCoroutineManager");
            Object.DontDestroyOnLoad(_instance);
            _runner = _instance.AddComponent<CoroutineRunner>();
        }
    }

    /// <summary>
    ///     启动一个协程并自动在协程完成后销毁
    /// </summary>
    /// <param name="coroutine">要运行的协程</param>
    /// <returns>协程的唯一标识符</returns>
    public static string LaunchCoroutine(IEnumerator coroutine)
    {
        Initialize();
        return _runner.RunCoroutine(coroutine);
    }

    /// <summary>
    ///     停止指定标识符的协程
    /// </summary>
    /// <param name="id">协程的唯一标识符</param>
    public static void StopCoroutine(string id)
    {
        if (_runner is not null && !string.IsNullOrEmpty(id)) _runner.StopCoroutineById(id);
    }

    /// <summary>
    ///     停止所有正在运行的协程
    /// </summary>
    public static void StopAllCoroutines()
    {
        if (_runner is not null) _runner.StopAllCoroutines();
    }

    /// <summary>
    ///     协程运行器组件
    /// </summary>
    private class CoroutineRunner : MonoBehaviour
    {
        // 存储协程ID和Coroutine的映射关系
        private readonly Dictionary<string, Coroutine> _runningCoroutines = new();

        /// <summary>
        ///     运行协程并返回唯一标识符
        /// </summary>
        public string RunCoroutine(IEnumerator coroutine)
        {
            var id = Guid.NewGuid().ToString();
            var handle = StartCoroutine(WrapCoroutine(coroutine, id));
            _runningCoroutines[id] = handle;
            return id;
        }

        /// <summary>
        ///     根据ID停止协程
        /// </summary>
        public void StopCoroutineById(string id)
        {
            if (_runningCoroutines.TryGetValue(id, out var handle))
            {
                StopCoroutine(handle);
                _runningCoroutines.Remove(id);
            }
        }

        /// <summary>
        ///     停止所有协程
        /// </summary>
        public new void StopAllCoroutines()
        {
            base.StopAllCoroutines();
            _runningCoroutines.Clear();
        }

        /// <summary>
        ///     包装协程，使其在完成后自动从字典中移除
        /// </summary>
        private IEnumerator WrapCoroutine(IEnumerator coroutine, string id)
        {
            yield return StartCoroutine(coroutine);

            // 协程完成后，从字典中移除
            if (_runningCoroutines.ContainsKey(id)) _runningCoroutines.Remove(id);
        }
    }
}