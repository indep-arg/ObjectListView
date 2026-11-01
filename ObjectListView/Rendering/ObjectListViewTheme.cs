using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BrightIdeasSoftware {
    /// <summary>
    /// Selects the default palette used by ObjectListView controls created by an application.
    /// Configure this before creating controls. This does not theme unrelated WinForms controls.
    /// </summary>
    public static class ObjectListViewTheme {
        private static ObjectListViewColorMode colorMode;
        private static bool systemDarkMode;
        private static HotItemStyle lightHotItemStyle;
        private static HotItemStyle darkHotItemStyle;

        /// <summary>
        /// Gets or sets the application-wide ObjectListView color mode.
        /// System mode samples the Windows preference when this property is assigned.
        /// </summary>
        public static ObjectListViewColorMode ColorMode {
            get { return colorMode; }
            set {
                if (!Enum.IsDefined(typeof(ObjectListViewColorMode), value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (value == ObjectListViewColorMode.System)
                {
                    systemDarkMode = ReadSystemDarkMode();
                }

                colorMode = value;
            }
        }

        /// <summary>
        /// Gets whether the selected mode uses a dark palette. High contrast always takes precedence.
        /// </summary>
        public static bool IsDarkMode {
            get {
                if (SystemInformation.HighContrast)
                {
                    return false;
                }

                if (colorMode == ObjectListViewColorMode.Dark)
                {
                    return true;
                }

                if (colorMode == ObjectListViewColorMode.System)
                {
                    return systemDarkMode;
                }
#if NET10_0_OR_GREATER
                if (colorMode == ObjectListViewColorMode.Default)
                {
                    return Application.IsDarkModeEnabled;
                }
#endif
                return false;
            }
        }

        /// <summary>Gets the stock WinForms window background color.</summary>
        public static Color WindowColor { get { return ResolveColor(Color.FromArgb(50, 50, 50), Color.White, SystemColors.Window); } }
        /// <summary>Gets the default text color.</summary>
        public static Color TextColor { get { return ResolveColor(Color.FromArgb(240, 240, 240), Color.Black, SystemColors.WindowText); } }
        /// <summary>Gets the background color of headers and alternate rows.</summary>
        public static Color ControlColor { get { return ResolveColor(Color.FromArgb(32, 32, 32), Color.FromArgb(240, 240, 240), SystemColors.Control); } }
        /// <summary>Gets the input and dropdown background color.</summary>
        public static Color ControlLightColor { get { return ResolveColor(Color.FromArgb(46, 46, 46), Color.FromArgb(227, 227, 227), SystemColors.ControlLight); } }
        /// <summary>Gets the stock focused-input background color.</summary>
        public static Color ControlLightLightColor { get { return ResolveColor(Color.FromArgb(31, 31, 31), Color.White, SystemColors.ControlLightLight); } }
        /// <summary>Gets the separator and pressed background color.</summary>
        public static Color ControlDarkColor { get { return ResolveColor(Color.FromArgb(74, 74, 74), Color.FromArgb(160, 160, 160), SystemColors.ControlDark); } }
        /// <summary>Gets the disabled text color.</summary>
        public static Color GrayTextColor { get { return ResolveColor(Color.FromArgb(150, 150, 150), Color.FromArgb(109, 109, 109), SystemColors.GrayText); } }
        /// <summary>Gets the selected background color.</summary>
        public static Color HighlightColor { get { return ResolveColor(Color.FromArgb(40, 100, 180), Color.FromArgb(0, 120, 215), SystemColors.Highlight); } }
        /// <summary>Gets the selected text color.</summary>
        public static Color HighlightTextColor { get { return ResolveColor(Color.Black, Color.White, SystemColors.HighlightText); } }
        /// <summary>Gets the focus, checkbox, and active-tab accent color.</summary>
        public static Color AccentColor { get { return HighlightColor; } }
        /// <summary>Gets the normal button background color.</summary>
        public static Color ButtonColor { get { return ResolveColor(Color.FromArgb(51, 51, 51), Color.FromArgb(240, 240, 240), SystemColors.Control, false); } }
        /// <summary>Gets the button hover color.</summary>
        public static Color ButtonHoverColor { get { return ResolveColor(Color.FromArgb(69, 69, 69), Color.FromArgb(227, 227, 227), SystemColors.ControlLight, false); } }
        /// <summary>Gets the stock WinForms pressed-button background color.</summary>
        public static Color ButtonPressedColor { get { return ResolveColor(Color.FromArgb(102, 102, 102), Color.FromArgb(160, 160, 160), SystemColors.ControlDark, false); } }
        /// <summary>Gets the stock WinForms button foreground color.</summary>
        public static Color ButtonTextColor { get { return ResolveColor(Color.FromArgb(240, 240, 240), Color.Black, SystemColors.ControlText, false); } }
        /// <summary>Gets the stock WinForms disabled-button foreground color.</summary>
        public static Color ButtonDisabledTextColor { get { return ResolveColor(Color.FromArgb(204, 204, 204), Color.FromArgb(109, 109, 109), SystemColors.GrayText, false); } }
        /// <summary>Gets the row hover background color.</summary>
        public static Color HoverColor { get { return ControlLightColor; } }
        /// <summary>Gets the inactive selection background color.</summary>
        public static Color InactiveSelectionColor { get { return ControlColor; } }
        /// <summary>Gets the inactive selection foreground from the selected palette.</summary>
        public static Color InactiveSelectionTextColor { get { return ResolveColor(Color.FromArgb(240, 240, 240), Color.Black, SystemColors.ControlText); } }
        /// <summary>Gets the description and inactive-tab text color.</summary>
        public static Color SecondaryTextColor { get { return GrayTextColor; } }
        /// <summary>Gets a readable hyperlink foreground, respecting the system color in high contrast.</summary>
        public static Color LinkColor { get { return ResolveColor(Color.LightSkyBlue, Color.Blue, SystemColors.HotTrack, false); } }
        /// <summary>Gets the stock WinForms menu background color.</summary>
        public static Color MenuColor { get { return ResolveColor(Color.FromArgb(55, 55, 55), Color.FromArgb(240, 240, 240), SystemColors.Menu); } }
        /// <summary>Gets the stock WinForms menu highlight color.</summary>
        public static Color MenuHighlightColor { get { return ResolveColor(Color.FromArgb(42, 128, 210), Color.FromArgb(0, 120, 215), SystemColors.MenuHighlight); } }

        internal static Color AlternateRowColor { get { return IsDarkMode ? ControlColor : Color.LemonChiffon; } }
        internal static Color HeaderTextColor { get { return IsDarkMode ? TextColor : Color.Black; } }
        internal static Color EmptyListTextColor { get { return IsDarkMode ? TextColor : SystemColors.ControlDarkDark; } }
        internal static Color EmptyListBackColor { get { return IsDarkMode ? ControlLightColor : Color.BlanchedAlmond; } }
        internal static Color HyperlinkTextColor { get { return IsDarkMode ? LinkColor : Color.Blue; } }
        internal static Color VisitedLinkTextColor { get { return IsDarkMode ? Color.FromArgb(197, 134, 192) : Color.Purple; } }
        internal static bool UsesCustomHeaderColors {
            get { return !SystemInformation.HighContrast && (IsDarkMode || colorMode != ObjectListViewColorMode.Default); }
        }
        private static bool UsesCustomEditorColors {
            get { return IsDarkMode || colorMode != ObjectListViewColorMode.Default; }
        }

        internal static HotItemStyle DefaultHotItemStyle {
            get {
                if (IsDarkMode)
                {
                    return darkHotItemStyle ?? (darkHotItemStyle = new HotItemStyle { BackColor = HoverColor });
                }

                return lightHotItemStyle ?? (lightHotItemStyle = new HotItemStyle { BackColor = Color.FromArgb(224, 235, 253) });
            }
        }

        internal static Color ResolveWindowColor(Color color) {
            return color == SystemColors.Window ? WindowColor : color;
        }

        internal static Color ResolveTextColor(Color color) {
            return color == SystemColors.WindowText ? TextColor : color;
        }

        internal static Color GetHeaderBackColor(bool pressed, bool hot) {
            return pressed ? ControlDarkColor : hot ? ControlLightColor : ControlColor;
        }

        internal static Color GetDescriptionColor(bool printing) {
            return IsDarkMode && !printing ? SecondaryTextColor : Color.FromArgb(45, 46, 49);
        }

        internal static void ApplyCellEditorColors(Control editor) {
            if (!UsesCustomEditorColors)
            {
                return;
            }

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(editor);
            PropertyDescriptor backColor = properties[nameof(Control.BackColor)];
            PropertyDescriptor foreColor = properties[nameof(Control.ForeColor)];
            if (backColor != null && !backColor.ShouldSerializeValue(editor))
            {
                editor.BackColor = IsDarkMode ? ControlLightColor : WindowColor;
            }

            if (foreColor != null && !foreColor.ShouldSerializeValue(editor))
            {
                editor.ForeColor = TextColor;
            }
        }

        private static Color ResolveColor(Color dark, Color light, Color system, bool useSystemInDark = true) {
            if (SystemInformation.HighContrast)
            {
                return system;
            }

            if (IsDarkMode) {
#if NET10_0_OR_GREATER
                if (useSystemInDark && Application.IsDarkModeEnabled)
                {
                    return system;
                }
#endif
                return dark;
            }
            return colorMode == ObjectListViewColorMode.Default ? system : light;
        }

        private static bool ReadSystemDarkMode() {
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) {
                    object value = key == null ? null : key.GetValue("AppsUseLightTheme");
                    return value is int && (int)value == 0;
                }
            } catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException || ex is IOException) {
                Trace.TraceWarning("ObjectListView could not read the Windows color preference: {0}", ex.Message);
                return false;
            }
        }
    }
}
