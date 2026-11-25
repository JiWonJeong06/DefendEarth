using System;
using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 오브젝트 풀. 탄·몬스터·효과를 매번 만들고 지우지 않고 꺼 두었다가 다시 쓴다(저사양 폰 대비).
    /// </summary>
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly Func<T> create;
        private readonly Stack<T> free = new Stack<T>();

        public ComponentPool(Func<T> create, int prewarm = 0)
        {
            this.create = create;
            for (int i = 0; i < prewarm; i++)
            {
                var item = create();
                item.gameObject.SetActive(false);
                free.Push(item);
            }
        }

        public T Get()
        {
            var item = free.Count > 0 ? free.Pop() : create();
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null || !item.gameObject.activeSelf) return;
            item.gameObject.SetActive(false);
            free.Push(item);
        }
    }
}
