﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace RPGGamer_Radio_Desktop.Helpers;

public class CustomCollection<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) return;

        foreach (var item in items)
        {
            Items.Add(item);
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}