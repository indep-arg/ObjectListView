using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ObjectListViewDemo;

namespace BrightIdeasSoftware.Tests {
    [STATestClass]
    public class TestShellImages {
        [TestMethod]
        public void Test_ShellIcons_DoNotLeakNativeHandles() {
            using (Icon warmup = ShellUtilities.GetFileIcon(".txt", true, true))
            {
                Assert.IsNotNull(warmup);
            }

            using (Process process = Process.GetCurrentProcess()) {
                int before = GetGuiResources(process.Handle, 1);
                for (int i = 0; i < 200; i++) {
                    using (Icon icon = ShellUtilities.GetFileIcon(".txt", true, true))
                    {
                        Assert.IsNotNull(icon);
                    }
                }
                int after = GetGuiResources(process.Handle, 1);
                Assert.IsLessThanOrEqualTo(before + 5, after);
            }
        }

        [TestMethod]
        public void Test_ResizedShellImages_AreCachedAndRemainUsable() {
            using (var small = new ImageList { ImageSize = new Size(24, 24), ColorDepth = ColorDepth.Depth32Bit })
            using (var large = new ImageList { ImageSize = new Size(48, 48), ColorDepth = ColorDepth.Depth32Bit })
            using (var list = new ObjectListView { SmallImageList = small, LargeImageList = large }) {
                var helper = new SysImageListHelper(list);
                int index = helper.GetImageIndex(".txt");
                Assert.IsGreaterThanOrEqualTo(0, index);
                Assert.AreEqual(index, helper.GetImageIndex(".txt"));
                Assert.AreEqual(1, small.Images.Count);
                Assert.AreEqual(small.Images.Count, large.Images.Count);
                using (Image smallImage = small.Images[index])
                using (Image largeImage = large.Images[index]) {
                    Assert.AreEqual(small.ImageSize, smallImage.Size);
                    Assert.AreEqual(large.ImageSize, largeImage.Size);
                    using (var bitmap = new Bitmap(48, 48))
                    using (var graphics = Graphics.FromImage(bitmap)) {
                        graphics.DrawImage(smallImage, Point.Empty);
                        graphics.DrawImage(largeImage, Point.Empty);
                    }
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetGuiResources(IntPtr process, int flags);
    }
}
