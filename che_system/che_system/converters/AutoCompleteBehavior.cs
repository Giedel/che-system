using che_system.modals.model;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace che_system.converters
{
    public static class AutoCompleteBehavior
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.RegisterAttached("ItemsSource", typeof(IEnumerable), typeof(AutoCompleteBehavior),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static IEnumerable GetItemsSource(DependencyObject obj)
        {
            return (IEnumerable)obj.GetValue(ItemsSourceProperty);
        }

        public static void SetItemsSource(DependencyObject obj, IEnumerable value)
        {
            obj.SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.TextChanged -= OnTextBoxTextChanged;
                if (e.NewValue != null)
                {
                    textBox.TextChanged += OnTextBoxTextChanged;
                    CreatePopup(textBox, e.NewValue as IEnumerable);
                }
            }
        }

        private static void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var popup = textBox.Tag as Popup;
                if (popup == null) return;

                var itemsSource = GetItemsSource(textBox) as ObservableCollection<Add_Item_Model>;
                if (itemsSource == null) return;

                var searchText = textBox.Text;
                if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 1)
                {
                    (popup.Child as ListBox).ItemsSource = null;
                    popup.IsOpen = false;
                    return;
                }

                var normalizedSearch = Normalize(searchText);
                var filteredItems = itemsSource.Where(item =>
                    !string.IsNullOrEmpty(normalizedSearch) &&
                    (Normalize(item.ItemName).Contains(normalizedSearch) ||
                     Normalize(item.ChemicalFormula).Contains(normalizedSearch) ||
                     normalizedSearch.Contains(Normalize(item.ChemicalFormula))))
                    .OrderBy(item => (Normalize(item.ItemName) + Normalize(item.ChemicalFormula)).IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    .Take(10).ToList();

                var listBox = popup.Child as ListBox;
                if (listBox != null)
                {
                    listBox.ItemsSource = filteredItems;
                    popup.IsOpen = filteredItems.Any();
                }
            }
        }

        private static void CreatePopup(TextBox textBox, IEnumerable itemsSource)
        {
            var popup = new Popup
            {
                PlacementTarget = textBox,
                Placement = PlacementMode.Bottom,
                IsOpen = false,
                AllowsTransparency = true,
                StaysOpen = false
            };

            var listBox = new ListBox
            {
                MinWidth = textBox.ActualWidth,
                MaxHeight = 200,
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            listBox.SelectionChanged += (s, args) =>
            {
                if (args.AddedItems.Count > 0 && args.AddedItems[0] is Add_Item_Model selectedItem)
                {
                    TriggerSelection(textBox, popup, selectedItem);
                }
            };

            // DataTemplate for display
            var template = new DataTemplate();
            var stackFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackFactory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
            var nameFactory = new FrameworkElementFactory(typeof(TextBlock));
            nameFactory.SetBinding(TextBlock.TextProperty, new Binding("ItemName"));
            var formulaFactory = new FrameworkElementFactory(typeof(TextBlock));
            formulaFactory.SetBinding(TextBlock.TextProperty, new Binding("ChemicalFormula") { StringFormat = "({0})" });
            stackFactory.AppendChild(nameFactory);
            stackFactory.AppendChild(formulaFactory);
            template.VisualTree = stackFactory;
            listBox.ItemTemplate = template;

            // Keyboard handling
            Action<Add_Item_Model> triggerAction = selectedItem =>
            {
                TriggerSelection(textBox, popup, selectedItem);
            };

            listBox.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    if (listBox.SelectedItem is Add_Item_Model selected)
                    {
                        triggerAction(selected);
                        args.Handled = true;
                    }
                }
                else if (args.Key == Key.Escape)
                {
                    popup.IsOpen = false;
                    textBox.Focus();
                    args.Handled = true;
                }
            };

            textBox.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Down && popup.IsOpen)
                {
                    (popup.Child as ListBox)?.Focus();
                    args.Handled = true;
                }
                else if (args.Key == Key.Enter)
                {
                    var popup = textBox.Tag as Popup;
                    if (popup?.IsOpen == true)
                    {
                        var listBox = popup.Child as ListBox;
                        if (listBox?.ItemsSource?.Cast<Add_Item_Model>().Any() == true)
                        {
                            if (listBox.SelectedItem == null)
                            {
                                listBox.SelectedIndex = 0;
                            }
                            if (listBox.SelectedItem is Add_Item_Model selected)
                            {
                                triggerAction(selected);
                                args.Handled = true;
                                return;
                            }
                        }
                    }
                    // If no popup or no selection, let DataGrid handle Enter to commit
                }
            };

            popup.Child = listBox;
            textBox.Tag = popup;
        }

        private static void TriggerSelection(TextBox textBox, Popup popup, Add_Item_Model selectedItem)
        {
            var detail = textBox.DataContext as SlipDetail_Model;
            if (detail != null)
            {
                detail.ItemName = selectedItem.ItemName;
                detail.ItemId = selectedItem.ItemId;
                detail.SelectedItem = selectedItem;
            }
            textBox.Text = selectedItem.ItemName;
            popup.IsOpen = false;
            textBox.Focus();

            // Commit DataGrid cell if in editing mode
            DataGrid dataGrid = null;
            DependencyObject parent = textBox;
            while (parent != null)
            {
                if (parent is DataGrid)
                {
                    dataGrid = (DataGrid)parent;
                    break;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (dataGrid != null)
            {
                dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            }
        }

        private static string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.ToLowerInvariant()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace(".", "")
                .Replace(",", "");
        }
    }
}
