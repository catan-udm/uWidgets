using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            old?.Dispose();
        }
    }

    public bool HasImage => CurrentBitmap != null;
    public bool ShowPlaceholder => CurrentBitmap == null;
    public bool HasMultipleImages => imageFiles.Count > 1;
    public bool HasCaption => !string.IsNullOrWhiteSpace(model.Caption);
    public string Caption => model.Caption;
    public Stretch ImageStretch => model.FillWidget ? Stretch.UniformToFill : Stretch.Uniform;

    public void NextImage()
    {
        if (imageFiles.Count <= 1) return;
        currentIndex = (currentIndex + 1) % imageFiles.Count;
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
            return;
        }

        try
        {
            var filePath = imageFiles[Math.Clamp(currentIndex, 0, imageFiles.Count - 1)];
            if (File.Exists(filePath))
            {
                using var stream = File.OpenRead(filePath);
                CurrentBitmap = new Bitmap(stream);
            }
            else
            {
                CurrentBitmap = null;
            }
        }
        catch
        {
            CurrentBitmap = null;
        }
    }

    public void Dispose()
    {
        TimerService.Timer1Minute.Unsubscribe(OnTick);
        currentBitmap?.Dispose();
        currentBitmap = null;
        GC.SuppressFinalize(this);
    }
}
