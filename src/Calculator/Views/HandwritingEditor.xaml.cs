using MyScript.IInk;
using MyScriptEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace CalculatorApp.Views
{
    public sealed partial class HandwritingEditor : UserControl
    {
        public static readonly DependencyProperty EditorProperty =
            DependencyProperty.Register("Editor", typeof(MyScript.IInk.Editor), typeof(HandwringCalculator), new PropertyMetadata(default));

        public Visibility EditorBarVisibility
        {
            get { return (Visibility)GetValue(EditorBarVisibilityProperty); }
            set { SetValue(EditorBarVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditorBarVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditorBarVisibilityProperty =
            DependencyProperty.Register("EditorBarVisibility", typeof(Visibility), typeof(HandwritingEditor), new PropertyMetadata(Visibility.Collapsed));

        // Offscreen rendering
        private float _dpiX = 96;
        private float _dpiY = 96;

        // Defines the type of content (possible values are: "Text Document", "Text", "Diagram", "Math", "Drawing" and "Raw Content")
        private const string PartType = "Math";

        public HandwritingEditor()
        {
            this.InitializeComponent();
            Initialize(App.Engine);
        }

        public MyScript.IInk.Editor Editor
        {
            get { return (MyScript.IInk.Editor)GetValue(EditorProperty); }
            set { SetValue(EditorProperty, value); }
        }

        public string GetExpression()
        {
            var expression = string.Empty;
            var mimeTypes = Editor.GetSupportedExportMimeTypes(null);
            if ((mimeTypes != null) && (mimeTypes.Contains(MimeType.MATHML)))
            {
                expression = Editor.Export_(null, MimeType.MATHML, null);
            }

            return expression;
        }

        private void Initialize(MyScript.IInk.Engine engine)
        {
            // Initialize the editor with the engine
            var info = DisplayInformation.GetForCurrentView();
            _dpiX = info.RawDpiX;
            _dpiY = info.RawDpiY;
            var pixelDensity = UcEditor.GetPixelDensity();

            if (pixelDensity > 0.0f)
            {
                _dpiX /= pixelDensity;
                _dpiY /= pixelDensity;
            }

            // RawDpi properties can return 0 when the monitor does not provide physical dimensions and when the user is
            // in a clone or duplicate multiple -monitor setup.
            if (_dpiX == 0 || _dpiY == 0)
                _dpiX = _dpiY = 96;

            var renderer = engine.CreateRenderer(_dpiX, _dpiY, UcEditor);
            renderer.AddListener(new RendererListener(UcEditor));
            var toolController = engine.CreateToolController();
            Initialize(Editor = engine.CreateEditor(renderer, toolController));
            Initialize(Editor.ToolController);

            NewFile();
        }

        private void Initialize(MyScript.IInk.Editor editor)
        {
            editor.SetViewSize((int)ActualWidth, (int)ActualHeight);
            editor.SetFontMetricsProvider(new FontMetricsProvider(_dpiX, _dpiY));
            editor.AddListener(new EditorListener(UcEditor));
        }

        private static void Initialize(MyScript.IInk.ToolController controller)
        {
            controller.SetToolForType(MyScript.IInk.PointerType.MOUSE, MyScript.IInk.PointerTool.PEN);
            controller.SetToolForType(MyScript.IInk.PointerType.PEN, MyScript.IInk.PointerTool.PEN);
            controller.SetToolForType(MyScript.IInk.PointerType.TOUCH, MyScript.IInk.PointerTool.PEN);
        }

        private void AppBar_UndoButton_Click(object sender, RoutedEventArgs e)
        {
            Editor.Undo();
        }

        private void AppBar_RedoButton_Click(object sender, RoutedEventArgs e)
        {
            Editor.Redo();
        }

        private void AppBar_ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Editor.Clear();
        }

        private async void AppBar_ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var supportedStates = Editor.GetSupportedTargetConversionStates(null);

                if ((supportedStates != null) && (supportedStates.Count() > 0))
                    Editor.Convert(null, supportedStates[0]);
            }
            catch (Exception ex)
            {
                var msgDialog = new Windows.UI.Popups.MessageDialog(ex.ToString());
                await msgDialog.ShowAsync();
            }
        }

        private void ClosePackage()
        {
            var part = Editor.Part;
            var package = part?.Package;
            Editor.Part = null;
            part?.Dispose();
            package?.Dispose();
        }

        private async void NewFile()
        {
            try
            {
                // Close current package
                ClosePackage();

                // Create package and part
                var packageName = MakeUntitledFilename();
                var package = Editor.Engine.CreatePackage(packageName);
                var part = package.CreatePart(PartType);
                Editor.Part = part;
            }
            catch (Exception ex)
            {
                ClosePackage();

                var msgDialog = new Windows.UI.Popups.MessageDialog(ex.ToString());
                await msgDialog.ShowAsync();
                Application.Current.Exit();
            }
        }

        private static string MakeUntitledFilename()
        {
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            var num = 0;
            string name;

            do
            {
                var baseName = "File" + (++num) + ".iink";
                name = System.IO.Path.Combine(localFolder, baseName);
            }
            while (System.IO.File.Exists(name));

            return name;
        }

        private void OnPenClick(object sender, RoutedEventArgs e)
        {
            if (!(Editor?.ToolController is MyScript.IInk.ToolController controller)) return;
            controller.SetToolForType(MyScript.IInk.PointerType.MOUSE, MyScript.IInk.PointerTool.PEN);
            controller.SetToolForType(MyScript.IInk.PointerType.PEN, MyScript.IInk.PointerTool.PEN);
            controller.SetToolForType(MyScript.IInk.PointerType.TOUCH, MyScript.IInk.PointerTool.PEN);
        }

        private void OnTouchClick(object sender, RoutedEventArgs e)
        {
            if (!(Editor?.ToolController is MyScript.IInk.ToolController controller)) return;
            controller.SetToolForType(MyScript.IInk.PointerType.MOUSE, MyScript.IInk.PointerTool.HAND);
            controller.SetToolForType(MyScript.IInk.PointerType.PEN, MyScript.IInk.PointerTool.HAND);
            controller.SetToolForType(MyScript.IInk.PointerType.TOUCH, MyScript.IInk.PointerTool.HAND);
        }

        private void OnAutoClick(object sender, RoutedEventArgs e)
        {
            if (!(Editor?.ToolController is MyScript.IInk.ToolController controller)) return;
            controller.SetToolForType(MyScript.IInk.PointerType.MOUSE, MyScript.IInk.PointerTool.PEN);
            controller.SetToolForType(MyScript.IInk.PointerType.PEN, MyScript.IInk.PointerTool.HAND);
            controller.SetToolForType(MyScript.IInk.PointerType.TOUCH, MyScript.IInk.PointerTool.PEN);
        }

        private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (args.KeyboardAccelerator.Key == Windows.System.VirtualKey.Enter)
            {
                AppBar_ConvertButton_Click(sender, null);
            }
            else if (args.KeyboardAccelerator.Key == Windows.System.VirtualKey.Z && args.KeyboardAccelerator.Modifiers == Windows.System.VirtualKeyModifiers.Control)
            {
                AppBar_UndoButton_Click(sender, null);
            }
            else if (args.KeyboardAccelerator.Key == Windows.System.VirtualKey.Y && args.KeyboardAccelerator.Modifiers == Windows.System.VirtualKeyModifiers.Control)
            {
                AppBar_RedoButton_Click(sender, null);
            }
            else if (args.KeyboardAccelerator.Key == Windows.System.VirtualKey.Delete)
            {
                AppBar_ClearButton_Click(sender, null);
            }
        }
    }
}
