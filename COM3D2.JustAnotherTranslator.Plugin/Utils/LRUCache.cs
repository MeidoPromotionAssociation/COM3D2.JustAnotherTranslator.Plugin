using System;
using System.Collections.Generic;
using System.Linq;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

/// <summary>
///     LRU缓存实现，使用LinkedList和Dictionary实现O(1)的访问和更新
/// </summary>
/// <typeparam name="TKey">键类型</typeparam>
/// <typeparam name="TValue">值类型</typeparam>
public class LRUCache<TKey, TValue>
{
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly int _capacity;
    private readonly LinkedList<CacheItem> _lruList;

    /// <summary>
    ///     创建一个新的LRU缓存
    /// </summary>
    /// <param name="capacity">缓存容量</param>
    public LRUCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive", nameof(capacity));

        _capacity = capacity;
        _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
        _lruList = new LinkedList<CacheItem>();
    }

    /// <summary>
    ///     获取当前缓存中的项数
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    ///     获取缓存中的值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <returns>如果缓存中存在该键，则返回true，否则返回false</returns>
    public bool TryGet(TKey key, out TValue value)
    {
        value = default;

        if (_cache.TryGetValue(key, out var node))
        {
            // 将节点移动到链表末尾，表示最近使用
            _lruList.Remove(node);
            _lruList.AddLast(node);

            value = node.Value.Value;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     添加或更新缓存中的值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void Set(TKey key, TValue value)
    {
        if (_cache.TryGetValue(key, out var existingNode))
        {
            // 更新现有节点的值并移动到链表末尾
            _lruList.Remove(existingNode);
            existingNode.Value.Value = value;
            _lruList.AddLast(existingNode);
        }
        else
        {
            // 如果缓存已满，移除最久未使用的项（链表头部）
            if (_cache.Count >= _capacity)
            {
                var oldest = _lruList.First;
                _lruList.RemoveFirst();
                _cache.Remove(oldest.Value.Key);
            }

            // 添加新节点到链表末尾
            var cacheItem = new CacheItem(key, value);
            var newNode = _lruList.AddLast(cacheItem);
            _cache.Add(key, newNode);
        }
    }

    /// <summary>
    ///     清空缓存
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        _lruList.Clear();
    }

    /// <summary>
    ///     获取缓存中的所有键
    /// </summary>
    /// <returns>缓存中所有键的集合</returns>
    public IEnumerable<TKey> GetAllKeys()
    {
        return _cache.Keys;
    }

    /// <summary>
    ///     获取缓存中的所有值
    /// </summary>
    /// <returns>缓存中所有值的集合</returns>
    public IEnumerable<TValue> GetAllValues()
    {
        return _cache.Values.Select(node => node.Value.Value);
    }

    /// <summary>
    ///     移除指定键的缓存项
    /// </summary>
    /// <param name="key">键</param>
    public void Remove(TKey key)
    {
        if (_cache.TryGetValue(key, out var node))
        {
            _lruList.Remove(node);
            _cache.Remove(key);
        }
    }

    /// <summary>
    ///     缓存项，包含键和值
    /// </summary>
    private class CacheItem
    {
        public CacheItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public TKey Key { get; }
        public TValue Value { get; set; }
    }
}