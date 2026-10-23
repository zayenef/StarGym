using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using WpfWindow = System.Windows.Window;

namespace StarGymApp.Desktop;

public partial class PhotoCaptureWindow : WpfWindow
{
    private readonly object frameLock = new();
    private readonly CancellationTokenSource previewCancellation = new();
    private VideoCapture? camera;
    private Mat? latestFrame;
    private Task? previewTask;
    private bool isClosing;

    public byte[]? CapturedPhoto { get; private set; }

    public PhotoCaptureWindow()
    {
        InitializeComponent();
        Loaded += Window_OnLoaded;
        Closing += Window_OnClosing;
    }

    private async void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var openedCamera = await Task.Run(() =>
            {
                var capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
                if (capture.IsOpened()) return capture;
                capture.Dispose();
                return new VideoCapture(0, VideoCaptureAPIs.MSMF);
            });
            if (isClosing)
            {
                openedCamera.Dispose();
                return;
            }

            camera = openedCamera;
            if (!camera.IsOpened())
            {
                camera.Dispose();
                camera = null;
                CameraStatusText.Text = "No camera was found. Connect a webcam and try again.";
                return;
            }

            camera.Set(VideoCaptureProperties.FrameWidth, 1280);
            camera.Set(VideoCaptureProperties.FrameHeight, 720);
            CaptureButton.IsEnabled = true;
            CameraStatusText.Text = "Camera ready. Capture a clear photo of the member.";
            previewTask = Task.Run(() => PreviewCamera(camera, previewCancellation.Token));
        }
        catch (Exception exception)
        {
            CameraStatusText.Text = $"Camera unavailable: {exception.Message}";
        }
    }

    private void PreviewCamera(VideoCapture activeCamera, CancellationToken cancellationToken)
    {
        using var frame = new Mat();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!activeCamera.Read(frame) || frame.Empty())
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (CapturedPhoto is null)
                        {
                            CameraStatusText.Text = "The camera stopped responding. Check the webcam and try again.";
                            CaptureButton.IsEnabled = false;
                        }
                    });
                    return;
                }

                lock (frameLock)
                {
                    latestFrame?.Dispose();
                    latestFrame = frame.Clone();
                }

                var bitmap = CreateBitmap(frame);
                Dispatcher.InvokeAsync(() =>
                {
                    if (CapturedPhoto is null && !isClosing)
                    {
                        CameraPreviewImage.Source = bitmap;
                    }
                });
                Thread.Sleep(33);
            }
        }
        catch (Exception exception)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    CameraStatusText.Text = $"Camera preview stopped: {exception.Message}";
                    CaptureButton.IsEnabled = false;
                });
            }
        }
    }

    private void CaptureButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CapturedPhoto is not null)
        {
            CapturedPhoto = null;
            UsePhotoButton.IsEnabled = false;
            CaptureButton.Content = "Capture photo";
            CameraStatusText.Text = "Camera ready. Capture a clear photo of the member.";
            return;
        }

        byte[] photo;
        lock (frameLock)
        {
            if (latestFrame is null || latestFrame.Empty())
            {
                CameraStatusText.Text = "Waiting for a camera image. Please try again.";
                return;
            }

            if (!Cv2.ImEncode(".jpg", latestFrame, out photo))
            {
                CameraStatusText.Text = "The photo could not be encoded. Please try again.";
                return;
            }
        }

        CapturedPhoto = photo;
        CameraPreviewImage.Source = LoadBitmap(photo);
        CameraStatusText.Text = "Photo captured. Retake it if needed, or use this photo.";
        CaptureButton.Content = "Retake";
        UsePhotoButton.IsEnabled = true;
    }

    private void UsePhotoButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CapturedPhoto is null)
        {
            CameraStatusText.Text = "Capture a photo before continuing.";
            return;
        }

        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => Close();

    private void Window_OnClosing(object? sender, CancelEventArgs e)
    {
        if (isClosing) return;
        isClosing = true;
        CaptureButton.IsEnabled = false;
        previewCancellation.Cancel();
        _ = CleanupCameraAsync();
    }

    private async Task CleanupCameraAsync()
    {
        try
        {
            if (previewTask is not null) await previewTask;
        }
        finally
        {
            camera?.Dispose();
            lock (frameLock)
            {
                latestFrame?.Dispose();
                latestFrame = null;
            }
            previewCancellation.Dispose();
        }
    }

    private static BitmapSource CreateBitmap(Mat frame)
    {
        using var bgra = new Mat();
        switch (frame.Channels())
        {
            case 1:
                Cv2.CvtColor(frame, bgra, ColorConversionCodes.GRAY2BGRA);
                break;
            case 3:
                Cv2.CvtColor(frame, bgra, ColorConversionCodes.BGR2BGRA);
                break;
            case 4:
                frame.CopyTo(bgra);
                break;
            default:
                throw new InvalidOperationException($"Unsupported camera image format with {frame.Channels()} channels.");
        }
        var stride = bgra.Cols * 4;
        var pixels = new byte[bgra.Rows * stride];
        Marshal.Copy(bgra.Data, pixels, 0, pixels.Length);
        var bitmap = BitmapSource.Create(bgra.Cols, bgra.Rows, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapImage LoadBitmap(byte[] photo)
    {
        using var stream = new MemoryStream(photo);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
