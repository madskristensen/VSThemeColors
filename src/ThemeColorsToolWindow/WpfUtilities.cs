using System.Runtime.InteropServices;
using System.Windows.Media;
using Microsoft;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MadsKristensen.ExtensibilityTools.ThemeColorsToolWindow
{
    public static class ImageMonikerHelpers
    {
        public static ImageSource GetImage(this ImageMoniker imageMoniker)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var vsIconService = ServiceProvider.GlobalProvider.GetService(typeof(SVsImageService)) as IVsImageService2;
            Assumes.Present(vsIconService);

            var imageAttributes = new ImageAttributes
            {
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
                ImageType = (uint)_UIImageType.IT_Bitmap,
                Format = (uint)_UIDataFormat.DF_WPF,
                LogicalHeight = 16,//IconHeight,
                LogicalWidth = 16,//IconWidth,
                StructSize = Marshal.SizeOf(typeof(ImageAttributes))
            };

            IVsUIObject result = vsIconService.GetImage(imageMoniker, imageAttributes);

            result.get_Data(out var data);
            var glyph = data as ImageSource;
            glyph.Freeze();

            return glyph;
        }
    }
}
