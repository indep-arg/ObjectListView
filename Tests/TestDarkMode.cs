using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestDarkMode : ThemeTestFixture
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_RejectedRowHeightImageCopy_IsDisposedAndKeepsThePreviousList(bool attachBeforeThrow) {
            using (var source = new ImageList())
            using (var bitmap = new Bitmap(16, 16))
            using (var list = new RejectingImageListView { View = View.Details }) {
                source.Images.Add(bitmap);
                list.SmallImageList = source;
                list.RowHeight = 32;
                ImageList previous = list.BaseSmallImageList;
                list.RejectReplacement = true;
                list.AttachBeforeThrow = attachBeforeThrow;
                Assert.ThrowsExactly<InvalidOperationException>(() => list.RowHeight = 40);
                Assert.IsTrue(list.CopyDisposed);
                Assert.AreSame(previous, list.BaseSmallImageList);
                Assert.AreSame(source, list.SmallImageList);
                list.Dispose();
                Assert.IsTrue(list.CopyDisposed);
                Assert.AreEqual(1, source.Images.Count);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_RejectedCheckboxImageCopy_IsDisposedAndCanBeRetried(bool attachBeforeThrow) {
            using (var source = new ImageList())
            using (var bitmap = new Bitmap(16, 16))
            using (var list = new RejectingImageListView { View = View.Details }) {
                source.Images.Add("asset", bitmap);
                list.SmallImageList = source;
                list.RejectReplacement = true;
                list.AttachBeforeThrow = attachBeforeThrow;
                Assert.ThrowsExactly<InvalidOperationException>(() => list.UseSubItemCheckBoxes = true);
                Assert.IsTrue(list.CopyDisposed);
                Assert.AreSame(source, list.SmallImageList);
                Assert.AreSame(source, list.BaseSmallImageList);

                list.RejectReplacement = false;
                list.SetupSubItemCheckBoxes();
                Assert.AreEqual(0, list.SmallImageList.Images.IndexOfKey("asset"));
                Assert.IsTrue(list.SmallImageList.Images.ContainsKey(ObjectListViewBase.CHECKED_KEY));
                Assert.AreEqual(1, source.Images.Count);
            }
        }

        [TestMethod]
        public void Test_RowHeightImageCopies_AreDisposedWithoutDisposingTheSource() {
            using (var source = new ImageList())
            using (var bitmap = new Bitmap(16, 16)) {
                source.Images.Add(bitmap);
                bool sourceDisposed = false;
                source.Disposed += delegate { sourceDisposed = true; };
                bool currentDisposed = false;
                using (var list = new ObjectListView { View = View.Details, SmallImageList = source, RowHeight = 32 }) {
                    ImageList previous = list.BaseSmallImageList;
                    bool previousDisposed = false;
                    previous.Disposed += delegate { previousDisposed = true; };
                    list.RowHeight = 40;
                    Assert.IsTrue(previousDisposed);
                    Assert.IsFalse(sourceDisposed);
                    list.BaseSmallImageList.Disposed += delegate { currentDisposed = true; };
                }
                Assert.IsTrue(currentDisposed);
                Assert.IsFalse(sourceDisposed);
            }
        }

        [TestMethod]
        public void Test_DefaultRowColors_UseDarkPaletteAndPreserveOverrides() {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false, UseAlternatingBackColors = true }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                list.SetObjects(new[] { new { Name = "First" }, new { Name = "Second" } });
                Assert.IsLessThan(0.5f, list.BackColor.GetBrightness());
                Assert.IsGreaterThan(0.5f, list.ForeColor.GetBrightness());
                Assert.AreEqual(ObjectListViewTheme.ControlColor, list.GetItem(1).BackColor);
                Assert.IsLessThan(0.5f, list.HotItemStyleOrDefault.BackColor.GetBrightness());

                list.AlternateRowBackColor = Color.Purple;
                list.HotItemStyle = new HotItemStyle { BackColor = Color.Teal };
                list.SelectedBackColor = Color.Navy;
                list.SelectedForeColor = Color.Yellow;
                list.SetObjects(new[] { new { Name = "First" }, new { Name = "Second" } });
                Assert.AreEqual(Color.Purple, list.GetItem(1).BackColor);
                Assert.AreEqual(Color.Teal, list.HotItemStyleOrDefault.BackColor);
                Assert.AreEqual(Color.Navy, list.SelectedBackColorOrDefault);
                Assert.AreEqual(Color.Yellow, list.SelectedForeColorOrDefault);
            }
        }
        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void Test_CustomHeader_UsesDarkBackgroundAndReadableText(bool hot, bool pressed) {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false })
            using (var bitmap = new Bitmap(200, 40))
            using (var graphics = Graphics.FromImage(bitmap)) {
                var column = new OLVColumn("Header", "Name") { Width = 200 };
                list.Columns.Add(column);
                list.CreateControl();
                var header = new DarkHeaderProbe(list);
                try {
                    Rectangle bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    header.DrawBackground(graphics, bounds, hot, pressed, new HeaderStateStyle());
                    Color expected = pressed ? ObjectListViewTheme.ControlDarkColor :
                        hot ? ObjectListViewTheme.ControlLightColor : ObjectListViewTheme.ControlColor;
                    Assert.AreEqual(expected.ToArgb(), bitmap.GetPixel(2, 2).ToArgb());
                    header.DrawText(graphics, bounds, column, new HeaderStateStyle());
                    Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.TextColor));

                    header.DrawBackground(graphics, bounds, hot, pressed,
                        new HeaderStateStyle { BackColor = Color.Purple });
                    Assert.AreEqual(Color.Purple.ToArgb(), bitmap.GetPixel(2, 2).ToArgb());
                    header.DrawText(graphics, bounds, column, new HeaderStateStyle { ForeColor = Color.Black });
                    Assert.IsTrue(ContainsColor(bitmap, Color.Black));
                } finally {
                    header.ReleaseHandle();
                }
            }
        }
        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public void Test_HeaderPainting_RemainsDarkAfterHandleRecreation(bool explorerTheme, bool wordWrap) {
            using (var list = new DarkListProbe {
                View = View.Details, ShowGroups = false, HeaderWordWrap = wordWrap,
                UseExplorerTheme = explorerTheme, Size = new Size(200, 100)
            })
            using (var bitmap = new Bitmap(200, 100)) {
                list.Columns.Add(new OLVColumn("Header", "Name") { Width = 180 });
                list.SetObjects(new[] { new { Name = "Row" } });
                list.CreateControl();
                Application.DoEvents();
                list.DrawToBitmap(bitmap, list.ClientRectangle);
                Assert.AreEqual(ObjectListViewTheme.ControlColor.ToArgb(), bitmap.GetPixel(100, 5).ToArgb());
                Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.TextColor, new Rectangle(0, 0, 180, 20)));
                Assert.AreEqual(ObjectListViewTheme.ControlColor.ToArgb(), bitmap.GetPixel(190, 5).ToArgb());

                list.RecreateTestHandle();
                Application.DoEvents();
                list.DrawToBitmap(bitmap, list.ClientRectangle);
                Assert.AreEqual(ObjectListViewTheme.ControlColor.ToArgb(), bitmap.GetPixel(100, 5).ToArgb());
                Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.TextColor, new Rectangle(0, 0, 180, 20)));
                Assert.AreEqual(!explorerTheme, list.OwnerDraw);
            }
        }

        [TestMethod]
        public void Test_Hyperlinks_HaveReadableDefaultsAndPreserveOverrides() {
            using (var style = new HyperlinkStyle()) {
                Assert.AreEqual(ObjectListViewTheme.LinkColor, style.Normal.ForeColor);
                Assert.IsGreaterThan(0.5f, style.Visited.ForeColor.GetBrightness());
                style.Normal.ForeColor = Color.Orange;
                style.Visited.ForeColor = Color.Yellow;
                using (var list = new ObjectListView { UseHyperlinks = true, HyperlinkStyle = style }) {
                    Assert.AreEqual(Color.Orange, list.HyperlinkStyle.Normal.ForeColor);
                    Assert.AreEqual(Color.Yellow, list.HyperlinkStyle.Visited.ForeColor);
                }
            }
        }

        [TestMethod]
        [DataRow(CheckBoxState.UncheckedNormal)]
        [DataRow(CheckBoxState.UncheckedHot)]
        [DataRow(CheckBoxState.UncheckedPressed)]
        [DataRow(CheckBoxState.UncheckedDisabled)]
        [DataRow(CheckBoxState.CheckedNormal)]
        [DataRow(CheckBoxState.CheckedDisabled)]
        [DataRow(CheckBoxState.MixedNormal)]
        public void Test_CheckBoxes_DoNotUseAWhiteFill(CheckBoxState state) {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false })
            using (var bitmap = new Bitmap(40, 40))
            using (var graphics = Graphics.FromImage(bitmap)) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                list.SetObjects(new[] { new { Name = "Row" } });
                var item = list.GetItem(0);
                var renderer = new DarkCheckBoxProbe {
                    ListView = list, Column = list.GetColumn(0), ListItem = item,
                    SubItem = item.GetSubItem(0), State = state, UseCustomCheckboxImages = false
                };
                graphics.Clear(Color.Magenta);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                renderer.Draw(graphics, new Rectangle(0, 0, 40, 40));
                Assert.IsLessThan(0.5f, bitmap.GetPixel(2, 15).GetBrightness());
                if (state == CheckBoxState.UncheckedNormal)
                {
                    Assert.IsGreaterThan(0.5f, bitmap.GetPixel(0, 15).GetBrightness());
                }

                if (state < CheckBoxState.CheckedNormal)
                {
                    Assert.IsFalse(ContainsColor(bitmap, Color.White));
                }
                else if (state != CheckBoxState.CheckedDisabled)
                {
                    Assert.IsTrue(ContainsColor(bitmap, Color.White));
                }

                if (state == CheckBoxState.CheckedNormal || state == CheckBoxState.MixedNormal)
                {
                    Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.AccentColor));
                }
            }
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public void Test_Buttons_KeepDarkFillAndImageContent(bool withImage, bool disabled) {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false })
            using (var image = new Bitmap(16, 16))
            using (var imageGraphics = Graphics.FromImage(image))
            using (var bitmap = new Bitmap(140, 50))
            using (var graphics = Graphics.FromImage(bitmap)) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                list.SetObjects(new[] { new { Name = "Start" } });
                var item = list.GetItem(0);
                if (disabled) {
                    list.DisableObject(item.RowObject);
                    item = list.GetItem(0);
                }
                imageGraphics.Clear(Color.Magenta);
                if (withImage)
                {
                    item.ImageSelector = image;
                }

                var renderer = new DarkButtonProbe {
                    ListView = list, Column = list.GetColumn(0), ListItem = item, SubItem = item.GetSubItem(0)
                };
                graphics.Clear(Color.Black);
                renderer.Draw(graphics, new Rectangle(5, 5, 130, 40));
                Assert.IsLessThan(0.5f, bitmap.GetPixel(7, 7).GetBrightness());
                Assert.IsTrue(ContainsColor(bitmap, disabled ? ObjectListViewTheme.ButtonDisabledTextColor : ObjectListViewTheme.ButtonTextColor));
                if (withImage)
                {
                    Assert.IsTrue(ContainsColor(bitmap, Color.Magenta));
                }

                if (!disabled)
                {
                    Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.ButtonColor));
                }
            }
        }

        [TestMethod]
        public void Test_TaskDescriptions_UseReadableDefaultsAndPreserveOverrides() {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false })
            using (var bitmap = new Bitmap(300, 100))
            using (var graphics = Graphics.FromImage(bitmap)) {
                list.Columns.Add(new OLVColumn("Title", "Title"));
                var model = new { Title = "Task", Description = "Readable description" };
                list.SetObjects(new[] { model });
                var item = list.GetItem(0);
                var renderer = new DescribedTaskRenderer {
                    ListView = list, Column = list.GetColumn(0), ListItem = item,
                    SubItem = item.GetSubItem(0), RowObject = model, DescriptionAspectName = "Description"
                };
                Rectangle bounds = new Rectangle(0, 0, 300, 100);
                var args = new DrawListViewSubItemEventArgs(graphics, bounds, item, item.GetSubItem(0),
                    0, 0, list.GetColumn(0), ListViewItemStates.Default);
                renderer.RenderSubItem(args, graphics, bounds, model);
                Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.SecondaryTextColor));
                renderer.DescriptionColor = Color.Orange;
                renderer.RenderSubItem(args, graphics, bounds, model);
                Assert.IsTrue(ContainsColor(bitmap, Color.Orange));
                var overlay = (TextOverlay)list.EmptyListMsgOverlay;
                Assert.IsLessThan(0.5f, overlay.BackColor.GetBrightness());
                Assert.IsGreaterThan(0.5f, overlay.TextColor.GetBrightness());
            }
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void Test_CellEditor_UsesDarkDefaultsWithoutReplacingCustomColors(bool customEditor, bool systemColors) {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false }) {
                var column = new OLVColumn("Name", "Name");
                Color customBackColor = systemColors ? SystemColors.Info : Color.Purple;
                Color customForeColor = systemColors ? SystemColors.InfoText : Color.Yellow;
                if (customEditor)
                {
                    column.EditorCreator = delegate { return new TextBox { BackColor = customBackColor, ForeColor = customForeColor }; };
                }

                list.Columns.Add(column);
                list.SetObjects(new[] { new { Name = "Row" } });
                list.CreateControl();
                Control editor = null;
                list.CellEditStarting += delegate(object sender, CellEditEventArgs e) {
                    editor = e.Control;
                    e.Cancel = true;
                };
                try {
                    list.StartCellEdit(list.GetItem(0), 0);
                    Assert.IsNotNull(editor);
                    Assert.AreEqual(customEditor ? customBackColor : ObjectListViewTheme.ControlLightColor, editor.BackColor);
                    Assert.AreEqual(customEditor ? customForeColor : ObjectListViewTheme.TextColor, editor.ForeColor);
                } finally {
                    if (editor != null)
                    {
                        editor.Dispose();
                    }
                }
            }
        }

        [TestMethod]
        public void Test_ExplicitRowColors_AreNotReplacedByThePalette() {
            using (var list = new ObjectListView {
                BackColor = Color.Purple, ForeColor = Color.Yellow, View = View.Details, ShowGroups = false
            }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                list.SetObjects(new[] { new { Name = "Row" } });
                Assert.AreEqual(Color.Purple, list.GetItem(0).BackColor);
                Assert.AreEqual(Color.Yellow, list.GetItem(0).ForeColor);
            }
        }

#if NET10_0_OR_GREATER
        [TestMethod]
        public void Test_StockPalette_UsesWinFormsSystemColors() {
            SystemColorMode original = Application.ColorMode;
            try {
                Application.SetColorMode(SystemColorMode.Dark);
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Default;
                using (var list = new ObjectListView { View = View.Details, ShowGroups = false, UseAlternatingBackColors = true }) {
                    list.Columns.Add(new OLVColumn("Name", "Name"));
                    list.SetObjects(new[] { new { Name = "First" }, new { Name = "Second" } });
                    Assert.AreEqual(SystemColors.Window.ToArgb(), list.GetItem(0).BackColor.ToArgb());
                    Assert.AreEqual(SystemColors.WindowText.ToArgb(), list.GetItem(0).ForeColor.ToArgb());
                    Assert.AreEqual(SystemColors.Control.ToArgb(), list.GetItem(1).BackColor.ToArgb());
                    Assert.AreEqual(SystemColors.Highlight.ToArgb(), list.SelectedBackColorOrDefault.ToArgb());
                    Assert.AreEqual(SystemColors.HighlightText.ToArgb(), list.SelectedForeColorOrDefault.ToArgb());
                }

            } finally {
                Application.SetColorMode(original);
            }
        }

        [TestMethod]
        public void Test_ExplicitLightPalette_OverridesDarkWinFormsRowAndHeaderDefaults() {
            SystemColorMode original = Application.ColorMode;
            try {
                Application.SetColorMode(SystemColorMode.Dark);
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
                using (var list = new ObjectListView { View = View.Details, ShowGroups = false, Size = new Size(200, 100) })
                using (var bitmap = new Bitmap(200, 100)) {
                    list.Columns.Add(new OLVColumn("Name", "Name") { Width = 180 });
                    list.SetObjects(new[] { new { Name = "Row" } });
                    list.CreateControl();
                    Application.DoEvents();
                    Assert.AreEqual(Color.White, list.GetItem(0).BackColor);
                    Assert.AreEqual(Color.Black, list.GetItem(0).ForeColor);
                    Assert.AreEqual(Color.FromArgb(240, 240, 240), list.UnfocusedSelectedBackColorOrDefault);
                    Assert.AreEqual(Color.Black, list.UnfocusedSelectedForeColorOrDefault);
                    list.UnfocusedSelectedForeColor = Color.Purple;
                    Assert.AreEqual(Color.Purple, list.UnfocusedSelectedForeColorOrDefault);
                    list.DrawToBitmap(bitmap, list.ClientRectangle);
                    Assert.IsGreaterThan(0.5f, bitmap.GetPixel(100, 5).GetBrightness());
                }
            } finally {
                Application.SetColorMode(original);
            }
        }

        [TestMethod]
        public void Test_DefaultPalette_StillFollowsWinFormsColorMode() {
            SystemColorMode original = Application.ColorMode;
            try {
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Default;
                Application.SetColorMode(SystemColorMode.Dark);
                using (var list = new ObjectListView()) {
                    Assert.IsLessThan(0.5f, list.BackColor.GetBrightness());
                    Assert.IsGreaterThan(0.5f, list.ForeColor.GetBrightness());
                }
            } finally {
                Application.SetColorMode(original);
            }
        }
#endif

        [TestMethod]
        public void Test_ClassicMode_RetainsRowDefaults() {
            ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Default;
            using (var list = new ObjectListView()) {
                Assert.AreEqual(Color.LemonChiffon, list.AlternateRowBackColorOrDefault);
                Assert.AreEqual(Color.FromArgb(224, 235, 253), list.HotItemStyleOrDefault.BackColor);
            }
            using (var style = new HyperlinkStyle()) {
                Assert.AreEqual(Color.Blue, style.Normal.ForeColor);
                Assert.AreEqual(Color.Purple, style.Visited.ForeColor);
            }

        }

        private sealed class RejectingImageListView : ObjectListView {
            public bool RejectReplacement;
            public bool AttachBeforeThrow;
            public bool CopyDisposed;

            public override ImageList BaseSmallImageList {
                get { return base.BaseSmallImageList; }
                set {
                    if (this.RejectReplacement) {
                        value.Disposed += delegate { this.CopyDisposed = true; };
                        if (this.AttachBeforeThrow)
                        {
                            base.BaseSmallImageList = value;
                        }

                        throw new InvalidOperationException("The replacement was rejected.");
                    }
                    base.BaseSmallImageList = value;
                }
            }
        }

        private sealed class DarkHeaderProbe : HeaderControl
        {
            public DarkHeaderProbe(ObjectListViewBase list) : base(list) { }

            public void DrawBackground(Graphics graphics, Rectangle bounds, bool hot, bool pressed, HeaderStateStyle style) {
                this.DrawUnthemedBackground(graphics, bounds, 0, pressed, hot, style);
            }

            public void DrawText(Graphics graphics, Rectangle bounds, OLVColumn column, HeaderStateStyle style) {
                this.DrawHeaderImageAndText(graphics, bounds, column, style);
            }
        }

        private sealed class DarkListProbe : ObjectListView
        {
            public void RecreateTestHandle() {
                this.RecreateHandle();
            }
        }

        private sealed class DarkCheckBoxProbe : BaseRenderer {
            public CheckBoxState State;
            protected override CheckBoxState GetCheckBoxState(CheckState state) {
                return this.State;
            }
            public void Draw(Graphics graphics, Rectangle bounds) {
                this.DrawCheckBox(graphics, bounds);
            }
        }

        private sealed class DarkButtonProbe : ColumnButtonRenderer {
            public void Draw(Graphics graphics, Rectangle bounds) {
                this.DrawImageAndText(graphics, bounds);
            }
        }
    }

}
