using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

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

            // Scroll Fix
            Microsoft.Maui.Handlers.ScrollViewHandler.Mapper.AppendToMapping("AdvancedScrollFix", (handler, view) =>
            {
#if WINDOWS
                var scrollViewer = handler.PlatformView;
                if (scrollViewer == null) return;

                scrollViewer.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler((sender, e) =>
                {
                    var pointerPoint = e.GetCurrentPoint(null);
                    var delta = pointerPoint.Properties.MouseWheelDelta;
                    var isShiftPressed = e.KeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Shift);

                    DependencyObject parent = VisualTreeHelper.GetParent(scrollViewer);
                    while (parent != null && !(parent is ScrollViewer))
                    {
                        parent = VisualTreeHelper.GetParent(parent);
                    }

                    if (parent is ScrollViewer parentScrollViewer)
                    {
                        if (view.Orientation == ScrollOrientation.Horizontal)
                        {
                            if (isShiftPressed)
                            {
                                scrollViewer.ChangeView(scrollViewer.HorizontalOffset - delta, null, null);
                                e.Handled = true;
                            }
                            else
                            {
                                parentScrollViewer.ChangeView(null, parentScrollViewer.VerticalOffset - delta, null);
                                e.Handled = true;
                            }
                        }

                        else if (view.Orientation == ScrollOrientation.Both || view.Orientation == ScrollOrientation.Vertical)
                        {
                            if (!isShiftPressed)
                            {
                                bool isAtTop = scrollViewer.VerticalOffset <= 0;
                                bool isAtBottom = scrollViewer.VerticalOffset >= (scrollViewer.ScrollableHeight - 1.0);

                                if ((delta > 0 && isAtTop) || (delta < 0 && isAtBottom))
                                {
                                    parentScrollViewer.ChangeView(null, parentScrollViewer.VerticalOffset - delta, null);
                                    e.Handled = true;
                                }
                            }
                            else
                            {
                                scrollViewer.ChangeView(scrollViewer.HorizontalOffset - delta, null, null);
                                e.Handled = true;
                            }
                        }
                    }
                }), true);
#endif
            });

            return builder;
        }
    }
}
