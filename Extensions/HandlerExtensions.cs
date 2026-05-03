namespace SimplexMethodApp.Extensions
{
    public static class HandlerExtensions
    {
        public static MauiAppBuilder ConfigureCustomHandlers(this MauiAppBuilder builder)
        {
            // The Button Cursor Fix
            Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("CursorFix", (handler, view) =>
            {
#if WINDOWS
                var cursorProperty = typeof(Microsoft.UI.Xaml.UIElement).GetProperty("ProtectedCursor",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                handler.PlatformView.PointerEntered += (s, e) =>
                {
                    var handCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
                    cursorProperty?.SetValue(handler.PlatformView, handCursor);
                };

                handler.PlatformView.PointerExited += (s, e) =>
                {
                    cursorProperty?.SetValue(handler.PlatformView, null);
                };
#endif
            });

            // The Zero-Flicker Numeric Entry
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("ZeroFlickerNumeric", (handler, view) =>
            {
                if (view is Controls.NumericEntry)
                {
#if WINDOWS
                    handler.PlatformView.PreviewKeyDown += (sender, args) =>
                    {
                        bool isControl = args.Key == Windows.System.VirtualKey.Back ||
                                         args.Key == Windows.System.VirtualKey.Tab ||
                                         args.Key == Windows.System.VirtualKey.Left ||
                                         args.Key == Windows.System.VirtualKey.Right ||
                                         args.Key == Windows.System.VirtualKey.Delete;

                        bool isNumber = (args.Key >= Windows.System.VirtualKey.Number0 && args.Key <= Windows.System.VirtualKey.Number9) ||
                                        (args.Key >= Windows.System.VirtualKey.NumberPad0 && args.Key <= Windows.System.VirtualKey.NumberPad9);

                        bool isMath = args.Key == Windows.System.VirtualKey.Subtract ||
                                      args.Key == (Windows.System.VirtualKey)189 ||
                                      args.Key == (Windows.System.VirtualKey)190 ||
                                      args.Key == (Windows.System.VirtualKey)188 ||
                                      args.Key == Windows.System.VirtualKey.Decimal;

                        bool isShiftDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                                           .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

                        if (isShiftDown && isNumber)
                        {
                            args.Handled = true;
                            return;
                        }

                        if (!isNumber && !isControl && !isMath)
                        {
                            args.Handled = true;
                        }
                    };
#endif
                }
            });

            return builder;
        }
    }
}
