/*
 * [File purpose]
 * Author: Phillip Piper
 * Date: 10/25/2008 11:06 PM
 * 
 * CHANGE LOG:
 * when who what
 * 10/25/2008 JPP  Initial Version
 */

using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestColumnButtonRendering : WinFormsTestFixture
    {
        [TestMethod]
        [DataRow("", false, "image", 60)]
        [DataRow("", false, "image", 16)]
        [DataRow("Run", false, "image", 60)]
        [DataRow("Run", true, "image", 60)]
        [DataRow("Run", false, "index", 60)]
        [DataRow("Run", false, "key", 60)]
        public void Test_ButtonImage_IsDrawnBesideText(string text, bool rightToLeft, string selectorKind, int height) {
            using (var image = new Bitmap(16, 16))
            using (var imageGraphics = Graphics.FromImage(image))
            using (var images = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = image.Size })
            using (var bitmap = new Bitmap(240, 80))
            using (var graphics = Graphics.FromImage(bitmap)) {
                imageGraphics.Clear(Color.Magenta);
                images.Images.Add("icon", image);
                var list = mainForm.objectListView1;
                list.RightToLeftLayout = rightToLeft;
                var column = list.GetColumn(0);
                column.AspectGetter = delegate { return text; };
                column.ImageGetter = delegate {
                    if (selectorKind == "index")
                    {
                        return 0;
                    }

                    if (selectorKind == "key")
                    {
                        return "icon";
                    }

                    return image;
                };
                column.IsButton = true;
                list.SetObjects(new[] { PersonDb.All[0] });
                var item = list.GetItem(0);
                var renderer = new ButtonRendererProbe {
                    ListView = list, Column = column, ListItem = item, SubItem = item.GetSubItem(0),
                    ImageList = images
                };
                Rectangle bounds = new Rectangle(10, 10, 200, height);
                graphics.Clear(Color.White);
                renderer.DrawButton(graphics, bounds);

                int pixels = 0;
                int totalX = 0;
                for (int y = 0; y < bitmap.Height; y++) {
                    for (int x = 0; x < bitmap.Width; x++) {
                        if (bitmap.GetPixel(x, y).ToArgb() == Color.Magenta.ToArgb()) {
                            Assert.IsTrue(bounds.Contains(x, y));
                            pixels++;
                            totalX += x;
                        }
                    }
                }
                Assert.AreEqual(image.Width * image.Height, pixels);
                if (text.Length > 0)
                {
                    Assert.AreEqual(rightToLeft, (double)totalX / pixels > bounds.Left + bounds.Width / 2);
                }
            }
        }

        [TestMethod]
        public void Test_ButtonContentSize_IncludesImage() {
            using (var image = new Bitmap(24, 32))
            using (var bitmap = new Bitmap(300, 100))
            using (var graphics = Graphics.FromImage(bitmap)) {
                var list = mainForm.objectListView1;
                list.SetObjects(new[] { PersonDb.All[0] });
                var item = list.GetItem(0);
                var renderer = new ButtonRendererProbe {
                    ListView = list, Column = list.GetColumn(0), ListItem = item, SubItem = item.GetSubItem(0)
                };
                Rectangle bounds = new Rectangle(0, 0, 300, 100);
                Size textOnly = renderer.MeasureButton(graphics, bounds);
                item.ImageSelector = image;
                Size withImage = renderer.MeasureButton(graphics, bounds);
                Assert.AreEqual(textOnly.Width + image.Width + renderer.Spacing, withImage.Width);
                Assert.IsGreaterThanOrEqualTo(image.Height + renderer.ButtonPadding.Value.Height * 2, withImage.Height);
                Assert.AreEqual(16, renderer.MeasureButton(graphics, new Rectangle(0, 0, 300, 16)).Height);
            }
        }

        private sealed class ButtonRendererProbe : ColumnButtonRenderer
        {
            public void DrawButton(Graphics graphics, Rectangle bounds) {
                this.DrawImageAndText(graphics, bounds);
            }

            public Size MeasureButton(Graphics graphics, Rectangle bounds) {
                return this.CalculateContentSize(graphics, bounds);
            }
        }
    }

    [STATestClass]
    public class TestOlvFormatting : WinFormsTestFixture
    {
        [TestInitialize]
        public void InitEachTest() {
            this.olv = GetObjectListView();
            this.olv.UseAlternatingBackColors = false;
            this.olv.UseHyperlinks = false;
            this.olv.RowFormatter = null;
            this.olv.HyperlinkStyle = null;
            foreach (OLVColumn column in this.olv.Columns)
            {
                column.Hyperlink = false;
            }
        }

        [TestMethod]
        public void TestNoFormatting() {
            this.olv.HyperlinkStyle = null;
            this.olv.SetObjects(PersonDb.All);
            for (int i = 0; i < this.olv.GetItemCount(); i++) {
                Assert.AreEqual(this.olv.ForeColor, this.olv.GetItem(i).ForeColor);
                Assert.AreEqual(this.olv.BackColor, this.olv.GetItem(i).BackColor);
            }
        }

        [TestMethod]
        public void TestAlternateBackColors() {
            this.olv.UseAlternatingBackColors = true;
            this.olv.AlternateRowBackColor = Color.Pink;

            this.olv.SetObjects(PersonDb.All);
            for (int i = 0; i < this.olv.GetItemCount(); i++) {
                if ((i % 2) == 0)
                {
                    Assert.AreEqual(this.olv.BackColor, this.olv.GetItem(i).BackColor);
                }
                else
                {
                    Assert.AreEqual(this.olv.AlternateRowBackColor, this.olv.GetItem(i).BackColor);
                }
            }
        }

        [TestMethod]
        public void TestRowFormatter() {
            Color testForeColor = Color.Yellow;
            Color testBackColor = Color.Violet;

            this.olv.RowFormatter = delegate(OLVListItem olvi) {
                olvi.ForeColor = testForeColor;
                olvi.BackColor = testBackColor;
            };
            this.olv.SetObjects(PersonDb.All);
            for (int i = 0; i < this.olv.GetItemCount(); i++) {
                Assert.AreEqual(testForeColor, this.olv.GetItem(i).ForeColor);
                Assert.AreEqual(testBackColor, this.olv.GetItem(i).BackColor);
            }
        }

        [TestMethod]
        public void TestFormatRowEvent() {
            this.olv.FormatRow += new EventHandler<FormatRowEventArgs>(olv_FormatRow1);
            this.olv.SetObjects(PersonDb.All);
            for (int i = 0; i < this.olv.GetItemCount(); i++) {
                Assert.AreEqual(this.testForeColor, this.olv.GetItem(i).ForeColor);
                Assert.AreEqual(this.testBackColor, this.olv.GetItem(i).BackColor);
            }
            this.olv.FormatRow -= new EventHandler<FormatRowEventArgs>(olv_FormatRow1);
        }
        Color testForeColor = Color.Yellow;
        Color testBackColor = Color.Violet;

        void olv_FormatRow1(object sender, FormatRowEventArgs e) {
            e.Item.ForeColor = this.testForeColor;
            e.Item.BackColor = this.testBackColor;
        }

        [TestMethod]
        public void TestFormatCellEvent() {
            this.olv.FormatRow += new EventHandler<FormatRowEventArgs>(olv_FormatRow2);
            this.olv.FormatCell += new EventHandler<FormatCellEventArgs>(olv_FormatCell);
            this.olv.SetObjects(PersonDb.All);
            for (int i = 0; i < this.olv.GetItemCount(); i++) {
                if (i % 2 == 0) {
                    Assert.IsFalse(this.olv.GetItem(i).UseItemStyleForSubItems);
                    for (int j = 0; j < this.olv.Columns.Count; j++) {
                        Assert.AreEqual(this.testCellForeColor, this.olv.GetItem(i).SubItems[j].ForeColor);
                        Assert.AreEqual(this.testCellBackColor, this.olv.GetItem(i).SubItems[j].BackColor);
                    }
                } else {
                    Assert.IsTrue(this.olv.GetItem(i).UseItemStyleForSubItems);
                    Assert.AreEqual(this.olv.ForeColor, this.olv.GetItem(i).ForeColor);
                    Assert.AreEqual(this.olv.BackColor, this.olv.GetItem(i).BackColor);
                }
            }
            this.olv.FormatRow -= new EventHandler<FormatRowEventArgs>(olv_FormatRow2);
            this.olv.FormatCell -= new EventHandler<FormatCellEventArgs>(olv_FormatCell);
        }
        Color testCellForeColor = Color.Aquamarine;
        Color testCellBackColor = Color.BlanchedAlmond;

        void olv_FormatCell(object sender, FormatCellEventArgs e) {
            e.SubItem.ForeColor = this.testCellForeColor;
            e.SubItem.BackColor = this.testCellBackColor;
        }

        void olv_FormatRow2(object sender, FormatRowEventArgs e) {
            e.UseCellFormatEvents = (e.RowIndex % 2 == 0);
        }

        [TestMethod]
        public void TestFormatRowAndCell_CellTakesPriority() {
            Color formatCellForeground = Color.PaleGoldenrod;
            Color formatRowForeground = Color.Pink;
            Color formatRowBackground = Color.Fuchsia;

            EventHandler<FormatRowEventArgs> olvOnFormatRow = delegate(object sender, FormatRowEventArgs args) {
                args.Item.ForeColor = formatRowForeground;
                args.Item.BackColor = formatRowBackground;
                args.UseCellFormatEvents = true;
            };
            EventHandler<FormatCellEventArgs> olvOnFormatCell = delegate(object sender, FormatCellEventArgs args) {
                args.SubItem.ForeColor = formatCellForeground;
            };

            this.olv.FormatRow += olvOnFormatRow;
            this.olv.FormatCell += olvOnFormatCell;

            this.olv.SetObjects(PersonDb.All);

            for (int i = 0; i < this.olv.GetItemCount(); i++) {
                for (int j = 0; j < this.olv.Columns.Count; j++) {
                    Assert.AreEqual(formatCellForeground, this.olv.GetItem(i).SubItems[j].ForeColor);
                    Assert.AreEqual(formatRowBackground, this.olv.GetItem(i).SubItems[j].BackColor);
                }
            }
            this.olv.FormatRow -= olvOnFormatRow;
            this.olv.FormatCell -= olvOnFormatCell;
        }


        [TestMethod]
        public void TestHyperlinks()
        {
            this.olv.UseHyperlinks = true;
            this.olv.HyperlinkStyle = new HyperlinkStyle();
            this.olv.HyperlinkStyle.Normal.ForeColor = Color.Thistle;
            this.olv.HyperlinkStyle.Normal.BackColor = Color.SpringGreen;
            this.olv.HyperlinkStyle.Normal.FontStyle = FontStyle.Bold;

            foreach (OLVColumn column in this.olv.Columns)
            {
                column.Hyperlink = (column.Index < 2);
            }

            this.olv.SetObjects(PersonDb.All);
            for (int j = 0; j < this.olv.GetItemCount(); j++)
            {
                OLVListItem item = this.olv.GetItem(j);
                for (int i = 0; i < this.olv.Columns.Count; i++)
                {
                    OLVColumn column = this.olv.GetColumn(i);
                    if (column.Hyperlink)
                    {
                        Assert.AreEqual(this.olv.HyperlinkStyle.Normal.ForeColor, item.SubItems[i].ForeColor);
                        Assert.AreEqual(this.olv.HyperlinkStyle.Normal.BackColor, item.SubItems[i].BackColor);
                        Assert.IsTrue(item.SubItems[i].Font.Bold);
                    }
                    else
                    {
                        Assert.AreEqual(this.olv.ForeColor, item.SubItems[i].ForeColor);
                        Assert.AreEqual(this.olv.BackColor, item.SubItems[i].BackColor);
                        Assert.IsFalse(item.SubItems[i].Font.Bold);
                    }
                }
            }
        }

        [TestMethod]
        public void TestHyperlinksAndFormatCell_HyperlinkHasPriority() {
            Color hyperlinkForeground = Color.Thistle;
            Color formatCellForeground = Color.PaleGoldenrod;
            Color formatCellBackground = Color.DarkKhaki;

            this.olv.UseCellFormatEvents = true;
            EventHandler<FormatCellEventArgs> olvOnFormatCell = delegate(object sender, FormatCellEventArgs args) {
                args.SubItem.ForeColor = formatCellForeground;
                args.SubItem.BackColor = formatCellBackground;
            };
            this.olv.FormatCell += olvOnFormatCell;

            this.olv.UseHyperlinks = true;
            this.olv.HyperlinkStyle = new HyperlinkStyle();
            this.olv.HyperlinkStyle.Normal.ForeColor = hyperlinkForeground;

            foreach (OLVColumn column in this.olv.Columns)
            {
                column.Hyperlink = column.Index < 2;
            }

            this.olv.SetObjects(PersonDb.All);

            try {
                for (int j = 0; j < this.olv.GetItemCount(); j++) {
                    OLVListItem item = this.olv.GetItem(j);
                    for (int i = 0; i < this.olv.Columns.Count; i++) {
                        OLVColumn column = this.olv.GetColumn(i);
                        if (column.Hyperlink) {
                            Assert.AreEqual(hyperlinkForeground, item.SubItems[i].ForeColor);
                        } else {
                            Assert.AreEqual(formatCellForeground, item.SubItems[i].ForeColor);
                        }
                        Assert.AreEqual(formatCellBackground, item.SubItems[i].BackColor);
                    }
                }
            }
            finally {
                this.olv.FormatCell -= olvOnFormatCell;
            }
        }

        protected virtual ObjectListView GetObjectListView() {
            return mainForm.objectListView1;
        }
        protected ObjectListView olv;
    }

    [STATestClass]
    public class TestFastOlvFormatting : TestOlvFormatting
    {
        protected override ObjectListView GetObjectListView() {
            return mainForm.fastObjectListView1;
        }
    }

    [STATestClass]
    public class TestTreeListViewFormatting : TestOlvFormatting
    {
        protected override ObjectListView GetObjectListView() {
            return mainForm.treeListView1;
        }
    }
}
