﻿using AiurStore.Abstracts;
using AiurStore.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AiurStore.Models
{
    public abstract class InOutDatabase<T> : IEnumerable<T>, IAfterable<T>
    {
        public IStoreProvider Provider { get; set; }
        private object _obj = new object();
        public InOutDatabase()
        {
            var options = new InOutDbOptions();
            this.OnConfiguring(options);
            Provider = options.Provider;
        }

        protected abstract void OnConfiguring(InOutDbOptions options);

        public void Add(T newObject)
        {
            lock (_obj)
            {
                Provider.Add(JsonTools.Serialize(newObject));
            }
        }

        public void Clear()
        {
            lock (_obj)
            {
                Provider.Clear();
            }
        }

        public void Insert(int index, T newObject)
        {
            lock (_obj)
            {
                Provider.Insert(index, JsonTools.Serialize(newObject));
            }
        }

        public void InsertAfter(Func<T, bool> predicate, T newObject)
        {
            if (GetInsertIndex(predicate, out int index))
            {
                Insert(index, newObject);
            }
        }

        public IEnumerable<T> After(Func<T, bool> func)
        {
            lock (_obj)
            {
                var yielding = false;
                foreach (var item in this)
                {
                    if (yielding)
                    {
                        yield return item;
                    }
                    if (func(item))
                    {
                        yielding = true;
                    }
                }
            }
        }

        private bool GetInsertIndex(Func<T, bool> predicate, out int index)
        {
            index = 1;
            foreach (var item in this)
            {
                if (predicate(item))
                {
                    return true;
                }
                index++;
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            List<T> copy = null;
            lock(_obj)
            {
                copy = Provider.GetAll().Select(t => JsonTools.Deserialize<T>(t)).ToList();
            }
            return copy.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
