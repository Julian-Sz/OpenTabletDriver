using System.Collections.Generic;
using Eto.Forms;
using OpenTabletDriver.Desktop.Reflection;
using OpenTabletDriver.UX.Controls.Generic;
using OpenTabletDriver.Plugin;

namespace OpenTabletDriver.UX.Controls.Bindings
{
    public sealed class GestureTouchpadBindingEditor : BindingEditor
    {
        public GestureTouchpadBindingEditor()
        {
            this.Content = new Scrollable
            {
                Border = BorderType.None,
                Content = new StackLayout
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Spacing = 5,
                    Items = 
                    {
                        new Group
                        {
                            Text = "Gesture Touchpad",
                            Content = touchGestures = new GestureTouchpadBindingDisplayList
                            {
                                Prefix = "Gesture Binding",
                            }
                        }
                    }
                }
            };

            touchGestures.ItemSourceBinding.Bind(SettingsBinding.Child(c => (IList<PluginSettingStore>)c.TouchGestures));
        }

        private GestureTouchpadBindingDisplayList touchGestures;
    }
}
