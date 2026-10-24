using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Plugin.LocalNotification;

namespace Vafadar.Zanance.DeliveryReview;

/// <summary>Initializes the existing notification SDK without any database, key, account or network service.</summary>
[Application]
public sealed class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() => MauiApp.CreateBuilder().UseMauiApp<DeliveryApplication>().UseLocalNotification().Build();
}
