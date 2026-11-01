using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Microsoft.DotNet.DesignTools.Client;
using Microsoft.DotNet.DesignTools.Client.Proxies;

namespace BrightIdeasSoftware.Design.Client
{
    public abstract class PrinterDataEditor : UITypeEditor
    {
        protected abstract bool IsPen { get; }
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            var client = provider.GetRequiredService<IDesignToolsClient>();
            var session = provider.GetRequiredService<DesignerSession>();
            var service = provider.GetRequiredService<IWindowsFormsEditorService>();
            var endpoint = client.Protocol.GetEndpoint<PrinterEditorEndpoint>().GetSender(client);
            var response = endpoint.SendRequest(new PrinterEditorRequest(session.Id, value, this.IsPen));
            var model = (ObjectProxy)response.ViewModel;
            object localValue = PrinterEditorState.Decode(model.GetPropertyValue<string>("State"), this.IsPen);
            using (BrushForm form = this.IsPen ? new PenForm() : new BrushForm())
            using (var dialog = new Form {
                Text = this.IsPen ? "Edit pen" : "Edit brush",
                ClientSize = new Size(form.Width, form.Height + 40),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false
            })
            using (var buttons = new FlowLayoutPanel {
                Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft
            })
            using (var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel })
            using (var ok = new Button { Text = "OK", DialogResult = DialogResult.OK }) {
                if (this.IsPen)
                {
                    ((PenForm)form).SetPenData((PenData)localValue);
                }
                else
                {
                    form.SetBrush((IBrushData)localValue);
                }

                form.Dock = DockStyle.Fill;
                buttons.Controls.Add(cancel);
                buttons.Controls.Add(ok);
                dialog.Controls.Add(form);
                dialog.Controls.Add(buttons);
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;
                form.Show();
                if (service.ShowDialog(dialog) != DialogResult.OK)
                {
                    return value;
                }

                model.SetPropertyValue("State", PrinterEditorState.Encode(
                    this.IsPen ? (object)((PenForm)form).GetPenData() : form.GetBrushData(), this.IsPen));
            }
            return model.GetPropertyValue("Value");
        }
    }

    public sealed class BrushDataEditor : PrinterDataEditor
    {
        protected override bool IsPen { get { return false; } }
    }

    public sealed class PenDataEditor : PrinterDataEditor
    {
        protected override bool IsPen { get { return true; } }
    }
}
