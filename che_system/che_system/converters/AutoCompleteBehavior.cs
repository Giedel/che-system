//-- AutoCompleteBehavior.cs --
using che_system.modals.model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace che_system.converters
{
    public static class AutoCompleteBehavior
    {
        // Categories to exclude from suggestions (lowercase)
        private static readonly string[] ExcludedCategories = { "consumable", "chemical" };

        // Public configuration dependency properties
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.RegisterAttached(
                "ItemsSource",
                typeof(IEnumerable),
                typeof(AutoCompleteBehavior),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty MinimumPrefixLengthProperty =
            DependencyProperty.RegisterAttached(
                "MinimumPrefixLength",
                typeof(int),
                typeof(AutoCompleteBehavior),
                new PropertyMetadata(1));

        public static readonly DependencyProperty MaxSuggestionsProperty =
            DependencyProperty.RegisterAttached(
                "MaxSuggestions",
                typeof(int),
                typeof(AutoCompleteBehavior),
                new PropertyMetadata(10));

        public static IEnumerable GetItemsSource(DependencyObject obj) =>
            (IEnumerable)obj.GetValue(ItemsSourceProperty);

        public static void SetItemsSource(DependencyObject obj, IEnumerable value) =>
            obj.SetValue(ItemsSourceProperty, value);

        public static int GetMinimumPrefixLength(DependencyObject obj) =>
            (int)obj.GetValue(MinimumPrefixLengthProperty);

        public static void SetMinimumPrefixLength(DependencyObject obj, int value) =>
            obj.SetValue(MinimumPrefixLengthProperty, value);

        public static int GetMaxSuggestions(DependencyObject obj) =>
            (int)obj.GetValue(MaxSuggestionsProperty);

        public static void SetMaxSuggestions(DependencyObject obj, int value) =>
            obj.SetValue(MaxSuggestionsProperty, value);

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox) return;

            textBox.TextChanged -= OnTextBoxTextChanged;
            textBox.PreviewKeyDown -= OnTextBoxPreviewKeyDown;
            textBox.Unloaded -= OnTextBoxUnloaded;
            textBox.SizeChanged -= OnTextBoxSizeChanged;

            if (textBox.Tag is Popup oldPopup)
            {
                if (oldPopup.Child is ListBox oldList)
                {
                    oldList.SelectionChanged -= OnListBoxSelectionChanged;
                    oldList.PreviewMouseLeftButtonUp -= OnListBoxPreviewMouseLeftButtonUp;
                    oldList.KeyDown -= OnListBoxKeyDown;
                }
                oldPopup.IsOpen = false;
                textBox.Tag = null;
            }

            if (e.NewValue != null)
            {
                EnsurePopup(textBox);
                textBox.TextChanged += OnTextBoxTextChanged;
                textBox.PreviewKeyDown += OnTextBoxPreviewKeyDown;
                textBox.Unloaded += OnTextBoxUnloaded;
                textBox.SizeChanged += OnTextBoxSizeChanged;
            }
        }

        private static void OnTextBoxUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.TextChanged -= OnTextBoxTextChanged;
                tb.PreviewKeyDown -= OnTextBoxPreviewKeyDown;
                tb.Unloaded -= OnTextBoxUnloaded;
                tb.SizeChanged -= OnTextBoxSizeChanged;
            }
        }

        private static void OnTextBoxSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is TextBox tb && tb.Tag is Popup popup && popup.Child is FrameworkElement fe)
            {
                fe.MinWidth = tb.ActualWidth;
            }
        }

        private static void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            if (textBox.Tag is not Popup popup) return;
            if (popup.Child is not ListBox listBox) return;
            if (!popup.IsOpen) return;

            switch (e.Key)
            {
                case Key.Down:
                    if (listBox.Items.Count > 0)
                    {
                        if (listBox.SelectedIndex < 0) listBox.SelectedIndex = 0;
                        else if (listBox.SelectedIndex < listBox.Items.Count - 1)
                            listBox.SelectedIndex++;
                        listBox.ScrollIntoView(listBox.SelectedItem);
                        e.Handled = true;
                    }
                    break;

                case Key.Up:
                    if (listBox.Items.Count > 0)
                    {
                        if (listBox.SelectedIndex > 0)
                        {
                            listBox.SelectedIndex--;
                            listBox.ScrollIntoView(listBox.SelectedItem);
                        }
                        e.Handled = true;
                    }
                    break;

                case Key.Enter:
                    if (listBox.SelectedItem is Add_Item_Model sel)
                    {
                        TriggerSelection(textBox, popup, sel);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    popup.IsOpen = false;
                    e.Handled = true;
                    break;
            }
        }

        private static void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            if (textBox.Tag is not Popup popup) return;
            if (popup.Child is not ListBox listBox) return;

            var minLen = GetMinimumPrefixLength(textBox);
            var maxSuggestions = GetMaxSuggestions(textBox);

            var searchText = textBox.Text ?? string.Empty;
            if (searchText.Length < minLen)
            {
                listBox.ItemsSource = null;
                popup.IsOpen = false;
                return;
            }

            var source = GetItemsSource(textBox);
            if (source == null)
            {
                listBox.ItemsSource = null;
                popup.IsOpen = false;
                return;
            }

            var normalizedSearch = Normalize(searchText);
            if (string.IsNullOrEmpty(normalizedSearch))
            {
                listBox.ItemsSource = null;
                popup.IsOpen = false;
                return;
            }

            // Exclude both Consumable and Chemical categories (case-insensitive)
            var candidates = source
                .OfType<Add_Item_Model>()
                .Where(item => item == null || !IsExcludedCategory(item.Category));

            var filtered = candidates
                .Select(item => new
                {
                    Item = item,
                    NameNorm = Normalize(item?.ItemName),
                    FormulaNorm = Normalize(item?.ChemicalFormula)
                })
                .Where(x =>
                    (!string.IsNullOrEmpty(x.NameNorm) && x.NameNorm.Contains(normalizedSearch)) ||
                    (!string.IsNullOrEmpty(x.FormulaNorm) && x.FormulaNorm.Contains(normalizedSearch)) ||
                    (!string.IsNullOrEmpty(x.FormulaNorm) && normalizedSearch.Contains(x.FormulaNorm)))
                .OrderBy(x =>
                {
                    var composite = (x.NameNorm + x.FormulaNorm);
                    var idx = composite.IndexOf(normalizedSearch, StringComparison.Ordinal);
                    return idx < 0 ? int.MaxValue : idx;
                })
                .ThenBy(x => x.NameNorm)
                .Take(maxSuggestions)
                .Select(x => x.Item)
                .ToList();

            listBox.ItemsSource = filtered;
            popup.IsOpen = filtered.Any();

            if (popup.IsOpen && listBox.SelectedIndex < 0 && filtered.Count == 1)
                listBox.SelectedIndex = 0;
        }

        private static bool IsExcludedCategory(string category) =>
            !string.IsNullOrWhiteSpace(category) &&
            ExcludedCategories.Contains(category.Trim().ToLowerInvariant());

        private static void EnsurePopup(TextBox textBox)
        {
            if (textBox.Tag is Popup existing && existing.Child is ListBox) return;

            var popup = new Popup
            {
                PlacementTarget = textBox,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                IsOpen = false
            };

            var listBox = new ListBox
            {
                MinWidth = textBox.ActualWidth,
                MaxHeight = 240,
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
            };

            listBox.ItemContainerStyle = new Style(typeof(ListBoxItem))
            {
                Setters =
                {
                    new Setter(Control.PaddingProperty, new Thickness(4,2,4,2)),
                    new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)
                }
            };

            var template = new DataTemplate(typeof(Add_Item_Model));
            var stackFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackFactory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            var nameFactory = new FrameworkElementFactory(typeof(TextBlock));
            nameFactory.SetBinding(TextBlock.TextProperty, new Binding(nameof(Add_Item_Model.ItemName)));

            var formulaFactory = new FrameworkElementFactory(typeof(TextBlock));
            formulaFactory.SetBinding(TextBlock.TextProperty,
                new Binding(nameof(Add_Item_Model.ChemicalFormula))
                {
                    Converter = ChemicalFormulaFormatter.Instance
                });

            var formulaStyle = new Style(typeof(TextBlock));
            formulaStyle.Setters.Add(new Setter(TextElement.FontSizeProperty, 11.0));
            formulaStyle.Setters.Add(new Setter(TextElement.ForegroundProperty, Brushes.DimGray));
            var hideTrigger = new Trigger
            {
                Property = TextBlock.TextProperty,
                Value = string.Empty
            };
            hideTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            formulaStyle.Triggers.Add(hideTrigger);
            formulaFactory.SetValue(FrameworkElement.StyleProperty, formulaStyle);

            stackFactory.AppendChild(nameFactory);
            stackFactory.AppendChild(formulaFactory);
            template.VisualTree = stackFactory;
            listBox.ItemTemplate = template;

            listBox.SelectionChanged += OnListBoxSelectionChanged;
            listBox.PreviewMouseLeftButtonUp += OnListBoxPreviewMouseLeftButtonUp;
            listBox.KeyDown += OnListBoxKeyDown;

            popup.Child = listBox;
            textBox.Tag = popup;
        }

        private static void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private static void OnListBoxPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox lb &&
                lb.SelectedItem is Add_Item_Model item &&
                GetOwningTextBox(lb) is TextBox tb &&
                tb.Tag is Popup popup)
            {
                TriggerSelection(tb, popup, item);
                e.Handled = true;
            }
        }

        private static void OnListBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ListBox lb) return;
            if (GetOwningTextBox(lb) is not TextBox tb) return;
            if (tb.Tag is not Popup popup) return;

            if (e.Key == Key.Enter && lb.SelectedItem is Add_Item_Model item)
            {
                TriggerSelection(tb, popup, item);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                popup.IsOpen = false;
                tb.Focus();
                e.Handled = true;
            }
        }

        private static TextBox GetOwningTextBox(DependencyObject child)
        {
            DependencyObject current = child;
            while (current != null)
            {
                if (current is Popup popup && popup.PlacementTarget is TextBox tb) return tb;
                current = VisualTreeHelper.GetParent(current) ?? (current as FrameworkElement)?.Parent;
            }
            return null;
        }

        private static void TriggerSelection(TextBox textBox, Popup popup, Add_Item_Model selectedItem)
        {
            if (textBox.DataContext is SlipDetail_Model detail)
            {
                detail.ItemName = selectedItem.ItemName;
                detail.ItemId = selectedItem.ItemId;
                detail.SelectedItem = selectedItem;
            }

            textBox.TextChanged -= OnTextBoxTextChanged;
            textBox.Text = selectedItem.ItemName;
            textBox.CaretIndex = textBox.Text.Length;
            textBox.TextChanged += OnTextBoxTextChanged;

            popup.IsOpen = false;
            textBox.Focus();

            var dataGrid = FindParent<DataGrid>(textBox);
            dataGrid?.CommitEdit(DataGridEditingUnit.Cell, true);
        }

        private static T FindParent<T>(DependencyObject start) where T : DependencyObject
        {
            var current = start;
            while (current != null)
            {
                if (current is T wanted) return wanted;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            Span<char> buffer = stackalloc char[input.Length];
            var span = input.AsSpan();
            int idx = 0;
            for (int i = 0; i < span.Length; i++)
            {
                var c = span[i];
                if (c == ' ' || c == '-' || c == '.' || c == ',') continue;
                buffer[idx++] = char.ToLowerInvariant(c);
            }
            return new string(buffer.Slice(0, idx));
        }

        private sealed class ChemicalFormulaFormatter : IValueConverter
        {
            public static readonly ChemicalFormulaFormatter Instance = new();
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var text = value as string;
                return string.IsNullOrWhiteSpace(text) ? string.Empty : $"({text.Trim()})";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var text = value as string;
                if (string.IsNullOrWhiteSpace(text)) return string.Empty;
                if (text.StartsWith("(") && text.EndsWith(")") && text.Length >= 2)
                    return text[1..^1];
                return text;
            }
        }
    }
}
