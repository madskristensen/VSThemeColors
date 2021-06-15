using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace VSThemeColors
{
    [Guid(PackageGuids.guidSwatchWindowString)]
    public class SwatchesWindow : ToolWindowPane
    {
        public const string _title = "Theme Swatches";

        public SwatchesWindow() : this(null)
        { }

#pragma warning disable IDE0060 // Remove unused parameter
        public SwatchesWindow(object state) : base(null)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            Caption = _title;
            Content = new SwatchesWindowControl();
        }

        public static object TextBoxTextBrushKey => EnvironmentColors.ComboBoxTextBrushKey;

        public static object TextBoxBackgroundBrushKey => EnvironmentColors.ComboBoxBackgroundBrushKey;

        public static object TextBoxBorderBrushKey => EnvironmentColors.ComboBoxBorderBrushKey;

        public static object TextBrushKey => EnvironmentColors.BrandedUITextBrushKey;
    }
}
