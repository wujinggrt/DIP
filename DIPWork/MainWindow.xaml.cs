using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Drawing;

namespace DIPWork
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializerFields();
        }

        private OpenFileDialog openDialog;
        private SaveFileDialog saveDialog;
        private string fileFormatFilter;

        private WriteableBitmap writeableBitmap;
        //private ImageProcessor 

        private byte[] R = null;
        private byte[] G = null;
        private byte[] B = null;

        private int rows;
        private int cols;

        private int stride;
        private double DPIX;
        private double DPIY;

        private void InitializerFields()
        {
            fileFormatFilter = "JPEG Image(*.jpg)|*.jpg|TIF Image (*.tif)|*.tif|" +
                "PNG Image (*.png)|*.png|BMP (*.bmp)|*.bmp";
            SetupOpenDialog();
            SetupSaveDialog();
        }

        private void SetupOpenDialog()
        {
            openDialog = new OpenFileDialog();
            openDialog.Title = "Openning image file...";
            openDialog.CheckFileExists = true;
            openDialog.DefaultExt = ".jpg";
            openDialog.Filter = fileFormatFilter;
            openDialog.FilterIndex = 0;
            openDialog.Multiselect = false;
        }

        private void SetupSaveDialog()
        {
            saveDialog = new SaveFileDialog();
            saveDialog.Title = "Saving image file...";
            saveDialog.DefaultExt = ".jpg";
            saveDialog.FilterIndex = 0;
            saveDialog.Filter = fileFormatFilter;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            /*if (processor.HasImage)
            {
                openDialog.InitialDirectory = processor.SourceImagePath;
            }
            else
            {*/
            openDialog.InitialDirectory = Environment.CurrentDirectory;
            //}
            /*

             */
            Nullable<bool> result = openDialog.ShowDialog();
            if (result == true)
            {
                var filePath = openDialog.FileName;
                Uri uri = new Uri(filePath, UriKind.RelativeOrAbsolute);
                var bitmapImage = new BitmapImage(uri);
                writeableBitmap = new WriteableBitmap(bitmapImage);

                stride = writeableBitmap.BackBufferStride;
                cols = writeableBitmap.PixelWidth;
                rows = writeableBitmap.PixelHeight;
                DPIX = writeableBitmap.DpiX;
                DPIY = writeableBitmap.DpiY;
                R = new byte[rows * cols];
                G = new byte[rows * cols];
                B = new byte[rows * cols];
                displayImageCtr.Source = writeableBitmap;
                DecodeImage(writeableBitmap);
            }
        }

        // save the image displayed in image control.
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Nullable<bool> result = saveDialog.ShowDialog();
            if (result == true)
            {
                BitmapSource BS = (BitmapSource)displayImageCtr.Source;
                PngBitmapEncoder PBE = new PngBitmapEncoder();
                PBE.Frames.Add(BitmapFrame.Create(BS));
                using (Stream stream = File.Create(saveDialog.FileName))
                {
                    PBE.Save(stream);
                }
            }

        }

        private int GetTailAddressOffset()
        {
            return (int)(4 - writeableBitmap.BackBufferStride % 4) % 4;
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            displayImageCtr.Source = Color2Gray();
        }
        
        protected void DecodeImage(WriteableBitmap source)
        {
            try
            {
                source.Lock();
                unsafe
                {
                    byte* pBackBuffer = (byte*)(source.BackBuffer);
                    int currentPixelIndex = 0; //当前解码像素索引                    
                    byte[] r = R;
                    byte[] g = G;
                    byte[] b = B;
                    int tailAddressOffset = GetTailAddressOffset();
                    for (int row = 0; row < rows; row++)
                    {
                        for (int column = 0; column < cols; column++)
                        {
                            b[currentPixelIndex] = *pBackBuffer++;
                            g[currentPixelIndex] = *pBackBuffer++;
                            r[currentPixelIndex++] = *pBackBuffer++;
                            pBackBuffer++;
                        }
                        pBackBuffer = pBackBuffer + tailAddressOffset;
                    }
                }
            }
            finally
            {
                source.Unlock();
            }
        }

        private WriteableBitmap Color2Gray()
        {
            WriteableBitmap wb = null;
            byte[] gray = new byte[R.Length];
            for (int i = 0; i < gray.Length; i++)
            {
                gray[i] = (byte)RGB2Gray64(R[i], G[i], B[i], 0);
            }
            wb = CreateWB(gray, PixelFormats.Gray8);
            return wb;
        }
        
        private long RGB2Gray64(long r, long g, long b, int scale)
        {   //gray = r*0.299+g*0.587+b*0.114  系数扩大了16384倍，即左移的14位
            return ((r << scale) * 4899 + (g << scale) * 9617 + (b << scale) * 1868) >> 14;
        }
        
        private WriteableBitmap CreateWB(byte[] imageData, PixelFormat pf)
        {
            WriteableBitmap wb = new WriteableBitmap(cols, rows, DPIX, DPIY, pf, null);
            Int32Rect rect = new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight);
            int stride = wb.PixelWidth * pf.BitsPerPixel / 8;
            wb.WritePixels(rect, imageData, stride, 0);
            return wb;
        }
    }

    class Processor
    {
        Processor(string src)
        {
            Uri uri = new Uri(src, UriKind.RelativeOrAbsolute);
            var bitmapImage = new BitmapImage(uri);
            writeableBitmap = new WriteableBitmap(bitmapImage);

            stride = writeableBitmap.BackBufferStride;
            cols = writeableBitmap.PixelWidth;
            rows = writeableBitmap.PixelHeight;
            DPIX = writeableBitmap.DpiX;
            DPIY = writeableBitmap.DpiY;
            R = new byte[rows * cols];
            G = new byte[rows * cols];
            B = new byte[rows * cols];

            DecodeImage(writeableBitmap);
        }

        private WriteableBitmap writeableBitmap;

        private byte[] R = null;
        private byte[] G = null;
        private byte[] B = null;

        private int rows;
        private int cols;

        private int stride;
        private double DPIX;
        private double DPIY;

        public WriteableBitmap Image
        {
            get
            {
                return writeableBitmap;
            }
            set
            {
            }
        }

        private int GetTailAddressOffset()
        {
            return (int)(4 - writeableBitmap.BackBufferStride % 4) % 4;
        }

        protected void DecodeImage(WriteableBitmap source)
        {
            try
            {
                source.Lock();
                unsafe
                {
                    byte* pBackBuffer = (byte*)(source.BackBuffer);
                    int currentPixelIndex = 0; //当前解码像素索引                    
                    byte[] r = R;
                    byte[] g = G;
                    byte[] b = B;
                    int tailAddressOffset = GetTailAddressOffset();
                    for (int row = 0; row < rows; row++)
                    {
                        for (int column = 0; column < cols; column++)
                        {
                            b[currentPixelIndex] = *pBackBuffer++;
                            g[currentPixelIndex] = *pBackBuffer++;
                            r[currentPixelIndex++] = *pBackBuffer++;
                            pBackBuffer++;
                        }
                        pBackBuffer = pBackBuffer + tailAddressOffset;
                    }
                }
            }
            finally
            {
                source.Unlock();
            }
        }

        public WriteableBitmap Color2Gray()
        {
            WriteableBitmap wb = null;
            byte[] gray = new byte[R.Length];
            for (int i = 0; i < gray.Length; i++)
            {
                gray[i] = (byte)RGB2Gray64(R[i], G[i], B[i], 0);
            }
            wb = CreateWB(gray, PixelFormats.Gray8);
            return wb;
        }

        private long RGB2Gray64(long r, long g, long b, int scale)
        {   //gray = r*0.299+g*0.587+b*0.114  系数扩大了16384倍，即左移的14位
            return ((r << scale) * 4899 + (g << scale) * 9617 + (b << scale) * 1868) >> 14;
        }

        private WriteableBitmap CreateWB(byte[] imageData, PixelFormat pf)
        {
            WriteableBitmap wb = new WriteableBitmap(cols, rows, DPIX, DPIY, pf, null);
            Int32Rect rect = new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight);
            int stride = wb.PixelWidth * pf.BitsPerPixel / 8;
            wb.WritePixels(rect, imageData, stride, 0);
            return wb;
        }
    }
}
