using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaceRegistrator.Models;
using FlashCap;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MsBox.Avalonia;
using System.IO.Compression;
using SkiaSharp;
using System.Threading;


namespace FaceRegistrator.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
            if (!File.Exists("Faces"))
            {
                Directory.CreateDirectory("Faces");
            }
        }

        [ObservableProperty]
        private bool useOpenCV = false;

        [ObservableProperty]
        private int deviceNumber = 0;

        [ObservableProperty]
        private bool capturingVideo = false;

    //    [ObservableProperty]
    //    private VideoCapture? videoCapture = null;

        #region Private Observable Class Members
        // Members
        [ObservableProperty]
        private ObservableCollection<string> errors = new ObservableCollection<string>();

        [ObservableProperty]
        private string? selectedErrorText = null;

        [ObservableProperty]
        private string hostName = "http://atkif.nurdev.uz";

        [ObservableProperty]
        private string cameraActionButtonTitle = "Kamera tanlanmagan";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RequestStudentsCommand))]
        private Group? selectedGroup = null;

        partial void OnSelectedGroupChanged(Group? value)
        {
            if (value != null)
            {
                Task.Run(RequestStudents);
            }
        }

        [ObservableProperty]
        private ReadOnlyCollection<Group>? groups;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartOrStopCapturingCommand))]
        [NotifyCanExecuteChangedFor(nameof(TakePictureCommand))]
        private bool isCameraReady = false;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendPicturesToServerCommand))]
        private Student? selectedStudent = null;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemovePictureCommand))]
        private SKBitmap? selectedPicture = null;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendPicturesToServerCommand))]
        private ObservableCollection<SKBitmap> pictures = new ObservableCollection<SKBitmap>();

        [ObservableProperty]
        private SKBitmap? frameImage = null;

        [ObservableProperty]
        private ReadOnlyCollection<Student>? students;

        [ObservableProperty]
        private List<CameraDevice>? cameraDevices;

        [ObservableProperty]
        private ReadOnlyCollection<VideoCharacteristics>? cameraFormats;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectCameraDeviceCommand))]
        private VideoCharacteristics? selectedCameraFormat;
        #endregion
        
       



        #region Property/Members
        // Members
        private CameraDevice? selectedCameraDevice;
        private CaptureDevice? mWebCamera;

        // Properties
        public CameraDevice? SelectedCameraDevice
        {
            get => selectedCameraDevice;
            set
            {
                if (SetProperty(ref selectedCameraDevice, value))
                {
                    CameraFormats = value?.GetVideoFormats();
                    SelectedCameraFormat = null;
                }
            }
        }
        #endregion


        #region Relay Command Functions
        // Commands

        [RelayCommand]
        private async Task RequestGroups()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{HostName}/api/groups");
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            Groups = JsonConvert.DeserializeObject<ReadOnlyCollection<Group>?>(reader.ReadToEnd());
                            SelectedGroup = null;
                            Students = null;
                            SelectedStudent = null;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                var error = $"Guruhlarni internetdan yuklashda xatolik\nError: {ex.Message}";
                Errors.Add(error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanRequestStudents))]
        private async Task RequestStudents()
        {
            try
            {
                if (SelectedGroup == null)
                    throw new ArgumentNullException("Guruh tanlanmagan");

                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{HostName}/api/students/{SelectedGroup.ID}");
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            Students = JsonConvert.DeserializeObject<ReadOnlyCollection<Student>?>(reader.ReadToEnd());
                            SelectedStudent = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var error = $"Talabalarni internetdan yuklashda xatolik yuz berdi\nError: {ex.Message}";
                Errors.Add(error);
            }
        }

        private bool CanRequestStudents()
        {
            return SelectedGroup != null;
        }

        [RelayCommand]
        private async Task RefreshDevices()
        {
            await DisposeWebCamera();
            var devices = new CaptureDevices();
            var deviceList = new List<CameraDevice>();
            foreach (var  device in devices.EnumerateDescriptors())
            {
                deviceList.Add(new CameraDevice(device));
            }
            CameraDevices = deviceList;
        }

        [RelayCommand(CanExecute = nameof(CanConnectCameraDevice))]
        private async Task ConnectCameraDevice()
        {
            try
            {
                /* if (VideoCapture != null)
                {
                    VideoCapture.Stop();
                    VideoCapture.Dispose();
                    VideoCapture = null;
                    CapturingVideo = false;
                }

                if (UseOpenCV)
                {
                    await DisposeWebCamera();
                    VideoCapture = new VideoCapture(DeviceNumber);
                    VideoCapture.ImageGrabbed += ProccessImage;
                    IsCameraReady = true;
                    await StartOrStopCapturing();
                    return;
                } */

                if (SelectedCameraDevice == null)
                    throw new ArgumentNullException(nameof(SelectedCameraDevice));

                if (SelectedCameraFormat == null)
                    throw new ArgumentNullException(nameof(SelectedCameraFormat));

                await DisposeWebCamera();

                var descriptor = SelectedCameraDevice.GetCaptureDescriptor();
                var format = SelectedCameraFormat;
                if (format == null || descriptor == null)
                    throw new ArgumentNullException($"nameof(format), nameof(descriptor)");

                mWebCamera = await descriptor.OpenAsync(format, TranscodeFormats.DoNotTranscode, false, 1, OnPixelBufferArrivedAsync);
                IsCameraReady = true;
                await StartOrStopCapturing();
            }
            catch (Exception ex)
            {
                var error = $"Web kameraga ulanishda xatolik\n{ex.GetType()} Error: {ex.Message}";
                Errors.Add(error);
            }
        }

        private void ProccessImage(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private bool CanConnectCameraDevice()
        {
            return 
                /* UseOpenCV == true ?
                true : */
                SelectedCameraDevice != null && SelectedCameraFormat != null;
        }

        [RelayCommand]
        private void ClearErrors()
        {
            SelectedErrorText = null;
            Errors.Clear();
        }

        [RelayCommand(CanExecute = nameof(CanStartOrStopCapturing))]
        private async Task StartOrStopCapturing()
        {
            try
            {
               /* if (UseOpenCV)
                {
                    if (VideoCapture == null)
                        throw new ArgumentNullException(nameof(VideoCapture));
                    if (CapturingVideo)
                    {

                    } else
                    {

                    }
                    return;
                }*/

                if (mWebCamera == null || !IsCameraReady)
                    throw new ArgumentNullException(nameof(mWebCamera));

                if (mWebCamera.IsRunning)
                {
                    await mWebCamera.StopAsync();
                    CameraActionButtonTitle = $"{mWebCamera.Name} (Start)";
                }
                else
                {
                    await mWebCamera.StartAsync();
                    CameraActionButtonTitle = $"{mWebCamera.Name} (Stop)";
                }
                
            }
            catch (Exception ex)
            {
                var error = $"Web kamera bilan ishlashda xotolik yuz berdi\nError: {ex.Message}";
                Errors.Add(error);
            }
        }

        private bool CanStartOrStopCapturing()
        {
            return IsCameraReady == true;
        }

        [RelayCommand(CanExecute = nameof(CanSendPicturesToServer))]
        private async Task SendPicturesToServer()
        {
            try
            {
                
                if (SelectedGroup == null || SelectedStudent == null || Pictures.Count == 0)
                {
                    throw new ArgumentNullException("Yetarlicha parametr tanlanmagan");
                }

                await StartOrStopCapturing();

                var student = SelectedStudent;
                var group = SelectedGroup;
                var counter = 0;

                var filename = $"Faces/student-{student.ID}.zip";
                if (File.Exists(filename))
                    File.Delete(filename);
                using (var file = File.OpenWrite(filename))
                {
                    using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
                    {
                        foreach (var picture in Pictures)
                        {
                            var templateName = $"face_{student.ID}_{counter++}_{DateTime.Now.ToShortDateString()}.png";
                            var entry = zip.CreateEntry(templateName);
                            using (var entryFile = entry.Open())
                            {
                                picture.Encode(SKEncodedImageFormat.Png, 1)
                                    .SaveTo(entryFile);
                            }
                        }
                        {
                            var entry = zip.CreateEntry("fileinfo.txt");
                            using (var entryFile = entry.Open())
                            {
                                using (var writer = new StreamWriter(entryFile))
                                {
                                    writer.WriteLine($"Student F.I.O.: {student.Fullname} (ID: {student.ID})");
                                    writer.WriteLine($"Group: {group.Name} (ID: {group.ID})");
                                    writer.WriteLine($"Faculty: ATKIF");
                                    writer.WriteLine($"Total images: {Pictures.Count}");
                                    writer.WriteLine($"Registerd date: {DateTime.Now.ToShortDateString()}");
                                }
                            }
                        }
                    }
                }
                /*
                using (var file = File.OpenRead(filename))
                {
                    using (var client = new HttpClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Post, $"{HostName}/api/send_face");
                        var content = new MultipartFormDataContent
                            {
                                { new StringContent($"{student.ID}"), "id" },
                                { new StringContent($"{group.ID}"), "group_id" },
                                { new StreamContent(file), "file", $"{student.ID}.zip" }
                            };
                        request.Content = content;
                        var response = await client.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                    }
                }
                */
                // File.Delete(filename);
                await StartOrStopCapturing();
                Pictures.Clear();
                SelectedStudent = null;
                var box = MessageBoxManager.GetMessageBoxStandard("ATKIF: Face Registrator", $"{student.Fullname}, talaba ma'lumotlari serverga yuborildi", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
                await box.ShowAsync();
            }
            catch (Exception ex)
            {
                var error = $"Ma'lumotlarni serverga yuborishda xatolik\n{ex.GetType()} Error: {ex.Message}";
                Errors.Add(error);
            }
        }

        private bool CanSendPicturesToServer()
        {
            return SelectedStudent != null;
        }

        [RelayCommand(CanExecute = nameof(CanTakePicture))]
        private async Task TakePicture()
        {
            try
            {
                if (FrameImage == null)
                    return;

                await StartOrStopCapturing();
                Pictures.Add(FrameImage.Copy());
                await StartOrStopCapturing();
            }
            catch (Exception ex)
            {
                var error = $"Web kameradan rasmni saqlashda xatolik\nError: {ex.Message}";
                Errors.Add(error);
            }
        }

        private bool CanTakePicture()
        {
            return IsCameraReady;
        }

        [RelayCommand(CanExecute = nameof(CanRemovePicture))]
        private void RemovePicture()
        {
            try
            {
                if (SelectedPicture != null)
                {
                    Pictures.Remove(SelectedPicture);
                    SelectedPicture = null;
                }
            }
            catch (Exception ex)
            {
                var error = $"Rasmni o'chirishda xatolik\nError: {ex.Message}";
                Errors.Add(error);
            }
        }

        private bool CanRemovePicture()
        {
            return SelectedPicture != null;
        }

        #endregion

        #region Private Functions

        public async Task DisposeWebCamera()
        {
            if (mWebCamera != null)
            {
                if (mWebCamera.IsRunning)
                {
                    await mWebCamera.StopAsync();
                }
                await mWebCamera.DisposeAsync();
                mWebCamera = null;
                IsCameraReady = false;
            }
        }

        private void OnPixelBufferArrivedAsync(PixelBufferScope bufferScope)
        {
            try
            {
                Thread.Sleep(70);
                FrameImage?.Dispose();
                var rawImage = bufferScope.Buffer.CopyImage();
                var bitmap = SKBitmap.Decode(rawImage);
                FrameImage = bitmap;
                bufferScope.ReleaseNow();
            }
            catch (ArgumentNullException)
            {

            }
            catch (Exception ex)
            {
                var error = $"Web kameradan rasmni olishda xatolik\n{ex.GetType()} Error: {ex.Message}";
                Errors.Add(error);
            }

        }

        #endregion
    }
}
