using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [TestClass]
    public abstract class ThemeTestFixture
    {
        private ObjectListViewColorMode originalMode;

        [TestInitialize]
        public void SetDarkMode() {
            this.originalMode = ObjectListViewTheme.ColorMode;
            ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Dark;
            if (!ObjectListViewTheme.IsDarkMode)
            {
                Assert.Inconclusive("Dark mode is disabled in high contrast.");
            }
        }

        [TestCleanup]
        public void RestoreColorMode() {
            ObjectListViewTheme.ColorMode = this.originalMode;
        }

        protected static bool ContainsColor(Bitmap bitmap, Color color, Rectangle? region = null) {
            Rectangle bounds = region ?? new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            for (int y = bounds.Top; y < bounds.Bottom; y++)
            {
                for (int x = bounds.Left; x < bounds.Right; x++)
                {
                    if (bitmap.GetPixel(x, y).ToArgb() == color.ToArgb())
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
