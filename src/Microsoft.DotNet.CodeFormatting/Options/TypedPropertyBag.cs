﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.CodeAnalysis.Options
{
    [Serializable]
    public class TypedPropertyBag<T> : Dictionary<string, T> where T : new()
    {
        public TypedPropertyBag() : this(null, StringComparer.OrdinalIgnoreCase) { }

        public TypedPropertyBag(PropertyBag initializer, IEqualityComparer<string> comparer) : base(comparer)
        {
            if (initializer != null)
            {
                foreach (string key in initializer.Keys)
                {
                    this[key] = (T)initializer[key];
                }
            }
        }

        protected TypedPropertyBag(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public new T this[string key]
        {
            get
            {
                T result;

                if (!base.TryGetValue(key, out result))
                {
                    result = base[key] = new T();
                }
                return result;
            }
            set { base[key] = value; }
        }

        public virtual T GetProperty(PerLanguageOption<T> setting, bool cacheDefault = true)
        {
            if (setting == null) { throw new ArgumentNullException("setting"); }

            T value;
            if (!base.TryGetValue(setting.Name, out value) && setting.DefaultValue != null)
            {
                value = setting.DefaultValue;

                if (cacheDefault) { this[setting.Name] = value; }
            }
            return value;
        }

        public virtual void SetProperty(IOption setting, T value)
        {
            if (setting == null) { throw new ArgumentNullException("setting"); }

            if (value == null && this.ContainsKey(setting.Name))
            {
                this.Remove(setting.Name);
                return;
            }
            this[setting.Name] = value;
        }
    }
}