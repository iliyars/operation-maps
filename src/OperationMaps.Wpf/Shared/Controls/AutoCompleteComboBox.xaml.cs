using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace OperationMaps.Wpf.Shared.Controls;

/// <summary>
/// An editable combo box that filters its drop-down list by substring match
/// against <see cref="DisplayMemberPath"/> as the user types, instead of the
/// stock <see cref="ComboBox"/>'s "jump to the first item starting with the
/// typed letter" behavior. Used where a plain dropdown list is long enough
/// that scanning it by eye is impractical (e.g. picking one of ~80 forms).
/// Wraps a real <see cref="ComboBox"/> rather than a hand-rolled Popup so
/// click/scroll/keyboard-nav in the dropdown all work for free.
/// </summary>
public partial class AutoCompleteComboBox : UserControl
{
  private List<object> _allItems = [];
  private ListCollectionView? _view;
  private bool _syncingSelection;
  private bool _syncingText;

  public AutoCompleteComboBox()
  {
    InitializeComponent();

    // ComboBox.Text isn't owned by this class, so listening for its changes
    // needs a property-changed subscription rather than a CLR event.
    DependencyPropertyDescriptor
        .FromProperty(ComboBox.TextProperty, typeof(ComboBox))
        .AddValueChanged(PART_ComboBox, OnComboTextChanged);

    PART_ComboBox.SelectionChanged += OnComboSelectionChanged;
  }

  // ── Dependency properties ──────────────────────────────────────────────

  public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
      nameof(ItemsSource), typeof(IEnumerable), typeof(AutoCompleteComboBox),
      new PropertyMetadata(null, OnItemsSourceChanged));

  public IEnumerable? ItemsSource
  {
    get => (IEnumerable?)GetValue(ItemsSourceProperty);
    set => SetValue(ItemsSourceProperty, value);
  }

  public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
      nameof(SelectedItem), typeof(object), typeof(AutoCompleteComboBox),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

  public object? SelectedItem
  {
    get => GetValue(SelectedItemProperty);
    set => SetValue(SelectedItemProperty, value);
  }

  public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
      nameof(DisplayMemberPath), typeof(string), typeof(AutoCompleteComboBox), new PropertyMetadata(""));

  public string DisplayMemberPath
  {
    get => (string)GetValue(DisplayMemberPathProperty);
    set => SetValue(DisplayMemberPathProperty, value);
  }

  /// <summary>
  /// The raw typed text, independent of <see cref="SelectedItem"/> — lets a
  /// caller accept free text the user typed that doesn't match any item in
  /// the list (e.g. a brand-new category to be created), not just a picked
  /// existing one.
  /// </summary>
  public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
      nameof(Text), typeof(string), typeof(AutoCompleteComboBox),
      new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextPropertyChanged));

  public string Text
  {
    get => (string)GetValue(TextProperty);
    set => SetValue(TextProperty, value);
  }

  private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    var control = (AutoCompleteComboBox)d;
    if (control._syncingText) return;

    control._syncingText = true;
    control.PART_ComboBox.Text = (string?)e.NewValue ?? "";
    control._syncingText = false;
  }

  // ── Items / filtering ────────────────────────────────────────────────────

  private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    var control = (AutoCompleteComboBox)d;

    // The ViewModel binds one ObservableCollection instance for this
    // control's whole lifetime and populates it later (async load) — this
    // callback alone only fires when the ItemsSource reference itself is
    // reassigned, so also watch content changes on the collection itself.
    if (e.OldValue is INotifyCollectionChanged oldNotify)
      oldNotify.CollectionChanged -= control.OnSourceCollectionChanged;
    if (e.NewValue is INotifyCollectionChanged newNotify)
      newNotify.CollectionChanged += control.OnSourceCollectionChanged;

    control.RebuildView();
  }

  private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
      => RebuildView();

  private void RebuildView()
  {
    _allItems = ItemsSource?.Cast<object>().ToList() ?? [];
    _view = new ListCollectionView(_allItems) { Filter = FilterPredicate };
    PART_ComboBox.ItemsSource = _view;

    if (SelectedItem is not null)
      SyncComboSelection();
  }

  private bool FilterPredicate(object item)
  {
    var text = PART_ComboBox.Text;
    return string.IsNullOrWhiteSpace(text)
        || GetDisplayText(item).Contains(text, StringComparison.CurrentCultureIgnoreCase);
  }

  private string GetDisplayText(object item)
  {
    if (string.IsNullOrEmpty(DisplayMemberPath)) return item.ToString() ?? "";
    var prop = TypeDescriptor.GetProperties(item)[DisplayMemberPath];
    return prop?.GetValue(item)?.ToString() ?? item.ToString() ?? "";
  }

  // ── Selection sync ───────────────────────────────────────────────────────

  private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      => ((AutoCompleteComboBox)d).SyncComboSelection();

  private void SyncComboSelection()
  {
    if (_syncingSelection) return;
    _syncingSelection = true;
    PART_ComboBox.SelectedItem = SelectedItem;
    _syncingSelection = false;
  }

  private void OnComboSelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    if (_syncingSelection) return;
    _syncingSelection = true;
    SelectedItem = PART_ComboBox.SelectedItem;
    PART_ComboBox.IsDropDownOpen = false;

    // ComboBox updates its own editable Text to match the new selection as
    // part of the same operation, which fires OnComboTextChanged — deferred
    // reset (rather than clearing the flag immediately) makes sure that
    // follow-up call still sees _syncingSelection=true and doesn't
    // immediately reopen the dropdown it was just told to close.
    Dispatcher.BeginInvoke(() => _syncingSelection = false,
        System.Windows.Threading.DispatcherPriority.Input);
  }

  // ── Typing -> filter ─────────────────────────────────────────────────────

  private void OnComboTextChanged(object? sender, EventArgs e)
  {
    if (!_syncingText)
    {
      _syncingText = true;
      Text = PART_ComboBox.Text;
      _syncingText = false;
    }

    if (_syncingSelection) return;
    _view?.Refresh();
    if (!PART_ComboBox.IsDropDownOpen && PART_ComboBox.IsKeyboardFocusWithin)
      PART_ComboBox.IsDropDownOpen = true;
  }
}
