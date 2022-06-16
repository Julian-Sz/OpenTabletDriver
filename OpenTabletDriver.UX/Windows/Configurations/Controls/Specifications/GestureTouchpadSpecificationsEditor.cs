using System;
using Eto.Forms;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.UX.Controls.Generic;
using OpenTabletDriver.UX.Controls.Generic.Text;

namespace OpenTabletDriver.UX.Windows.Configurations.Controls.Specifications
{
    public class GestureTouchpadSpecificationsEditor : SpecificationsEditor<GestureTouchpadSpecifications>
    {
        public GestureTouchpadSpecificationsEditor()
        {
            this.Content = new StackLayout
            {
                Spacing = 5,
                Items =
                {
                    new StackLayoutItem
                    {
                        Control = enable = new CheckBox
                        {
                            Text = "Enable",
                        }
                    },
                    new Group
                    {
                        Text = "Gesture Count",
                        Orientation = Orientation.Horizontal,
                        Content = gestureCount = new UnsignedIntegerNumberBox()
                    }
                }
            };

            enable.CheckedBinding.Cast<bool>().Bind(
                SpecificationsBinding.Convert(
                    c => c != null,
                    v => v ? new GestureTouchpadSpecifications() : null
                )
            );

            enable.CheckedBinding.Bind(gestureCount, g => g.Enabled);

            gestureCount.ValueBinding.Bind(SpecificationsBinding.Child(b => b.GestureCount));
        }

        private CheckBox enable;
        private MaskedTextBox<uint> gestureCount;
    }
}
