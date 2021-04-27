using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using OpenTabletDriver.Desktop;
using OpenTabletDriver.Desktop.Interop;
using OpenTabletDriver.Desktop.Reflection;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Display;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.UX.Controls.Area;
using OpenTabletDriver.UX.Controls.Generic;
using OpenTabletDriver.UX.Controls.Utilities;
using OpenTabletDriver.UX.Windows;

namespace OpenTabletDriver.UX.Controls
{
    public class OutputModeEditor : Panel
    {
        public OutputModeEditor()
        {
            this.Content = new StackLayout
            {
                Padding = 5,
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items =
                {
                    new StackLayoutItem(outputModeEditor, true),
                    outputModeSelector
                }
            };

            outputModeSelector.SelectedValueChanged += (sender, args) =>
            {
                if (outputModeSelector.SelectedType is TypeInfo type)
                    this.Store = new PluginSettingStore(type);
            };

            outputModeSelector.SelectedTypeBinding.Bind(
                StoreBinding.Convert<TypeInfo>(
                    c => c?.GetPluginReference().GetTypeReference(),
                    t => PluginSettingStore.FromPath(t.FullName)
                )
            );
        }

        private PluginSettingStore store;
        public PluginSettingStore Store
        {
            set
            {
                this.store = value;
                this.OnStoreChanged();
            }
            get => this.store;
        }
        
        public event EventHandler<EventArgs> StoreChanged;
        
        protected virtual void OnStoreChanged()
        {
            StoreChanged?.Invoke(this, new EventArgs());
            UpdateOutputMode(this.Store);
        }
        
        public BindableBinding<OutputModeEditor, PluginSettingStore> StoreBinding
        {
            get
            {
                return new BindableBinding<OutputModeEditor, PluginSettingStore>(
                    this,
                    c => c.Store,
                    (c, v) => c.Store = v,
                    (c, h) => c.StoreChanged += h,
                    (c, h) => c.StoreChanged -= h
                );
            }
        }

        private Panel outputModeEditor = new Panel();
        private AbsoluteModeEditor absoluteModeEditor = new AbsoluteModeEditor();
        private RelativeModeEditor relativeModeEditor = new RelativeModeEditor();
        private TypeDropDown<IOutputMode> outputModeSelector = new TypeDropDown<IOutputMode> { Width = 300 };

        public void SetTabletSize(TabletState tablet)
        {
            var tabletAreaEditor = absoluteModeEditor.tabletAreaEditor;
            if (tablet.Properties?.Specifications?.Digitizer is DigitizerSpecifications digitizer)
            {
                tabletAreaEditor.ViewModel.Background = new RectangleF[]
                {
                    new RectangleF(0, 0, digitizer.Width, digitizer.Height)
                };

                var settings = App.Current.Settings;
                if (settings != null && settings.TabletWidth == 0 && settings.TabletHeight == 0)
                {
                    settings.TabletWidth = digitizer.Width;
                    settings.TabletHeight = digitizer.Height;
                    settings.TabletX = digitizer.Width / 2;
                    settings.TabletY = digitizer.Height / 2;
                }
            }
            else
            {
                tabletAreaEditor.ViewModel.Background = null;
            }
        }

        public void SetDisplaySize(IEnumerable<IDisplay> displays)
        {
            var bgs = from disp in displays
                where !(disp is IVirtualScreen)
                select new RectangleF(disp.Position.X, disp.Position.Y, disp.Width, disp.Height);
            absoluteModeEditor.displayAreaEditor.ViewModel.Background = bgs;
        }

        private void UpdateOutputMode(PluginSettingStore store)
        {
            bool showAbsolute = false;
            bool showRelative = false;
            if (store != null)
            {
                var outputMode = store.GetPluginReference().GetTypeReference<IOutputMode>();
                showAbsolute = outputMode.IsSubclassOf(typeof(AbsoluteOutputMode));
                showRelative = outputMode.IsSubclassOf(typeof(RelativeOutputMode));
            }

            if (showAbsolute)
                outputModeEditor.Content = absoluteModeEditor;
            else if (showRelative)
                outputModeEditor.Content = relativeModeEditor;
        }

        private class AbsoluteModeEditor : Panel
        {
            public AbsoluteModeEditor()
            {
                this.Content = new StackLayout
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Items =
                    {
                        new StackLayoutItem(new Group("Display", displayAreaEditor), true),
                        new StackLayoutItem(new Group("Tablet", tabletAreaEditor), true)
                    }
                };

                var settings = App.Current.Settings;
                displayAreaEditor.Rebind(settings);
                tabletAreaEditor.Rebind(settings);
            }

            public DisplayAreaEditor displayAreaEditor = new DisplayAreaEditor
            {
                ViewModel = new AreaViewModel
                {
                    Unit = "px",
                    EnableRotation = false
                }
            };

            public TabletAreaEditor tabletAreaEditor = new TabletAreaEditor
            {
                ViewModel = new AreaViewModel
                {
                    InvalidBackgroundError = "No tablet detected.",
                    Unit = "mm",
                    EnableRotation = true
                }
            };

            public class DisplayAreaEditor : AreaEditor
            {
                public DisplayAreaEditor()
                    : base()
                {
                    this.ToolTip = "You can right click the area editor to set the area to a display, adjust alignment, or resize the area.";

                    Rebind(App.Current.Settings);
                }

                public void Rebind(Settings settings)
                {
                    this.Bind(c => c.ViewModel.Width, settings, m => m.DisplayWidth);
                    this.Bind(c => c.ViewModel.Height, settings, m => m.DisplayHeight);
                    this.Bind(c => c.ViewModel.X, settings, m => m.DisplayX);
                    this.Bind(c => c.ViewModel.Y, settings, m => m.DisplayY);
                    this.Bind(c => c.ViewModel.LockToUsableArea, settings, m => m.LockUsableAreaDisplay);
                }

                protected override void OnLoadComplete(EventArgs e)
                {
                    base.OnLoadComplete(e);

                    var subMenu = base.ContextMenu.Items.GetSubmenu("Set to display");
                    foreach (var display in DesktopInterop.VirtualScreen.Displays)
                    {
                        subMenu.Items.Add(
                            new ActionCommand
                            {
                                MenuText = display.ToString(),
                                Action = () =>
                                {
                                    this.ViewModel.Width = display.Width;
                                    this.ViewModel.Height = display.Height;
                                    if (display is IVirtualScreen virtualScreen)
                                    {
                                        this.ViewModel.X = virtualScreen.Width / 2;
                                        this.ViewModel.Y = virtualScreen.Height / 2;
                                    }
                                    else
                                    {
                                        virtualScreen = DesktopInterop.VirtualScreen;
                                        this.ViewModel.X = display.Position.X + virtualScreen.Position.X + (display.Width / 2);
                                        this.ViewModel.Y = display.Position.Y + virtualScreen.Position.Y + (display.Height / 2);
                                    }
                                }
                            }
                        );
                    }
                }
            }

            public class TabletAreaEditor : AreaEditor
            {
                public TabletAreaEditor()
                    : base()
                {
                    this.ToolTip = "You can right click the area editor to enable aspect ratio locking, adjust alignment, or resize the area.";                }

                private BooleanCommand lockAr, areaClipping, ignoreOutsideArea;

                public void Rebind(Settings settings)
                {
                    this.Bind(c => c.ViewModel.Width, settings, m => m.TabletWidth);
                    this.Bind(c => c.ViewModel.Height, settings, m => m.TabletHeight);
                    this.Bind(c => c.ViewModel.X, settings, m => m.TabletX);
                    this.Bind(c => c.ViewModel.Y, settings, m => m.TabletY);
                    this.Bind(c => c.ViewModel.Rotation, settings, m => m.TabletRotation);
                    this.Bind(c => c.ViewModel.LockToUsableArea, settings, m => m.LockUsableAreaTablet);
                    lockAr?.CheckedBinding.BindDataContext<Settings>(m => m.LockAspectRatio);
                    areaClipping?.CheckedBinding.BindDataContext<Settings>(m => m.EnableClipping);
                    ignoreOutsideArea?.CheckedBinding.BindDataContext<Settings>(m => m.EnableAreaLimiting);
                }

                protected override void OnLoadComplete(EventArgs e)
                {
                    base.OnLoadComplete(e);

                    base.ContextMenu.Items.AddSeparator();

                    lockAr = new BooleanCommand
                    {
                        MenuText = "Lock aspect ratio"
                    };

                    areaClipping = new BooleanCommand
                    {
                        MenuText = "Area clipping"
                    };

                    ignoreOutsideArea = new BooleanCommand
                    {
                        MenuText = "Ignore input outside area"
                    };

                    base.ContextMenu.Items.AddRange(
                        new Command[]
                        {
                            lockAr,
                            areaClipping,
                            ignoreOutsideArea
                        }
                    );

                    base.ContextMenu.Items.AddSeparator();

                    base.ContextMenu.Items.Add(
                        new ActionCommand
                        {
                            MenuText = "Convert area...",
                            Action = async () => await ConvertAreaDialog()
                        }
                    );
                }

                private async Task ConvertAreaDialog()
                {
                    var converter = new AreaConverterDialog();
                    await converter.ShowModalAsync(Application.Instance.MainForm);
                }
            }
        }

        private class RelativeModeEditor : Panel
        {
            public RelativeModeEditor()
            {
                this.Content = new SensitivityEditor();
            }

            public class SensitivityEditor : StackView
            {
                public SensitivityEditor()
                {
                    base.Orientation = Orientation.Horizontal;
                    base.VerticalContentAlignment = VerticalAlignment.Top;

                    UpdateBindings();
                    // App.SettingsChanged += (s) => UpdateBindings();
                }

                public void UpdateBindings()
                {
                    this.Items.Clear();

                    var xSensBox = new SensitivityEditorBox(
                        "X Sensitivity",
                        (s) => App.Current.Settings.XSensitivity = float.TryParse(s, out var val) ? val : 0f,
                        () => App.Current.Settings?.XSensitivity.ToString(),
                        "px/mm"
                    );
                    AddControl(xSensBox, true);

                    var ySensBox = new SensitivityEditorBox(
                        "Y Sensitivity",
                        (s) => App.Current.Settings.YSensitivity = float.TryParse(s, out var val) ? val : 0f,
                        () => App.Current.Settings?.YSensitivity.ToString(),
                        "px/mm"
                    );
                    AddControl(ySensBox, true);

                    var rotationBox = new SensitivityEditorBox(
                        "Rotation",
                        (s) => App.Current.Settings.RelativeRotation = float.TryParse(s, out var val) ? val : 0f,
                        () => App.Current.Settings?.RelativeRotation.ToString(),
                        "°"
                    );
                    AddControl(rotationBox, true);

                    var resetTimeBox = new SensitivityEditorBox(
                        "Reset Time",
                        (s) => App.Current.Settings.ResetTime = TimeSpan.TryParse(s, out var val) ? val : TimeSpan.FromMilliseconds(100),
                        () => App.Current.Settings?.ResetTime.ToString()
                    );
                    AddControl(resetTimeBox, true);
                }

                private class SensitivityEditorBox : Group
                {
                    public SensitivityEditorBox(
                        string header,
                        Action<string> setValue,
                        Func<string> getValue,
                        string unit = null
                    )
                    {
                        this.Text = header;
                        this.setValue = setValue;
                        this.getValue = getValue;

                        var layout = new StackView
                        {
                            Orientation = Orientation.Horizontal,
                            VerticalContentAlignment = VerticalAlignment.Center
                        };
                        layout.AddControl(textBox, true);

                        if (unit != null)
                        {
                            var unitControl = new Label
                            {
                                Text = unit,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            layout.AddControl(unitControl);
                        }

                        UpdateBindings();
                        // App.Current.SettingsChanged += (Settings) => UpdateBindings();
                        this.Content = layout;
                    }

                    private Action<string> setValue;
                    private Func<string> getValue;

                    private TextBox textBox = new TextBox();

                    private void UpdateBindings()
                    {
                        textBox.TextBinding.Bind(getValue, setValue);
                    }
                }
            }
        }
    }
}
