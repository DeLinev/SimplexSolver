using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SimplexMethodApp.ViewModels;

namespace SimplexMethodApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
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

            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<MainPage>();

            return builder.Build();
        }
    }
}
