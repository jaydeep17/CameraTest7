using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace CameraTest7
{
    public partial class OutputPage : PhoneApplicationPage
    {
        private String readText;
        int count = 0, maxTextLength;
        List<String> text;
        public OutputPage()
        {
            InitializeComponent();
            text = (List<String>)PhoneApplicationService.Current.State["text"];
            readText = String.Join(" ", text);
            txt.Text = readText;
        }
    }
}