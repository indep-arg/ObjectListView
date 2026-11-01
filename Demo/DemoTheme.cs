using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BrightIdeasSoftware;

namespace ObjectListViewDemo {
    internal static class DemoTheme {
        internal static bool IsDarkMode {
            get { return ObjectListViewTheme.IsDarkMode; }
        }

        internal static void SetColorMode(ObjectListViewColorMode mode) {
            ObjectListViewTheme.ColorMode = mode;
#if NET10_0_OR_GREATER
            Application.SetColorMode(mode == ObjectListViewColorMode.Dark ? SystemColorMode.Dark :
                mode == ObjectListViewColorMode.System ? SystemColorMode.System : SystemColorMode.Classic);
#endif
        }

        internal static void ApplyInformationBanner(Label label) {
            if (!IsDarkMode)
            {
                return;
            }

            label.BackColor = Color.FromArgb(53, 42, 5);
            label.ForeColor = Color.FromArgb(255, 225, 138);
        }

        internal static void Apply(Control control) {
            if (!IsDarkMode)
            {
                return;
            }

#if NETFRAMEWORK
            if (control.BackColor.IsSystemColor) {
                control.BackColor = control is ListBox || control is ObjectListViewBase || control is DataGridView ?
                    ObjectListViewTheme.WindowColor :
                    control is TextBoxBase || control is ComboBox || control is UpDownBase ?
                    ObjectListViewTheme.ControlLightColor : ObjectListViewTheme.ControlColor;
            }
            if (control.ForeColor.IsSystemColor)
            {
                control.ForeColor = ObjectListViewTheme.TextColor;
            }

            if (control is GroupBox group) {
                group.Paint -= DrawGroupCaption;
                group.Paint += DrawGroupCaption;
            }
            if (control is ButtonBase button) {
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = button is CheckBox || button is RadioButton ?
                    (button.Parent == null ? ObjectListViewTheme.ControlColor : button.Parent.BackColor) :
                    ObjectListViewTheme.ControlLightColor;
                button.FlatAppearance.BorderColor = ObjectListViewTheme.ControlLightLightColor;
            }
            if (control is Form form)
            {
                form.HandleCreated += ApplyDarkTitleBar;
            }

            if (control is Button actionButton) {
                actionButton.FlatStyle = FlatStyle.Flat;
                actionButton.UseVisualStyleBackColor = false;
                actionButton.BackColor = ObjectListViewTheme.ButtonColor;
                actionButton.ForeColor = ObjectListViewTheme.ButtonTextColor;
                actionButton.FlatAppearance.BorderColor = ObjectListViewTheme.GrayTextColor;
                actionButton.FlatAppearance.MouseOverBackColor = ObjectListViewTheme.ButtonHoverColor;
                actionButton.FlatAppearance.MouseDownBackColor = ObjectListViewTheme.ButtonPressedColor;
            }
            if (control is ToolStrip strip)
            {
                strip.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            }
#endif
            if (control is ComboBox combo && combo.DropDownStyle == ComboBoxStyle.DropDownList) {
                combo.BackColor = ObjectListViewTheme.ControlLightColor;
                combo.ForeColor = ObjectListViewTheme.TextColor;
#if NETFRAMEWORK
                combo.FlatStyle = FlatStyle.Flat;
                combo.DrawMode = DrawMode.OwnerDrawFixed;
                combo.DrawItem += DrawComboBoxItem;
#endif
            }
            if (control is TabPage page) {
                page.UseVisualStyleBackColor = false;
                page.BackColor = ObjectListViewTheme.ControlColor;
            }
            if (control is DataGridView grid) {
                grid.EnableHeadersVisualStyles = false;
                grid.BackgroundColor = ObjectListViewTheme.WindowColor;
                grid.DefaultCellStyle.BackColor = ObjectListViewTheme.WindowColor;
                grid.DefaultCellStyle.ForeColor = ObjectListViewTheme.TextColor;
                grid.DefaultCellStyle.SelectionBackColor = ObjectListViewTheme.HighlightColor;
                grid.DefaultCellStyle.SelectionForeColor = ObjectListViewTheme.HighlightTextColor;
                grid.ColumnHeadersDefaultCellStyle.BackColor = ObjectListViewTheme.ControlColor;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = ObjectListViewTheme.TextColor;
                grid.RowHeadersDefaultCellStyle.BackColor = ObjectListViewTheme.ControlColor;
                grid.RowHeadersDefaultCellStyle.ForeColor = ObjectListViewTheme.TextColor;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = ObjectListViewTheme.HighlightColor;
                grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = ObjectListViewTheme.HighlightTextColor;
                grid.RowHeadersDefaultCellStyle.SelectionBackColor = ObjectListViewTheme.HighlightColor;
                grid.RowHeadersDefaultCellStyle.SelectionForeColor = ObjectListViewTheme.HighlightTextColor;
                grid.GridColor = ObjectListViewTheme.ControlDarkColor;
            }
#if NETFRAMEWORK
            if (control is ObjectListViewBase list)
            {
                list.BorderStyle = BorderStyle.FixedSingle;
            }
#endif
            foreach (Control child in control.Controls)
            {
                Apply(child);
            }
        }

#if NETFRAMEWORK
        private static void DrawGroupCaption(object sender, PaintEventArgs e) {
            GroupBox group = (GroupBox)sender;
            if (String.IsNullOrEmpty(group.Text))
            {
                return;
            }

            TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
            Size size = TextRenderer.MeasureText(e.Graphics, group.Text, group.Font, Size.Empty, flags);
            int inset = (int)Math.Ceiling(8 * e.Graphics.DpiX / 96f);
            Rectangle bounds = new Rectangle(inset, 0, size.Width, size.Height);
            TextRenderer.DrawText(e.Graphics, group.Text, group.Font, bounds, group.ForeColor, group.BackColor, flags);
        }

        private static void DrawComboBoxItem(object sender, DrawItemEventArgs e) {
            ComboBox combo = (ComboBox)sender;
            Color backColor = (e.State & DrawItemState.Selected) != 0 ? ObjectListViewTheme.HighlightColor : combo.BackColor;
            using (Brush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            string text = e.Index >= 0 ? combo.GetItemText(combo.Items[e.Index]) : combo.Text;
            TextRenderer.DrawText(e.Graphics, text, e.Font, e.Bounds,
                combo.Enabled ? combo.ForeColor : ObjectListViewTheme.GrayTextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            e.DrawFocusRectangle();
        }

        private static void ApplyDarkTitleBar(object sender, EventArgs e) {
            if (Environment.OSVersion.Version.Major < 6)
            {
                return;
            }

            int enabled = ObjectListViewTheme.IsDarkMode ? 1 : 0;
            int result = DwmSetWindowAttribute(((Form)sender).Handle, 20, ref enabled, sizeof(int));
            if (result != 0)
            {
                Trace.TraceInformation("This Windows version did not accept dark title-bar styling (HRESULT {0:X8}).", result);
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

        private sealed class DarkColorTable : ProfessionalColorTable {
            public DarkColorTable() { this.UseSystemColors = false; }
            public override Color ToolStripDropDownBackground { get { return ObjectListViewTheme.MenuColor; } }
            public override Color MenuItemSelected { get { return ObjectListViewTheme.MenuHighlightColor; } }
            public override Color MenuItemSelectedGradientBegin { get { return ObjectListViewTheme.MenuHighlightColor; } }
            public override Color MenuItemSelectedGradientEnd { get { return ObjectListViewTheme.MenuHighlightColor; } }
            public override Color MenuItemPressedGradientBegin { get { return ObjectListViewTheme.MenuHighlightColor; } }
            public override Color MenuItemPressedGradientMiddle { get { return ObjectListViewTheme.MenuHighlightColor; } }
            public override Color MenuItemPressedGradientEnd { get { return ObjectListViewTheme.MenuHighlightColor; } }
            public override Color MenuItemBorder { get { return ObjectListViewTheme.ControlLightLightColor; } }
            public override Color MenuBorder { get { return ObjectListViewTheme.ControlLightLightColor; } }
            public override Color ImageMarginGradientBegin { get { return ObjectListViewTheme.MenuColor; } }
            public override Color ImageMarginGradientMiddle { get { return ObjectListViewTheme.MenuColor; } }
            public override Color ImageMarginGradientEnd { get { return ObjectListViewTheme.MenuColor; } }
            public override Color ToolStripGradientBegin { get { return ObjectListViewTheme.ControlColor; } }
            public override Color ToolStripGradientMiddle { get { return ObjectListViewTheme.ControlColor; } }
            public override Color ToolStripGradientEnd { get { return ObjectListViewTheme.ControlColor; } }
            public override Color StatusStripGradientBegin { get { return ObjectListViewTheme.ControlColor; } }
            public override Color StatusStripGradientEnd { get { return ObjectListViewTheme.ControlColor; } }
            public override Color ButtonSelectedGradientBegin { get { return ObjectListViewTheme.ControlDarkColor; } }
            public override Color ButtonSelectedGradientMiddle { get { return ObjectListViewTheme.ControlDarkColor; } }
            public override Color ButtonSelectedGradientEnd { get { return ObjectListViewTheme.ControlDarkColor; } }
            public override Color ButtonSelectedBorder { get { return ObjectListViewTheme.ControlLightLightColor; } }
            public override Color ButtonPressedGradientBegin { get { return ObjectListViewTheme.ControlColor; } }
            public override Color ButtonPressedGradientMiddle { get { return ObjectListViewTheme.ControlColor; } }
            public override Color ButtonPressedGradientEnd { get { return ObjectListViewTheme.ControlColor; } }
            public override Color CheckBackground { get { return ObjectListViewTheme.ControlDarkColor; } }
            public override Color CheckSelectedBackground { get { return ObjectListViewTheme.ControlDarkColor; } }
        }
#endif
    }
}
