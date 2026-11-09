using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Photos.Models;
using ReactiveUI;
using uWidgets.Services;

namespace Photos.ViewModels;

public class PhotoFrameViewModel : ReactiveObject, IDisposable
{
    private static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".webp"];

    private readonly PhotoFrameModel model;
    private readonly List<string> imageFiles = [];
    private int currentIndex;
    private int minutesCounter;
    private Bitmap? currentBitmap;
    private Bitmap? secondaryBitmap;

    public PhotoFrameViewModel(PhotoFrameModel model)
    {
        this.model = model;
        LoadImageList();
        ShowCurrentImage();
        TimerService.Timer1Minute.Subscribe(OnTick);
    }

    public Bitmap? CurrentBitmap
    {
        get => currentBitmap;
        private set
        {
            var old = currentBitmap;
            this.RaiseAndSetIfChanged(ref currentBitmap, value);
            this.RaisePropertyChanged(nameof(HasImage));
            this.RaisePropertyChanged(nameof(ShowPlaceholder));
            this.RaisePropertyChanged(nameof(CurrentFileName));
            this.RaisePropertyChanged(nameof(DisplayCaption));
            this.RaisePropertyChanged(nameof(HasDisplayCaption));
            this.RaisePropertyChanged(nameof(ImageCounterText));
            old?.Dispose();
        }
    }

    public Bitmap? SecondaryBitmap
    {
        get => secondaryBitmap;
        private set
        {
            var old = secondaryBitmap;
            this.RaiseAndSetIfChanged(ref secondaryBitmap, value);
            this.RaisePropertyChanged(nameof(HasSecondaryImage));
            old?.Dispose();
        }
    }

    public bool HasImage => CurrentBitmap != null;
    public bool HasSecondaryImage => SecondaryBitmap != null;
    public bool ShowPlaceholder => CurrentBitmap == null;
    public bool HasMultipleImages => imageFiles.Count > 1;
    public bool HasCaption => !string.IsNullOrWhiteSpace(model.Caption);
    public string Caption => model.Caption;
    public string CurrentFileName =>
        imageFiles.Count > 0
            ? Path.GetFileNameWithoutExtension(imageFiles[Math.Clamp(currentIndex, 0, imageFiles.Count - 1)])
            : string.Empty;
    public string DisplayCaption => !string.IsNullOrWhiteSpace(model.Caption) ? model.Caption : CurrentFileName;
    public bool HasDisplayCaption => !string.IsNullOrWhiteSpace(DisplayCaption);
    public string ImageCounterText => imageFiles.Count > 0 ? $"{currentIndex + 1} / {imageFiles.Count}" : string.Empty;
    public Stretch ImageStretch => model.FillWidget ? Stretch.UniformToFill : Stretch.Uniform;
    public Thickness FrameMargin => model.FillWidget ? new Thickness(0) : new Thickness(10);
    public CornerRadius FrameCornerRadius => model.FillWidget ? new CornerRadius(0) : new CornerRadius(12);

    public void NextImage()
    {
        if (imageFiles.Count <= 1) return;
        currentIndex = (currentIndex + 1) % imageFiles.Count;
        ShowCurrentImage();
    }

    public void PreviousImage()
    {
        if (imageFiles.Count <= 1) return;
        currentIndex = (currentIndex - 1 + imageFiles.Count) % imageFiles.Count;
        ShowCurrentImage();
    }

    private void OnTick()
    {
        if (imageFiles.Count <= 1) return;
        minutesCounter++;
        if (minutesCounter >= Math.Max(1, model.IntervalMinutes))
        {
            minutesCounter = 0;
            NextImage();
        }
    }

    private void LoadImageList()
    {
        imageFiles.Clear();
        if (string.IsNullOrWhiteSpace(model.Path)) return;

        try
        {
            if (File.Exists(model.Path) && SupportedExtensions.Contains(Path.GetExtension(model.Path).ToLowerInvariant()))
            {
                imageFiles.Add(model.Path);
            }
            else if (Directory.Exists(model.Path))
            {
                var files = Directory.EnumerateFiles(model.Path)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .OrderBy(f => f);
                imageFiles.AddRange(files);
            }
        }
        catch
        {
            // Ignore IO errors on invalid/inaccessible paths
        }
    }

    private void ShowCurrentImage()
    {
        if (imageFiles.Count == 0)
        {
            CurrentBitmap = null;
            SecondaryBitmap = null;
            return;
        }

        CurrentBitmap = LoadBitmapSafe(imageFiles[Math.Clamp(currentIndex, 0, imageFiles.Count - 1)]);
        SecondaryBitmap = imageFiles.Count > 1
            ? LoadBitmapSafe(imageFiles[(currentIndex + 1) % imageFiles.Count])
            : null;
    }

    private static Bitmap? LoadBitmapSafe(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                using var stream = File.OpenRead(filePath);
                return new Bitmap(stream);
            }
        }
        catch
        {
            // Ignore decode errors
        }

        return null;
    }

    public void Dispose()
    {
        TimerService.Timer1Minute.Unsubscribe(OnTick);
        currentBitmap?.Dispose();
        currentBitmap = null;
        secondaryBitmap?.Dispose();
        secondaryBitmap = null;
        GC.SuppressFinalize(this);
    }
}
