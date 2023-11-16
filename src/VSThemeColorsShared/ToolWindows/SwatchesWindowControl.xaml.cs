using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace VSThemeColors
{
    /// <summary>
    ///     Interaction logic for SwatchesWindowControl.
    /// </summary>
    public partial class SwatchesWindowControl : UserControl
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SwatchesWindowControl" /> class.
        /// </summary>
        public SwatchesWindowControl()
        {
            InitializeComponent();

            AddColors(typeof(CommonControlsColors));
            AddColors(typeof(CommonDocumentColors));
            AddColors(typeof(EnvironmentColors));
            AddColors(typeof(HeaderColors));
            AddColors(typeof(InfoBarColors));
            AddColors(typeof(ProgressBarColors));
            AddColors(typeof(SearchControlColors));
            AddColors(typeof(StartPageColors));
            AddColors(typeof(ThemedDialogColors));

            if (Vsix.Name == "VS Theme Colors 2022") AddColors(Type.GetType("Microsoft.VisualStudio.PlatformUI.ThemedUtilityDialogColors, Microsoft.VisualStudio.Shell.15.0, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", true));

            AddColors(typeof(TreeViewColors));
            AddColors(typeof(UnthemedDialogColors));
            AddColors(typeof(VisualStudioInstallerColors));

            SizeChanged += OnSizeChanged;

            SimplifyTreeView();
        }

        private void AddColors(Type typeColors)
        {
            var tileSize = CalculateTileSize();

            foreach (PropertyInfo property in typeColors.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (!property.Name.EndsWith("BrushKey"))
                    continue;
                AddColorToWrapPanel(typeColors, tileSize, property);
                AddColorToTreeView(typeColors, property);
            }
        }

        private void AddColorToWrapPanel(Type typeColors, double tileSize, PropertyInfo property)
        {
            var tile = new Grid
            {
                Width = tileSize,
                Height = tileSize,
                Tag = property,
                ToolTip = $"{typeColors}.{property.Name}"
            };
            ToolTipService.SetShowDuration(tile, int.MaxValue);
            ToolTipService.SetInitialShowDelay(tile, 0);
            ToolTipService.SetBetweenShowDelay(tile, 0);

            tile.SetResourceReference(BackgroundProperty, property.GetValue(null));
            tile.MouseDown += TileOnMouseDown;
            Root.Children.Add(tile);
        }

        private void AddColorToTreeView(Type typeColors, PropertyInfo property)
        {
            var fullName = $"{typeColors}.{property.Name}";
            AddItemToTreeView(ColorTreeView, fullName.Split('.'));
        }

        private void SimplifyTreeView()
        {
            foreach (var item in ColorTreeView.Items) CombineSingleChildNodes(item as HeaderedItemsControl);

            if (ColorTreeView.Items.Count == 1)
            {
                var item = (TreeViewItem)ColorTreeView.Items[0];
                item.IsExpanded = true;
            }
        }

        private void AddItemToTreeView(ItemsControl parent, string[] path, int level = 0)
        {
            if (level >= path.Length)
                return;

            TreeViewItem existingItem = parent.Items
                .OfType<TreeViewItem>()
                .FirstOrDefault(item => item.Header.ToString() == path[level]);
            TreeViewItem currentItem;

            if (existingItem == null)
            {
                currentItem = new TreeViewItem { Header = path[level] };
                currentItem.SetResourceReference(ForegroundProperty, SwatchesWindow.TextBoxTextBrushKey);
                parent.Items.Add(currentItem);
                currentItem.Selected += TreeViewItemOnSelected;
            }
            else
            {
                currentItem = existingItem;
            }

            if (level < path.Length - 1) AddItemToTreeView(currentItem, path, level + 1);
        }

        private static void CombineSingleChildNodes(HeaderedItemsControl o)
        {
            while (true)
            {
                if (o.Items.Count != 1)
                {
                    foreach (TreeViewItem item in o.Items.OfType<TreeViewItem>()) CombineSingleChildNodes(item);

                    break;
                }

                var singleChild = (TreeViewItem)o.Items[0];
                o.Header = $"{o.Header}.{singleChild.Header}";
                o.Items.Clear();
                var children = singleChild.Items.OfType<TreeViewItem>().ToList();
                foreach (TreeViewItem grandchild in children)
                {
                    singleChild.Items.Remove(grandchild);
                    o.Items.Add(grandchild);
                }
            }
        }

        private void TreeViewItemOnSelected(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item)
            {
                var fullPath = GetFullPath(item);
                UpdateTextBoxWithoutTriggeringEvent(fullPath);
                FilterRoot(fullPath);
            }

            e.Handled = true;
        }

        private static string GetFullPath(TreeViewItem item)
        {
            var path = item.Header.ToString();
            DependencyObject parent = item.Parent;

            while (parent is TreeViewItem parentItem)
            {
                path = $"{parentItem.Header}.{path}";
                parent = parentItem.Parent;
            }

            return path;
        }

        private double CalculateTileSize()
        {
            var width = Root.ActualWidth % 100;

            //If there's less than half a block free, expand the tiles to fit
            if (width < 50)
                width = Root.ActualWidth / Math.Floor(Root.ActualWidth / 100);
            //There's more than half a block free, shrink the tiles to pull another onto the row
            else
                width = Root.ActualWidth / Math.Ceiling(Root.ActualWidth / 100);

            return width;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var tileSize = CalculateTileSize();

            foreach (Grid child in Root.Children.OfType<Grid>())
            {
                child.Width = tileSize;
                child.Height = tileSize;
            }
        }

        private void TileOnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var tile = (Grid)sender;
            var token = tile.Tag as PropertyInfo;
            var parent = (Panel)tile.Parent;
            parent.Children.Remove(tile);
            parent.Children.Insert(0, tile);

            var propertyName = token.Name;
            var className = token.DeclaringType.Name;
            var fullClassName = token.DeclaringType.FullName;
            var ns = token.DeclaringType.Namespace;
            var assemblyName = token.DeclaringType.Assembly.GetName().Name;

            //var propertyName = (string)tile.ToolTip;
            BrushName.Text = propertyName;
            XamlNamespace.Text = $"xmlns:platformUI=\"clr-namespace:{ns};assembly={assemblyName}\"";
            XamlUsage.Text = $"{{DynamicResource {{x:Static platformUI:{className}.{propertyName}}}}}";
            CSharpUsage.Text = $"{{TARGET_OBJECT}}.SetResourceReference({{DEPENDENCY_PROPERTY}}, {fullClassName}.{propertyName});";
        }

        private void UpdateTextBoxWithoutTriggeringEvent(string text)
        {
            // Entferne temporär den Event-Handler
            SearchTextBox.TextChanged -= TextBoxBase_OnTextChanged;

            // Aktualisiere den Text
            SearchTextBox.Text = text;

            // Füge den Event-Handler wieder hinzu
            SearchTextBox.TextChanged += TextBoxBase_OnTextChanged;
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UnselectTreeView(ColorTreeView.Items);
            FilterRoot(((TextBox)sender).Text);
        }

        private void UnselectTreeView(ItemCollection item)
        {
            foreach (TreeViewItem treeViewItem in item.OfType<TreeViewItem>())
            {
                treeViewItem.IsSelected = false;
                UnselectTreeView(treeViewItem.Items);
            }
        }

        private void FilterRoot(string value)
        {
            foreach (Grid child in Root.Children.OfType<Grid>())
            {
                var tag = child.Tag as PropertyInfo;
                var propertyName = $"{tag.DeclaringType.FullName}.{tag.Name}";

                var show = propertyName.IndexOf(value, StringComparison.OrdinalIgnoreCase) > -1;
                child.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void CopyClick(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var parent = (ContextMenu)item.Parent;

            if (parent.PlacementTarget is TextBox target && !string.IsNullOrEmpty(target.Text)) Clipboard.SetText(target.Text);
        }

        private void CsharpCopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(CSharpUsage.Text);
        }

        private void XamlUsageCopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(XamlUsage.Text);
        }

        private void XamlNamespaceCopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(XamlNamespace.Text);
        }
    }
}