using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Windows.Threading;
using System.Drawing;
using System.Drawing.Imaging;

namespace TcpReceive
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public delegate void writeDelegate(string str);
    public delegate void writeImageDelegate(Bitmap bitmap);
    public delegate string readDelegate();

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            string hostname = Dns.GetHostName();
            string str_ipad = null;
            IPAddress[] adrList = Dns.GetHostAddresses(hostname);
            foreach (IPAddress address in adrList)
            {
                str_ipad = address.ToString();
            }
            txtLocalAd.Text = str_ipad;

            Thread t = new Thread(new ThreadStart(ListenData));
            t.Start();
        }

        public void writeData(string str)
        {
            txtData.AppendText(str);
        }

        public void writeRemoteAd(string str)
        {
            txtRemoteAd.Text = str;
        }

        public void writeImage(Bitmap bitmap)
        {
            Clipboard.Clear();
            Clipboard.SetDataObject(bitmap);
            txtData.CaretPosition = txtData.Document.ContentEnd;
            txtData.Paste();
            Clipboard.Clear();
        }

        public string readLocalAd()
        {
            return txtLocalAd.Text;
        }

        public void ListenData()
        {
            //IPAddress ipad = IPAddress.Parse("10.18.83.167");
            string local_ipad = (string)txtLocalAd.Dispatcher.Invoke(new readDelegate(readLocalAd));
            IPAddress ipad = IPAddress.Parse(local_ipad);
            Int32 prt = 2112;
            TcpListener tl = new TcpListener(ipad, prt);
            tl.Start();

            TcpClient tc = tl.AcceptTcpClient();

            string remote_ipad = tc.Client.RemoteEndPoint.ToString();
            Dispatcher.Invoke(new writeDelegate(writeRemoteAd), new object[] { remote_ipad });

            NetworkStream ns = tc.GetStream();
            StreamReader sr = new StreamReader(ns);

            //string result = sr.ReadToEnd();
            //Dispatcher.Invoke(new writeDelegate(writeData), new object[] { result });
            var bary = new byte[4];
            ns.Read(bary, 0, bary.Length);
            int datasize = BitConverter.ToInt32(bary, 0);
            int readsize = 0;
            var bitmap = new byte[datasize];
            while (readsize < datasize)
            {
                readsize += ns.Read(bitmap, readsize, datasize - readsize);
            }
            //BitmapSource bitmapSource = BitmapSource.Create(2, 2, 300, 300, PixelFormats.Indexed8, BitmapPalettes.Gray256, bitmap, 2);
            //Bitmap bmp = GetBitmap(bitmapSource);
            BitmapImage bitmapImage = LoadImage(bitmap);
            Bitmap bmp = BitmapImage2Bitmap(bitmapImage);
            Dispatcher.Invoke(new writeImageDelegate(writeImage), new object[] { bmp });
            //Dispatcher.Invoke(new writeDelegate(writeData), new object[] { datasize.ToString() });

            tc.Close();
            tl.Stop();
        }

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        /*
        public Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
                source.PixelWidth,
                source.PixelHeight,
                PixelFormats.Pbgra32);
            BitmapData data = bmp.LockBits(
                new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size),
                ImageLockMode.WriteOnly,
                PixelFormats.Pbgra32);
            source.CopyPixels(
                Int32Rect.Empty,
                data.Scan0,
                data.Height * data.Stride,
                data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }
        */
    }
}
