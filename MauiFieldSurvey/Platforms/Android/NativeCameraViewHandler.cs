using Android.Content;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using Java.Lang;
using Java.Util.Concurrent;
using Microsoft.Maui.Handlers;
using MauiFieldSurvey.Controls;

namespace MauiFieldSurvey.Platforms.Android
{
    public class NativeCameraViewHandler : ViewHandler<NativeCameraView, PreviewView>
    {
        public static IPropertyMapper<NativeCameraView, NativeCameraViewHandler> PropertyMapper = new PropertyMapper<NativeCameraView, NativeCameraViewHandler>(ViewHandler.ViewMapper) { };

        private IExecutorService _cameraExecutor;
        private ImageCapture _imageCapture;
        private PreviewView _previewView;

        public NativeCameraViewHandler() : base(PropertyMapper) { }

        protected override PreviewView CreatePlatformView()
        {
            _previewView = new PreviewView(Context);
            _cameraExecutor = Executors.NewSingleThreadExecutor();
            return _previewView;
        }

        protected override void ConnectHandler(PreviewView platformView)
        {
            base.ConnectHandler(platformView);
            VirtualView.TakePhotoRequested += OnTakePhotoRequested;
            StartCamera();
        }

        protected override void DisconnectHandler(PreviewView platformView)
        {
            VirtualView.TakePhotoRequested -= OnTakePhotoRequested;
            _cameraExecutor?.Shutdown();
            base.DisconnectHandler(platformView);
        }

        private void StartCamera()
        {
            var cameraProviderFuture = ProcessCameraProvider.GetInstance(Context);
            cameraProviderFuture.AddListener(new Runnable(() =>
            {
                var cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();

                // 1. Configurar la vista previa (Preview)
                var preview = new Preview.Builder().Build();
                // Fix: Pass both required parameters (executor and surfaceProvider)
                preview.SetSurfaceProvider(ContextCompat.GetMainExecutor(Context), _previewView.SurfaceProvider);

                // 2. Configurar la captura de imagen (Optimizada para no saturar memoria)
                _imageCapture = new ImageCapture.Builder()
                    .SetCaptureMode(ImageCapture.CaptureModeMinimizeLatency)
                    .Build();

                var cameraSelector = CameraSelector.DefaultBackCamera;

                try
                {
                    cameraProvider.UnbindAll();
                    // Atamos la cámara al ciclo de vida de la actividad principal de MAUI
                    var lifecycleOwner = (AndroidX.Lifecycle.ILifecycleOwner)Platform.CurrentActivity;
                    cameraProvider.BindToLifecycle(lifecycleOwner, cameraSelector, preview, _imageCapture);
                }
                catch (System.Exception exc)
                {
                    VirtualView.OnCaptureFailed($"Error iniciando cámara: {exc.Message}");
                }

            }), ContextCompat.GetMainExecutor(Context));
        }

        private void OnTakePhotoRequested(object sender, EventArgs e)
        {
            if (_imageCapture == null) return;

            // Archivo temporal seguro
            var photoFile = new Java.IO.File(Context.CacheDir, $"{Guid.NewGuid()}.jpg");
            var outputOptions = new ImageCapture.OutputFileOptions.Builder(photoFile).Build();

            _imageCapture.TakePicture(outputOptions, _cameraExecutor, new ImageSavedCallback(VirtualView, photoFile.AbsolutePath));
        }

        // Callback nativo cuando CameraX termina de guardar el archivo
        private class ImageSavedCallback : Java.Lang.Object, ImageCapture.IOnImageSavedCallback
        {
            private readonly NativeCameraView _virtualView;
            private readonly string _filePath;

            public ImageSavedCallback(NativeCameraView virtualView, string filePath)
            {
                _virtualView = virtualView;
                _filePath = filePath;
            }

            public void OnImageSaved(ImageCapture.OutputFileResults outputFileResults)
            {
                // Devolver al hilo principal de MAUI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _virtualView.OnPhotoCaptured(_filePath);
                });
            }

            public void OnError(ImageCaptureException exception)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _virtualView.OnCaptureFailed(exception.Message);
                });
            }
        }
    }
}