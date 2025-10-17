//-- Reports_View.xaml.cs --

using che_system.view_model;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using che_system.modals.model;
using che_system.repositories;

namespace che_system.view
{
    /// <summary>
    /// Interaction logic for Reports_View.xaml
    /// </summary>
    public partial class Reports_View : UserControl
    {
        public Reports_View()
        {
            InitializeComponent();
            DataContext = new Reports_ViewModel();
        }

        private void CustodianRemarks_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true; // Prevent newline

                if (sender is TextBox tb)
                {
                    // Force commit edit
                    var dataGrid = FindAncestor<DataGrid>(tb);
                    if (dataGrid != null)
                    {
                        dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                        dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                    }
                }
            }
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T target)
                    return target;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the selected tab
                var selectedTab = ReportsTabControl.SelectedItem as TabItem;
                if (selectedTab == null)
                    return;

                // Determine which DataGrid is active
                DataGrid activeGrid = null;
                string reportTitle = selectedTab.Header.ToString();

                if (selectedTab.Header.ToString() == "Monthly Chemical Usage")
                    activeGrid = MonthlyUsageGrid;
                else if (selectedTab.Header.ToString() == "Inventory Status")
                    activeGrid = InventoryStatusGrid;
                else if (selectedTab.Header.ToString() == "Replacements History")
                    activeGrid = ReplacementsGrid;

                // Validate grid content
                if (activeGrid == null || activeGrid.Items.Count == 0)
                {
                    MessageBox.Show("No data available to export.", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Prompt user for save location
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    DefaultExt = ".pdf",
                    FileName = $"{reportTitle}_{DateTime.Now:yyyyMMdd}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportDataGridToPdf(activeGrid, saveDialog.FileName, reportTitle);

                    MessageBox.Show(
                        $"Report successfully exported to {saveDialog.FileName}",
                        "Export Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting report: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExportDataGridToPdf(DataGrid dataGrid, string filePath, string reportTitle)
        {
            // Extract visible column headers
            var columns = dataGrid.Columns
                .Where(col => col.Visibility == Visibility.Visible)
                .Select(col => col.Header.ToString())
                .ToList();

            // Extract data from DataGrid
            var rows = new List<List<string>>();
            foreach (var item in dataGrid.Items)
            {
                var row = new List<string>();

                foreach (var column in dataGrid.Columns.Where(col => col.Visibility == Visibility.Visible))
                {
                    var cellValue = string.Empty;

                    if (column is DataGridTextColumn textColumn)
                    {
                        var binding = textColumn.Binding ?? textColumn.ClipboardContentBinding;

                        if (binding is System.Windows.Data.Binding b && b.Path != null)
                        {
                            var property = item.GetType().GetProperty(b.Path.Path);
                            if (property != null)
                            {
                                var val = property.GetValue(item);
                                cellValue = val?.ToString() ?? string.Empty;
                            }
                        }

                        row.Add(cellValue);
                    }
                }

                rows.Add(row);
            }

            // Generate PDF using QuestPDF
            Document.Create(document =>
            {
                document.Page(page =>
                {
                    // Landscape layout
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header (repeats automatically on every page)
                    page.Header().PaddingBottom(10).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten1).Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text(reportTitle)
                                    .SemiBold().FontSize(16);

                                column.Item().Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                                    .FontSize(9)
                                    .FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            });

                            row.ConstantItem(100).AlignRight().Text("CHE System")
                                .Bold().FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                        });
                    });

                    // Table Content
                    page.Content().Element(container =>
                    {
                        container.Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(columnsDefinition =>
                            {
                                foreach (var _ in columns)
                                    columnsDefinition.RelativeColumn();
                            });

                            // Header row
                            table.Header(header =>
                            {
                                foreach (var column in columns)
                                {
                                    header.Cell().Element(CellStyle)
                                        .Background(QuestPDF.Helpers.Colors.Grey.Lighten3)
                                        .Text(column).SemiBold();
                                }
                            });

                            // Data rows
                            int rowIndex = 0;
                            foreach (var row in rows)
                            {
                                bool isEven = rowIndex % 2 == 0;

                                foreach (var cell in row)
                                {
                                    table.Cell().Element(container =>
                                    {
                                        container = container
                                            .Background(isEven ? QuestPDF.Helpers.Colors.Grey.Lighten5 : QuestPDF.Helpers.Colors.White)
                                            .Element(CellStyle);

                                        container.Text(cell);
                                    });
                                }

                                rowIndex++;
                            }
                        });
                    });

                    // Footer with page numbering (repeats automatically)
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            })
            .GeneratePdf(filePath);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
                .Padding(5)
                .AlignMiddle();
        }

        // Updated: Persist + append [by FirstName] to Custodian Remarks (Custodians only)
        private void InventoryStatusGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (e.Row?.Item is InventoryStatusModel row && row.ItemId > 0)
                    {
                        if (DataContext is Reports_ViewModel vm && vm.IsCustodian)
                        {
                            // Capitalize first letter of name
                            var first = string.IsNullOrWhiteSpace(vm.CurrentUserFirstName)
                                ? ExtractFirstName(Thread.CurrentPrincipal?.Identity?.Name)
                                : vm.CurrentUserFirstName;

                            first = char.ToUpper(first[0]) + first.Substring(1).ToLower();

                            if (!string.IsNullOrWhiteSpace(row.CustodianRemarks))
                            {
                                var trimmed = row.CustodianRemarks.Trim();
                                var tag = $"[by {first}]";

                                if (!trimmed.EndsWith(tag, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!trimmed.Contains(tag, StringComparison.OrdinalIgnoreCase))
                                    {
                                        row.CustodianRemarks = $"{trimmed} {tag}";
                                        row.RefreshCustodianRemarks();

                                    }
                                }
                            }

                            // Save directly to DB
                            var repo = new Inventory_Repository();
                            repo.UpdateCustodianFields(row.ItemId, row.ReceivedBy, row.CustodianRemarks);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save custodian fields: {ex.Message}",
                        "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }), DispatcherPriority.Background);
        }


        private static string ExtractFirstName(string? nameOrUsername)
        {
            if (string.IsNullOrWhiteSpace(nameOrUsername)) return "Unknown";
            var parts = nameOrUsername.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : nameOrUsername;
        }
    }
}
