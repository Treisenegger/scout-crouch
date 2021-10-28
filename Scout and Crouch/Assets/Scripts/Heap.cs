using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Heap<T> where T : IHeapItem<T> {
    
    T[] items;
    int currentSize;

    public int Count {
        get {
            return currentSize;
        }
    }

    public Heap(int _maxSize) {
        items = new T[_maxSize];
        currentSize = 0;
    }

    // Adds a new item to the heap
    public void Add(T _item) {
        items[currentSize] = _item;
        _item.HeapIndex = currentSize;
        currentSize++;
        SortUp(_item);
    }

    // Returns and removes the item with the lowest priority, and reorders the heap
    public T Pop() {
        T _minItem = items[0];
        currentSize--;
        items[0] = items[currentSize];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return _minItem;
    }

    // Return true if the heap contains certain item
    public bool Contains(T _item) {
        return Equals(_item, items[_item.HeapIndex]);
    }

    // Relocate item when its priority was updated (this only works upwards because A* only lowers its items' priorities)
    public void UpdatePriority(T _item) {
        SortUp(_item);
    }

    // Relocate item upwards in the heap when it has a lower priority than its parent
    private void SortUp(T _item) {
        T _parent = items[(_item.HeapIndex - 1) / 2];

        while (true) {
            if (_item.CompareTo(_parent) > 0) {
                Swap(_item, _parent);
            }
            else {
                return;
            }
            _parent = items[(_item.HeapIndex - 1) / 2];
        }
    }

    // Relocate item downwards in the heap when it has a higher priority than one of its children
    private void SortDown(T _item) {
        while (true) {
            int _childAIndex = 2 * _item.HeapIndex + 1;
            int _childBIndex = 2 * _item.HeapIndex + 2;
            int _minChildIndex = _childAIndex;

            if (_childAIndex < currentSize) {
                if (_childBIndex < currentSize && items[_childAIndex].CompareTo(items[_childBIndex]) < 0) {
                    _minChildIndex = _childBIndex;
                }
                
                if (_item.CompareTo(items[_minChildIndex]) < 0) {
                    Swap(_item, items[_minChildIndex]);
                }
                else {
                    return;
                }
            }
            else {
                return;
            }
        }
    }

    // Swap two items' positions in the heap
    private void Swap(T _itemA, T _itemB) {
        int _itemAIndex = _itemA.HeapIndex;
        _itemA.HeapIndex = _itemB.HeapIndex;
        _itemB.HeapIndex = _itemAIndex;

        items[_itemA.HeapIndex] = _itemA;
        items[_itemB.HeapIndex] = _itemB;
    }
}


public interface IHeapItem<T> : IComparable<T> {
    public int HeapIndex { get; set; }
}