﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Devices;
using System.Threading;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;

namespace CameraTest7
{
    public partial class MainPage : PhoneApplicationPage
    {
        private PhotoCamera camera;
        private Thread imageProcessing;
        private static bool pumpARGBFrames, isStable = false;
        private static ManualResetEvent pauseFramesEvent = new ManualResetEvent(true);
        private WriteableBitmap wb;   // wb for original image
        private Utils utils;
        private BitmapImage bmp;
        public static Accelerometer acc;
        public double oldx;
        public double oldy;
        public double oldz;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            bmp = new BitmapImage(new Uri("img.jpg", UriKind.Relative));
            bmp.CreateOptions = BitmapCreateOptions.None;
            acc = new Accelerometer();
            try
            {

                acc.Start();
                acc.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(acc_CurrentValueChanged);
                oldy = acc.CurrentValue.Acceleration.Y;
                oldx = acc.CurrentValue.Acceleration.X;
                oldz = acc.CurrentValue.Acceleration.Z;
            }
            catch (Microsoft.Devices.Sensors.AccelerometerFailedException)
            {

            }
           
        }

        void acc_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {

            Dispatcher.BeginInvoke(delegate()
            {

                if (Math.Abs((double)(oldx - acc.CurrentValue.Acceleration.X)) > 0.20 || Math.Abs((double)(oldy - acc.CurrentValue.Acceleration.Y)) > 0.20)
                {
                    accTextbox.Text = "Rotate";
                    isStable = false;
                }
                else
                {
                    accTextbox.Text = "Good";
                    isStable = true;
                }
            });
      
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if ((PhotoCamera.IsCameraTypeSupported(CameraType.Primary) == true))
            {
                camera = new PhotoCamera(CameraType.Primary);

                camera.Initialized += new EventHandler<CameraOperationCompletedEventArgs>(cameraInitialized);
                camera.CaptureCompleted += new EventHandler<CameraOperationCompletedEventArgs>(captureCompleted);
                camera.CaptureImageAvailable += new EventHandler<ContentReadyEventArgs>(captureImageAvailable);
                camera.AutoFocusCompleted += new EventHandler<CameraOperationCompletedEventArgs>(cam_AutoFocusCompleted);
                viewfinderBrush.SetSource(camera);
                previewTransform.Rotation = camera.Orientation;// -90.0;
            }
            else
            {
                txtmsg.Text = "Sorry, but I can't find a working camera on this device";
            }
        }

        private void cam_AutoFocusCompleted(object sender, CameraOperationCompletedEventArgs e)
        {
            camera.CaptureImage();
        }

        private void Process()
        {
            int w = (int)camera.PreviewResolution.Width;    // width
            int h = (int)camera.PreviewResolution.Height;   // height
            int w2 = w / 2;     // half the image width
            int h2 = h / 2;     // half the image height
            int[] ARGBPx = new int[w * h];      // original pixels

            try
            {
                PhotoCamera phCam = (PhotoCamera)camera;
                while (pumpARGBFrames)
                {
                    pauseFramesEvent.WaitOne();
                    phCam.GetPreviewBufferArgb32(ARGBPx);
                    ARGBPx = utils.Binarize(ARGBPx, 125);   // try & error with threashold value
                    //ARGBPx = utils.Bitwise_not(ARGBPx);   // STILL BUGGY - Makes the Image disappear
                    ARGBPx = utils.Erode(ARGBPx, w, h);
                    Utils.Boundaries b = utils.CheckBoundaries(ARGBPx);
                    ImageHandler(b);
                    pauseFramesEvent.Reset();
                    Deployment.Current.Dispatcher.BeginInvoke(delegate()
                    {
                        ARGBPx.CopyTo(wb.Pixels, 0);
                        wb.Invalidate();
                        // TODO: identify if the image is complete, 
                        // IF yes, capture => stop this thread => crop the text area => send to hawaii => hope for best results
                        // else, continue
                        pauseFramesEvent.Set();
                    });
                }
            }
            catch (Exception e)
            {
                Dispatcher.BeginInvoke(delegate()
                {
                    // Display error message.
                    txtmsg.Text = e.Message;
                    
                });
            }
        }

        private void ImageHandler(Utils.Boundaries b)
        {
            if (b.Left || b.Right || b.Top || b.Bottom)
            {
                String s = "";
                //if (b.Left && !b.Right && !b.Top && !b.Bottom)
                //    s += "Move to the left";
                //else if (b.Right && !b.Left && !b.Top && !b.Bottom)
                //    s += "Move to the right";
                //else if (b.Top && !b.Left && !b.Right && !b.Bottom)
                //    s += "Move to the top";
                //else if (b.Bottom && !b.Left && !b.Top && !b.Right)
                //    s += "Move to the bottom";
                //else
                //    s += "Move upwards";
                if (b.Right)
                    s += " right ";
                if (b.Left)
                    s += " left ";
                if (b.Top)
                    s += " Top ";
                if (b.Bottom)
                    s += " bottom ";

                Dispatcher.BeginInvoke(delegate() { txtmsg.Text = s; });
            }
            else
            {
                Dispatcher.BeginInvoke(delegate()
                {
                    txtmsg.Text = "Please don't move the phone, let me click";
                    //camera.CaptureImage();
                    //pumpARGBFrames = false;
                });
                if(isStable)
                    camera.Focus();
            }
        }

        private void cameraInitialized(object sender, CameraOperationCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke(delegate()
            {
                imageProcessing = new Thread(Process);
                pumpARGBFrames = true;
                int w = (int)camera.PreviewResolution.Width;
                int h = (int)camera.PreviewResolution.Height;
                wb = new WriteableBitmap(w, h);
                utils = new Utils(w, h);
                img.Source = wb;
                imageProcessing.Start();
                txtmsg.Text = "width = " + w.ToString() + " height = " + h.ToString();
                startFlash();
            });
        }

        private void captureCompleted(object sender, CameraOperationCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke(delegate()
            {
                txtmsg.Text = "Image captured";
            });
        }

        private void captureImageAvailable(object sender, ContentReadyEventArgs e)
        {
            Dispatcher.BeginInvoke(delegate()
            {
                txtmsg.Text = "Image Available";
                BitmapImage bmpImage = new BitmapImage();
                bmpImage.CreateOptions = BitmapCreateOptions.None;
                bmpImage.SetSource(e.ImageStream);
                WriteableBitmap wb = new WriteableBitmap(bmpImage);
                Utils.resizeImage(ref wb);
                using (MemoryStream ms = new MemoryStream())
                {
                    wb.SaveJpeg(ms, (int)wb.PixelWidth, (int)wb.PixelHeight, 0, 100);
                    bmpImage.SetSource(ms);
                }
                PhoneApplicationService.Current.State["image"] = bmpImage;
                NavigationService.Navigate(new Uri("/ShowImage.xaml", UriKind.Relative));
                //txtmsg.Text = "Everything finished happily :)";
            });
        }

        private void cameraCanvasTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (camera != null)
            {
                try
                {
                    camera.Focus();
                }

                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(delegate()
                    {
                        txtmsg.Text = ex.Message;
                    });
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (camera != null)
            {
                pumpARGBFrames = false;
                //imageProcessing.Abort();
                camera.Dispose();
                camera.Initialized -= cameraInitialized;
                camera.CaptureCompleted -= captureCompleted;
                camera.CaptureImageAvailable -= captureImageAvailable;
            }
        }

        private void startFlash()
        {
            if (camera.IsFlashModeSupported(FlashMode.On))
            {
                try
                {
                    camera.FlashMode = FlashMode.On;
                }
                catch (Exception ex) { }
            }
        }

        private void stopFlash()
        {
            if (camera.IsFlashModeSupported(FlashMode.Off))
            {
                try
                {
                    camera.FlashMode = FlashMode.Off;
                }
                catch (Exception ex) { }
            }
        }

        private void setAutoFlash()
        {
            if (camera.IsFlashModeSupported(FlashMode.Auto))
            {
                try
                {
                    camera.FlashMode = FlashMode.Auto;
                }
                catch (Exception ex) { }
            }
        }
    }
}