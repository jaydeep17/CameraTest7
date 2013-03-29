using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using Microsoft.Hawaii.Ocr.Client;
using Microsoft.Hawaii;

namespace CameraTest7
{
    public partial class ShowImage : PhoneApplicationPage
    {
        BitmapImage bmp1;
        WriteableBitmap bmp;
        Utils utils;
        public ShowImage()
        {
            InitializeComponent();
            bmp1 = (BitmapImage)PhoneApplicationService.Current.State["image"];
            bg.ImageSource = bmp1;
            bmp = new WriteableBitmap(bmp1);
            txt.Text = "width = " + bmp1.PixelWidth.ToString() + " height = " + bmp1.PixelHeight.ToString();
            utils = new Utils(bmp1.PixelWidth, bmp1.PixelHeight);
            for (int i = 0; i < 10; i++)
            {
                StartOcr(new WriteableBitmap((BitmapImage)PhoneApplicationService.Current.State["image"+i.ToString()]));
            }
        }

        private void StartOcr(WriteableBitmap bmp)
        {

            if (bmp1.PixelHeight > 640 || bmp1.PixelWidth > 640)
                Utils.resizeImage(ref bmp);

            byte[] photoBuffer = Utils.imageToByte(bmp);
            OcrService.RecognizeImageAsync(Utils.HawaiiApplicationId, photoBuffer, (output) =>
            {
                Dispatcher.BeginInvoke(() => OnOcrComplete(output));
            });
        }

        List<String>[] alltext = new List<string>[10];
        int count = 0;

        private void OnOcrComplete(OcrServiceResult result)
        {
            if (result.Status == Status.Success)
            {
                int wordCount = 0;
                List<String> text = new List<string>();
                foreach (OcrText item in result.OcrResult.OcrTexts)
                {
                    wordCount += item.Words.Count;
                    foreach (var word in item.Words)
                    {
                        text.Add(word.Text);
                    }
                    //sb.AppendLine(item.Text);
                }
                //MessageBox.Show(sb.ToString());
                alltext[count] = text;
                count++;
                int index = 0;
                if (count == 10) {
                    int max = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        if (alltext[i].Count > max) {
                            max = alltext[i].Count;
                            index = i;
                        }
                    }
                    PhoneApplicationService.Current.State["text"] = alltext[index];
                    NavigationService.Navigate(new Uri("/OutputPage.xaml", UriKind.Relative));
                }
            }
            else
            {
                txt.Text = "[OCR conversion failed]\n" + result.Exception.Message;
            }
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            bmp = new WriteableBitmap(bmp1);
            //StartOcr();     // Initiates the OCR process
        }
    }


}