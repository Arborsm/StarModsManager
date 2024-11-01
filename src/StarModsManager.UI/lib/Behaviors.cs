using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.VisualTree;

namespace StarModsManager.lib;

public class DataGridBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<bool> AutoScrollToLastRowProperty =
        AvaloniaProperty.RegisterAttached<DataGridBehavior, DataGrid, bool>("AutoScrollToLastRow");

    private static readonly AttachedProperty<IDisposable?> SubscriptionProperty =
        AvaloniaProperty.RegisterAttached<DataGridBehavior, DataGrid, IDisposable>("Subscription")!;

    static DataGridBehavior()
    {
        AutoScrollToLastRowProperty.Changed.AddClassHandler<DataGrid>(OnAutoScrollToLastRowChanged);
    }

    public static void SetAutoScrollToLastRow(AvaloniaObject element, bool value)
    {
        element.SetValue(AutoScrollToLastRowProperty, value);
    }

    public static bool GetAutoScrollToLastRow(AvaloniaObject element)
    {
        return element.GetValue(AutoScrollToLastRowProperty);
    }

    private static void OnAutoScrollToLastRowChanged(DataGrid dataGrid, AvaloniaPropertyChangedEventArgs e)
    {
        if ((bool)(e.NewValue ?? false))
        {
            var subscription =
                dataGrid.GetObservable(DataGrid.ItemsSourceProperty).Subscribe(new ItemsSourceObserver(dataGrid));
            dataGrid.SetValue(SubscriptionProperty, subscription);
        }
        else
        {
            var subscription = dataGrid.GetValue(SubscriptionProperty);
            subscription?.Dispose();
            dataGrid.SetValue(SubscriptionProperty, null);
        }
    }

    private class ItemsSourceObserver(DataGrid dataGrid) : IObserver<IEnumerable>
    {
        private INotifyCollectionChanged? _currentCollection;

        public void OnCompleted()
        {
            DetachCollectionChangedHandler();
        }

        public void OnError(Exception error)
        {
            DetachCollectionChangedHandler();
        }

        public void OnNext(IEnumerable value)
        {
            DetachCollectionChangedHandler();

            if (value is not INotifyCollectionChanged notifyCollection) return;
            _currentCollection = notifyCollection;
            _currentCollection.CollectionChanged += OnCollectionChanged;
        }

        private void DetachCollectionChangedHandler()
        {
            if (_currentCollection == null) return;
            _currentCollection.CollectionChanged -= OnCollectionChanged;
            _currentCollection = null;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            var lastItem = ((IEnumerable?)sender)?.Cast<object>().LastOrDefault();
            if (lastItem != null) dataGrid.ScrollIntoView(lastItem, null);
        }
    }
}

public class ListBoxBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<ICommand> LoadMoreCommandProperty =
        AvaloniaProperty.RegisterAttached<ListBoxBehavior, ListBox, ICommand>("LoadMoreCommand");

    public static readonly AttachedProperty<double> LoadMoreThresholdProperty =
        AvaloniaProperty.RegisterAttached<ListBoxBehavior, ListBox, double>("LoadMoreThreshold", 300);

    static ListBoxBehavior()
    {
        LoadMoreCommandProperty.Changed.AddClassHandler<ListBox>(OnLoadMoreCommandChanged);
    }

    public static void SetLoadMoreCommand(ListBox element, ICommand value)
    {
        element.SetValue(LoadMoreCommandProperty, value);
    }

    public static ICommand GetLoadMoreCommand(ListBox element)
    {
        return element.GetValue(LoadMoreCommandProperty);
    }

    public static void SetLoadMoreThreshold(ListBox element, double value)
    {
        element.SetValue(LoadMoreThresholdProperty, value);
    }

    public static double GetLoadMoreThreshold(ListBox element)
    {
        return element.GetValue(LoadMoreThresholdProperty);
    }

    private static void OnLoadMoreCommandChanged(ListBox listBox, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is ICommand)
            listBox.AddHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged);
        else
            listBox.RemoveHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged);
    }

    private static void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        var scrollViewer = listBox.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer == null) return;
        var threshold = GetLoadMoreThreshold(listBox);
        var verticalOffset = scrollViewer.Offset.Y;
        var viewportHeight = scrollViewer.Viewport.Height;
        var extentHeight = scrollViewer.Extent.Height;
        if (extentHeight - (verticalOffset + viewportHeight) > threshold) return;
        var command = GetLoadMoreCommand(listBox);
        if (command.CanExecute(null)) command.Execute(null);
    }
}