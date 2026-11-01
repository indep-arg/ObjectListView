using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThreadExceptionEventArgs = System.Threading.ThreadExceptionEventArgs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NETFRAMEWORK
using DemoComboBox = ObjectListViewDemo.ThemedComboBox;
#else
using DemoComboBox = System.Windows.Forms.ComboBox;
#endif

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestDemoTheme : ThemeTestFixture
    {
        private string originalDirectory;
        private Exception uiException;
        private VisualStyleState originalVisualStyles;
#if NET10_0_OR_GREATER
        private SystemColorMode originalWinFormsMode;
#endif

        [TestInitialize]
        public void InitializeDemo() {
            Application.ThreadException += this.OnUiException;
            this.originalDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            this.originalVisualStyles = Application.VisualStyleState;
            Application.EnableVisualStyles();
#if NET10_0_OR_GREATER
            this.originalWinFormsMode = Application.ColorMode;
#endif
            ObjectListViewDemo.DemoTheme.SetColorMode(ObjectListViewColorMode.Dark);
        }

        [TestCleanup]
        public void RestoreDemoEnvironment() {
            Directory.SetCurrentDirectory(this.originalDirectory);
#if NET10_0_OR_GREATER
            Application.SetColorMode(this.originalWinFormsMode);
#endif
            Application.ThreadException -= this.OnUiException;
            Application.VisualStyleState = this.originalVisualStyles;
            if (this.uiException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(this.uiException).Throw();
            }
        }

        private void OnUiException(object sender, ThreadExceptionEventArgs e) {
            this.uiException = e.Exception;
            Console.Error.WriteLine(e.Exception);
            foreach (var form in Application.OpenForms.OfType<ObjectListViewDemo.MainForm>().ToArray())
            {
                form.Close();
            }
        }

        [TestMethod]
        public void Test_StandardControlPalette_PreservesExplicitDemoStyles() {
            using (var panel = new Panel())
            using (var label = new Label { BackColor = Color.FromArgb(255, 255, 128) })
            using (var grid = new DataGridView())
            using (var button = new StockButtonProbe { Text = "Run" })
            using (var combo = new DemoComboBox { DropDownStyle = ComboBoxStyle.DropDownList })
            using (var page = new TabPage { UseVisualStyleBackColor = true })
            using (var tabs = new TabControl())
            using (var list = new ObjectListView {
                View = View.Details, ShowGroups = false, UseAlternatingBackColors = true,
                AlternateRowBackColor = Color.FromArgb(255, 240, 220)
            }) {
                var column = new OLVColumn("Name", "Name") { HeaderForeColor = Color.Black };
                list.AllColumns.Add(column);
                list.Columns.Add(column);
                tabs.TabPages.Add(page);
                panel.Controls.AddRange(new Control[] { label, grid, button, combo, tabs, list });
                ObjectListViewDemo.DemoTheme.ApplyInformationBanner(label);
                ObjectListViewDemo.DemoTheme.Apply(panel);
                list.SetObjects(new[] { new { Name = "First" }, new { Name = "Second" } });

                Assert.IsLessThan(0.5f, label.BackColor.GetBrightness());
                Assert.IsGreaterThan(0.5f, label.ForeColor.GetBrightness());
                Assert.IsGreaterThan(label.ForeColor.B, label.ForeColor.R);
                Assert.AreEqual(Color.FromArgb(255, 240, 220), list.GetItem(1).BackColor);
                Assert.AreEqual(Color.Black, column.HeaderForeColor);
                Assert.IsFalse(page.UseVisualStyleBackColor);
                Assert.AreEqual(ObjectListViewTheme.ControlColor, page.BackColor);
                Assert.AreEqual(ObjectListViewTheme.ControlLightColor, combo.BackColor);
                Assert.AreEqual(ObjectListViewTheme.TextColor, combo.ForeColor);
                combo.Items.Add("Details");
                combo.SelectedIndex = 0;
                using (var bitmap = new Bitmap(combo.Width, combo.Height)) {
                    combo.DrawToBitmap(bitmap, combo.ClientRectangle);
                    Assert.IsLessThan(0.5f, bitmap.GetPixel(combo.Width / 2, 2).GetBrightness());
                }
                Assert.IsFalse(grid.EnableHeadersVisualStyles);
                Assert.IsLessThan(0.5f, grid.ColumnHeadersDefaultCellStyle.BackColor.GetBrightness());
                Assert.IsGreaterThan(0.5f, grid.ColumnHeadersDefaultCellStyle.ForeColor.GetBrightness());
                Assert.AreEqual(ObjectListViewTheme.HighlightTextColor.ToArgb(), grid.DefaultCellStyle.SelectionForeColor.ToArgb());
                grid.Columns.Add("Name", "Name");
                grid.Rows.Add("Row");
                using (var bitmap = new Bitmap(grid.Width, grid.Height)) {
                    grid.DrawToBitmap(bitmap, grid.ClientRectangle);
                    Assert.IsLessThan(0.5f, bitmap.GetPixel(grid.RowHeadersWidth + 20, 5).GetBrightness());
                    Assert.IsLessThan(0.5f, bitmap.GetPixel(5, grid.ColumnHeadersHeight + 10).GetBrightness());
                }
#if NETFRAMEWORK
                using (var bitmap = new Bitmap(combo.Width, combo.Height)) {
                    combo.DrawToBitmap(bitmap, combo.ClientRectangle);
                    Assert.IsLessThan(0.5f, bitmap.GetPixel(combo.Width / 2, 2).GetBrightness());
                    Assert.IsLessThan(0.5f, bitmap.GetPixel(combo.Width - 5, 2).GetBrightness());
                }
                using (var bitmap = new Bitmap(button.Width, button.Height)) {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        button.Draw(graphics);
                    }

                    Assert.AreEqual(ObjectListViewTheme.ButtonColor.ToArgb(), bitmap.GetPixel(button.Width / 2, 2).ToArgb());
                }
#else
                Assert.AreEqual(DrawMode.Normal, combo.DrawMode);
                Assert.AreEqual(FlatStyle.Standard, combo.FlatStyle);
                Assert.AreEqual(FlatStyle.Standard, button.FlatStyle);
#endif
            }
        }

        [TestMethod]
        public void Test_InformationBannerRole_DoesNotDependOnItsOriginalColor() {
            using (var banner = new Label { BackColor = Color.Purple })
            using (var ordinaryLabel = new Label { BackColor = Color.FromArgb(255, 255, 128) }) {
                ObjectListViewDemo.DemoTheme.ApplyInformationBanner(banner);
                ObjectListViewDemo.DemoTheme.Apply(ordinaryLabel);
                Assert.IsLessThan(0.5f, banner.BackColor.GetBrightness());
                Assert.IsGreaterThan(banner.ForeColor.B, banner.ForeColor.R);
                Assert.AreEqual(Color.FromArgb(255, 255, 128), ordinaryLabel.BackColor);
            }
        }

        [TestMethod]
        public void Test_ClassicMode_PreservesDemoBannerColors() {
            ObjectListViewDemo.DemoTheme.SetColorMode(ObjectListViewColorMode.Light);
            using (var label = new Label { BackColor = Color.FromArgb(255, 255, 128) }) {
                ObjectListViewDemo.DemoTheme.ApplyInformationBanner(label);
                Assert.AreEqual(Color.FromArgb(255, 255, 128), label.BackColor);
            }
        }

        [TestMethod]
        public void Test_CheckBoxImages_PreserveLateLoadedDesignerImagesAndAllStates() {
            using (var source = new ImageList()) {
                using (var list = new ObjectListView()) {
                    var initialization = (ISupportInitialize)list;
                    initialization.BeginInit();
                    list.SmallImageList = source;
                    list.UseSubItemCheckBoxes = true;
                    var resources = new ComponentResourceManager(typeof(ObjectListViewDemo.TabComplexExample));
                    source.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
                    for (int i = 0; i < 18; i++)
                    {
                        source.Images.SetKeyName(i, "asset" + i);
                    }

                    int expectedPixel;
                    using (var original = (Bitmap)source.Images["asset17"])
                    {
                        expectedPixel = original.GetPixel(8, 8).ToArgb();
                    }

                    initialization.EndInit();

                    Assert.AreEqual(17, list.SmallImageList.Images.IndexOfKey("asset17"));
                    Assert.AreEqual(18, source.Images.Count);
                    using (var preserved = (Bitmap)list.SmallImageList.Images["asset17"])
                    {
                        Assert.AreEqual(expectedPixel, preserved.GetPixel(8, 8).ToArgb());
                    }

                    foreach (string key in new[] { ObjectListViewBase.CHECKED_KEY, ObjectListViewBase.UNCHECKED_KEY, ObjectListViewBase.INDETERMINATE_KEY }) {
                        using (Image state = list.SmallImageList.Images[key])
                        {
                            Assert.AreEqual(new Size(16, 16), state.Size);
                        }
                    }
                }
                Assert.AreEqual(18, source.Images.Count);
            }
        }

        [TestMethod]
        public void Test_MainForm_UsesActualRuntimeControlsAndExplicitStyles() {
            using (var form = new ObjectListViewDemo.MainForm { Opacity = 0, ShowInTaskbar = false, Size = new Size(1450, 1150) }) {
                form.Show();
                var tabs = form.Controls.OfType<TabControl>().Single();
                Assert.AreEqual(typeof(ObjectListViewDemo.MainForm).Assembly, typeof(ObjectListViewDemo.DemoTheme).Assembly);
                string[] bannerNames = { "label1", "label2", "label3", "label5", "label26", "label32", "label42", "label12", "label30", "label1" };
                for (int index = 0; index < tabs.TabPages.Count; index++) {
                    tabs.SelectedIndex = index;
                    Application.DoEvents();
                    Control[] controls = Descendants(tabs.SelectedTab).ToArray();
                    var banner = controls.OfType<Label>().Single(label => label.Name == bannerNames[index]);
                    Assert.IsLessThan(0.5f, banner.BackColor.GetBrightness());
                    Assert.IsGreaterThan(0.5f, banner.ForeColor.GetBrightness());
                    foreach (ObjectListViewBase list in controls.OfType<ObjectListViewBase>()) {
                        if (list.UseAlternatingBackColors && list.GetItemCount() > 1)
                        {
                            Assert.IsLessThan(0.5f, list.GetItem(1).BackColor.GetBrightness());
                        }
                    }
#if NET10_0_OR_GREATER
                    foreach (ComboBox combo in controls.OfType<ComboBox>()) {
                        Assert.AreEqual(typeof(ComboBox), combo.GetType());
                        Assert.AreEqual(DrawMode.Normal, combo.DrawMode);
                    }
#endif
                }
#if NETFRAMEWORK
                Assert.IsInstanceOfType<ObjectListViewDemo.ThemedTabControl>(tabs);
#else
                Assert.AreEqual(typeof(TabControl), tabs.GetType());
#endif
                var simpleList = tabs.TabPages[0].Controls.OfType<ObjectListViewDemo.TabSimpleExample>().Single().ListView;
                Assert.IsGreaterThan(0.5f, simpleList.GetColumn(2).HeaderForeColor.GetBrightness());
                Assert.AreEqual(ObjectListViewTheme.TextColor, simpleList.GetColumn(3).HeaderForeColor);
                var complexList = tabs.TabPages[1].Controls.OfType<ObjectListViewDemo.TabComplexExample>().Single().ListView;
                Assert.AreEqual(2, complexList.SmallImageList.Images.IndexOfKey("user"));
                Assert.AreEqual(6, complexList.SmallImageList.Images.IndexOfKey("music"));
                Assert.AreEqual(12, complexList.SmallImageList.Images.IndexOfKey("star"));
                Assert.IsGreaterThan(0.5f, complexList.HyperlinkStyle.Normal.ForeColor.GetBrightness());
                foreach (GroupBox group in Descendants(form).OfType<GroupBox>()) {
                    Assert.IsGreaterThan(0.5f, group.ForeColor.GetBrightness(), group.Name);
                    using (var bitmap = new Bitmap(group.Width, group.Height)) {
                        group.DrawToBitmap(bitmap, group.ClientRectangle);
                        Assert.IsTrue(ContainsColor(bitmap, group.ForeColor,
                            new Rectangle(0, 0, group.Width, Math.Min(group.Height, group.Font.Height))), group.Name);
                    }
                }
            }
        }

        [TestMethod]
        public void Test_BusinessCardRendering_IsReusableAndRestoresGraphicsState() {
            using (var list = new ObjectListView { View = View.Tile, ShowGroups = false })
            using (var renderer = new ObjectListViewDemo.BusinessCardRenderer())
            using (var bitmap = new Bitmap(450, 300))
            using (var graphics = Graphics.FromImage(bitmap)) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                list.Columns.Add(new OLVColumn("Occupation", "Occupation") { IsTileViewColumn = true });
                var model = new ObjectListViewDemo.Models.Person("Ada") { Occupation = "Programmer", Photo = "missing" };
                list.SetObjects(new[] { model });
                list.CreateControl();
                var item = list.GetItem(0);
                Rectangle bounds = new Rectangle(10, 10, 400, 240);
                Color configuredHeader = renderer.HeaderBackColor;
                for (int i = 0; i < 20; i++) {
                    item.Selected = i % 2 == 0;
                    var args = new DrawListViewItemEventArgs(graphics, item, bounds, 0,
                        item.Selected ? ListViewItemStates.Selected : ListViewItemStates.Default);
                    Assert.IsTrue(renderer.RenderItem(args, graphics, bounds, model));
                }
                Assert.AreEqual(configuredHeader, renderer.HeaderBackColor);
                graphics.SetClip(new Rectangle(5, 5, 430, 280));
                graphics.TranslateTransform(2, 3);
                RectangleF clip = graphics.ClipBounds;
                using (var transform = graphics.Transform) {
                    renderer.DrawBusinessCard(graphics, bounds, model, list, item);
                    Assert.AreEqual(clip, graphics.ClipBounds);
                    using (var after = graphics.Transform)
                    {
                        CollectionAssert.AreEqual(transform.Elements, after.Elements);
                    }
                }
            }
        }

        [TestMethod]
        public void Test_ThemeMenu_ReloadsTheActualDemoAndPreservesWindowAndTab() {
            Exception failure = null;
            int stage = 0;
            int threadExits = 0;
            DateTime deadline = DateTime.UtcNow.AddSeconds(30);
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            Rectangle bounds = new Rectangle(workingArea.Left + 20, workingArea.Top + 20,
                Math.Min(1450, workingArea.Width - 40), Math.Min(1150, workingArea.Height - 40));
            using (var timer = new Timer { Interval = 100 }) {
                timer.Tick += delegate {
                    timer.Stop();
                    try {
                        if (DateTime.UtcNow > deadline)
                        {
                            throw new TimeoutException("The demo did not complete its theme reload.");
                        }

                        var form = Application.OpenForms.OfType<ObjectListViewDemo.MainForm>().SingleOrDefault();
                        if (form == null || !form.Visible || !form.IsHandleCreated) {
                            timer.Start();
                            return;
                        }
                        form.Opacity = 0;
                        form.ShowInTaskbar = false;
                        var tabs = form.Controls.OfType<TabControl>().Single();
                        var menu = form.Controls.OfType<StatusStrip>().Single().Items.OfType<ToolStripDropDownButton>().Single();
                        var items = menu.DropDownItems.OfType<ToolStripMenuItem>().ToArray();
                        ObjectListViewColorMode[] modes = { ObjectListViewColorMode.System, ObjectListViewColorMode.Dark, ObjectListViewColorMode.Light, ObjectListViewColorMode.System };
                        int[] checkedItems = { 0, 2, 1, 0 };
                        Assert.AreEqual(modes[stage], ObjectListViewTheme.ColorMode);
                        Assert.AreEqual(0, threadExits, "Theme changes must not stop the application's message loop.");
                        Assert.IsTrue(items[checkedItems[stage]].Checked);
                        Assert.AreEqual(1, items.Count(item => item.Checked));
                        if (stage == 0) {
                            form.Bounds = bounds;
                            bounds = form.Bounds;
                            tabs.SelectedIndex = 2;
                        } else {
                            Assert.AreEqual(bounds, form.Bounds);
                            Assert.AreEqual(2, tabs.SelectedIndex);
                        }
                        if (stage == 3) {
                            form.Close();
                            return;
                        }
                        int[] nextItems = { 2, 1, 0 };
                        int next = nextItems[stage++];
                        items[next].PerformClick();
                        timer.Start();
                    } catch (Exception ex) {
                        failure = ex;
                        foreach (Form form in Application.OpenForms.Cast<Form>().ToArray())
                        {
                            form.Close();
                        }

                        Application.ExitThread();
                    }
                };
                EventHandler threadExited = delegate { threadExits++; };
                Application.ThreadExit += threadExited;
                timer.Start();
                try {
                    ObjectListViewDemo.MainForm.RunDemo();
                } finally {
                    timer.Stop();
                    Application.ThreadExit -= threadExited;
                }
            }
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }

            Assert.AreEqual(3, stage);
            Assert.AreEqual(1, threadExits);
        }

        private static IEnumerable<Control> Descendants(Control parent) {
            foreach (Control child in parent.Controls) {
                yield return child;
                foreach (Control descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }

#if NETFRAMEWORK
        [TestMethod]
        public void Test_FrameworkCheckBoxes_UseTheirGroupBackground() {
            using (var group = new GroupBox { Text = "Settings" })
            using (var first = new CheckBox { Text = "Groups" })
            using (var second = new CheckBox { Text = "Item Count" })
            using (var third = new CheckBox { Text = "Hot Item", Checked = true }) {
                group.Controls.AddRange(new Control[] { first, second, third });
                ObjectListViewDemo.DemoTheme.Apply(group);
                Assert.AreEqual(group.BackColor, first.BackColor);
                Assert.AreEqual(group.BackColor, second.BackColor);
                Assert.AreEqual(group.BackColor, third.BackColor);
            }
        }
#endif

#if NETFRAMEWORK
        [TestMethod]
        public void Test_DarkControls_DoNotEraseTheirBackgroundWithWhite() {
            using (var tabs = new TabPaintProbe())
            using (var combo = new ComboPaintProbe { DropDownStyle = ComboBoxStyle.DropDownList })
            using (var bitmap = new Bitmap(200, 100))
            using (var graphics = Graphics.FromImage(bitmap)) {
                tabs.TabPages.Add("First");
                tabs.CreateControl();
                combo.CreateControl();
                graphics.Clear(Color.Black);
                IntPtr hdc = graphics.GetHdc();
                try {
                    Message tabMessage = Message.Create(tabs.Handle, 0x14, hdc, IntPtr.Zero);
                    tabs.Send(ref tabMessage);
                    Message comboMessage = Message.Create(combo.Handle, 0x14, hdc, IntPtr.Zero);
                    combo.Send(ref comboMessage);
                } finally {
                    graphics.ReleaseHdc(hdc);
                }
                Assert.IsLessThan(0.5f, bitmap.GetPixel(10, 10).GetBrightness());
            }
        }
#endif

#if NETFRAMEWORK
        [TestMethod]
        public void Test_FrameworkTabs_UseStockLayoutWithDarkBordersAndNoUnderline() {
            using (var tabs = new ObjectListViewDemo.ThemedTabControl { Size = new Size(240, 100) })
            using (var bitmap = new Bitmap(240, 100)) {
                tabs.TabPages.Add("First");
                tabs.TabPages.Add("Second");
                tabs.CreateControl();
                tabs.DrawToBitmap(bitmap, tabs.ClientRectangle);
                Rectangle first = tabs.GetTabRect(0);
                Rectangle second = tabs.GetTabRect(1);
                Size textSize = TextRenderer.MeasureText("First", tabs.Font, Size.Empty,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                Assert.IsGreaterThanOrEqualTo(textSize.Width + 12, first.Width);
                Assert.IsGreaterThanOrEqualTo(textSize.Height + 8, first.Height);
                Assert.AreNotEqual(bitmap.GetPixel(first.Left + 3, first.Top + 3).ToArgb(),
                    bitmap.GetPixel(second.Left + 3, second.Top + 3).ToArgb());
                Rectangle firstHeader = new Rectangle(first.Left, 0, first.Width, tabs.DisplayRectangle.Top);
                Rectangle secondHeader = new Rectangle(second.Left, 0, second.Width, tabs.DisplayRectangle.Top);
                Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.ControlDarkColor, firstHeader));
                Assert.IsFalse(ContainsColor(bitmap, ObjectListViewTheme.AccentColor, firstHeader));
                Assert.IsLessThan(0.5f, bitmap.GetPixel(first.Right - 1, first.Top + 1).GetBrightness());
                Assert.IsLessThan(0.5f, bitmap.GetPixel(second.Right - 1, second.Top + 1).GetBrightness());
                tabs.SelectedIndex = 1;
                tabs.DrawToBitmap(bitmap, tabs.ClientRectangle);
                Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.ControlDarkColor, secondHeader));
                Assert.IsFalse(ContainsColor(bitmap, ObjectListViewTheme.AccentColor, secondHeader));
            }
        }
#endif

        private sealed class StockButtonProbe : Button {
            public void Draw(Graphics graphics) {
                using (var args = new PaintEventArgs(graphics, this.ClientRectangle))
                {
                    this.OnPaint(args);
                }
            }
        }

#if NETFRAMEWORK
        private sealed class TabPaintProbe : ObjectListViewDemo.ThemedTabControl {
            public void Send(ref Message message) {
                this.WndProc(ref message);
            }
        }
#endif

#if NETFRAMEWORK
        private sealed class ComboPaintProbe : ObjectListViewDemo.ThemedComboBox {
            public void Send(ref Message message) {
                this.WndProc(ref message);
            }
        }
#endif
    }
}
