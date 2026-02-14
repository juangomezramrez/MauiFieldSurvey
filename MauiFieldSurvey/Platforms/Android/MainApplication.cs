using Android.App;
using Android.Runtime;

namespace MauiFieldSurvey;

// AQUÍ APLICAMOS LA ESTRATEGIA PARANOICA (LargeHeap = true)
// Y también mantenemos los iconos que venían por defecto
[Application(
    LargeHeap = true,
    AllowBackup = true,
    Icon = "@mipmap/appicon",
    RoundIcon = "@mipmap/appicon_round",
    SupportsRtl = true)]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
//using Android.App;
//using Android.Runtime;

//namespace MauiFieldSurvey
//{
//    [Application]
//    public class MainApplication : MauiApplication
//    {
//        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
//            : base(handle, ownership)
//        {
//        }

//        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
//    }
//}
