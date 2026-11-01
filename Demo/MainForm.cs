/*
 * ObjectListViewDemo - A simple demo to show the ObjectListView control
 *
 * User: Phillip Piper
 * Date: 15/10/2006 11:15 AM
 *
 * Change log:
 * 2015-06-12  JPP  COMPLETE REWRITE. Goal of rewrite is to make the code much easier to follow
 * 
 * 2009-07-04  JPP  Added ExampleVirtualDataSource for virtual list demo
 * [lots of stuff]
 * 2006-10-20  JPP  Added DataSet tab page
 * 2006-10-15  JPP  Initial version
 */

using System;
using System.Drawing;
using System.Windows.Forms;
using BrightIdeasSoftware;

namespace ObjectListViewDemo {

    public partial class MainForm {

        [STAThread]
        public static void Main(string[] args) {
#if !NETFRAMEWORK
            if (!Application.SetHighDpiMode(HighDpiMode.PerMonitorV2))
            {
                throw new InvalidOperationException("The demo must configure DPI awareness before initializing Windows Forms.");
            }
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            RunDemo();
        }

        internal static void RunDemo() {
            using (var context = new DemoApplicationContext())
            {
                Application.Run(context);
            }
        }

        private sealed class DemoApplicationContext : ApplicationContext {
            private ObjectListViewColorMode colorMode = ObjectListViewColorMode.System;
            private Rectangle? windowBounds;
            private FormWindowState windowState = FormWindowState.Normal;
            private int selectedTab;

            public DemoApplicationContext() {
                this.CreateMainForm();
            }

            private void CreateMainForm() {
                DemoTheme.SetColorMode(this.colorMode);
                var form = new MainForm();
                if (this.windowBounds.HasValue) {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Bounds = this.windowBounds.Value;
                    form.WindowState = this.windowState;
                    form.tabControl1.SelectedIndex = this.selectedTab;
                }
                form.FormClosing += delegate {
                    this.windowState = form.WindowState;
                    this.windowBounds = this.windowState == FormWindowState.Normal ? form.Bounds : form.RestoreBounds;
                    this.selectedTab = form.tabControl1.SelectedIndex;
                };
                this.MainForm = form;
            }

            protected override void OnMainFormClosed(object sender, EventArgs e) {
                var form = (MainForm)sender;
                ObjectListViewColorMode? requestedMode = form.requestedColorMode;
                if (!requestedMode.HasValue) {
                    base.OnMainFormClosed(sender, e);
                    return;
                }
                form.Dispose();
                this.colorMode = requestedMode.Value;
                this.CreateMainForm();
                this.MainForm.Show();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public MainForm() {
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();
            DemoTheme.Apply(this);
            InitializeExamples();
            InitializeThemeMenu();
        }

        private ObjectListViewColorMode? requestedColorMode;

        private void InitializeThemeMenu() {
            if (this.DesignMode)
            {
                return;
            }

            var themeMenu = new ToolStripDropDownButton("Theme") {
                Name = "themeMenu",
                ToolTipText = "Changing theme reloads the demo and resets example edits."
            };
            AddThemeMenuItem(themeMenu, "Follow system", ObjectListViewColorMode.System);
            AddThemeMenuItem(themeMenu, "Light", ObjectListViewColorMode.Light);
            AddThemeMenuItem(themeMenu, "Dark", ObjectListViewColorMode.Dark);
            this.statusStrip1.Items.Insert(0, themeMenu);
            themeMenu.DropDown.Renderer = this.statusStrip1.Renderer;
        }

        private void AddThemeMenuItem(ToolStripDropDownButton menu, string text, ObjectListViewColorMode mode) {
            var item = new ToolStripMenuItem(text) {
                Checked = ObjectListViewTheme.ColorMode == mode,
                ForeColor = ObjectListViewTheme.TextColor,
                ToolTipText = "Reload the demo using this theme; example edits will be reset."
            };
            item.Click += delegate {
                if (ObjectListViewTheme.ColorMode == mode)
                {
                    return;
                }

                this.requestedColorMode = mode;
                this.Close();
            };
            menu.DropDownItems.Add(item);
            if (item.Checked)
            {
                menu.Text = "Theme: " + text;
            }
        }

        void InitializeExamples() {
            // Use different font under Vista
            if (ObjectListView.IsVistaOrLater)
            {
                this.Font = new Font("Segoe UI", 9);
            }

            OLVDemoCoordinator coordinator = new OLVDemoCoordinator(this);

            this.tabSimple.Coordinator = coordinator;
            this.tabComplex.Coordinator = coordinator;
            this.tabDataSet.Coordinator = coordinator;
            this.tabFileExplorer1.Coordinator = coordinator;
            this.tabFastList1.Coordinator = coordinator;
            this.tabTreeListView1.Coordinator = coordinator;
            this.tabDataTreeListView1.Coordinator = coordinator;
            this.tabDragAndDrop1.Coordinator = coordinator;
            this.tabDescribedTask1.Coordinator = coordinator;

            // Printing tab is slightly different, since it needs to know about the ObjectListViews from the other tabs
            this.tabPrinting1.SimpleView = this.tabSimple.ListView;
            this.tabPrinting1.ComplexView = this.tabComplex.ListView;
            this.tabPrinting1.DataListView = this.tabDataSet.ListView;
            this.tabPrinting1.FileExplorerView = this.tabFileExplorer1.ListView;
            this.tabPrinting1.TreeListView = this.tabTreeListView1.ListView;
            this.tabPrinting1.Coordinator = coordinator;

            //this.tabControl1.SelectTab(this.tabDescribedTasks);
        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            if (tabControl1.TabPages[e.TabPageIndex].Name == "tabPagePrinting")
            {
                this.tabPrinting1.UpdatePrintPreview();
            }
        }
    }
}
