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

        private Processor processor = new Processor();

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
            if (processor.HasImage)
            {
                openDialog.InitialDirectory = processor.SourceImagePath;
            }
            else
            {
                openDialog.InitialDirectory = Environment.CurrentDirectory;
            }
            Nullable<bool> result = openDialog.ShowDialog();
            if (result == true)
            {
                processor.SetImage(openDialog.FileName);
                displayImageCtr.Source = processor.Image;
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

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            displayImageCtr.Source = processor.Color2Gray();
        }
    }


    class Processor
    {
        public Processor()
        {
        }

        private WriteableBitmap writeableBitmap = null;
        private bool hasImage = false;
        private string sourceImagePath = null;

        private ColorContents contents;

        private int rows;
        private int cols;

        private int stride;

        private double DPIX;
        private double DPIY;

        struct ColorContents
        {
            public byte[] R;
            public byte[] G;
            public byte[] B;

            public void Allocate(int size)
            {
                R = new byte[size];
                G = new byte[size];
                B = new byte[size];
            }
        }

        public WriteableBitmap Image
        {
            get
            {
                return writeableBitmap;
            }
            private set
            {
            }
        }

        public bool HasImage
        {
            get
            {
                return hasImage;
            }
            private set
            {
            }
        }

        public string SourceImagePath
        {
            get
            {
                return sourceImagePath;
            }
            private set
            {
            }
        }

        public void SetImage(string path)
        {
            hasImage = true;
            sourceImagePath = path;

            Uri uri = new Uri(path, UriKind.RelativeOrAbsolute);
            var bitmapImage = new BitmapImage(uri);
            writeableBitmap = new WriteableBitmap(bitmapImage);
        }

        public WriteableBitmap Color2Gray()
        {
            UpdateData();

            WriteableBitmap wb = null;
            byte[] gray = new byte[contents.R.Length];
            for (int i = 0; i < gray.Length; i++)
            {
                gray[i] = (byte)RGB2Gray64(contents.R[i], contents.G[i], contents.B[i], 0);
            }
            wb = CreateWB(gray, PixelFormats.Gray8);
            return wb;
        }

        private void UpdateData()
        {
            UpdateStride();
            UpdateSize();
            UpdateDPI();
            AllocateContents();
            DecodeImageBgr32();
        }

        private void UpdateStride()
        {
            stride = writeableBitmap.BackBufferStride;
        }

        private void UpdateSize()
        {
            cols = writeableBitmap.PixelWidth;
            rows = writeableBitmap.PixelHeight;
        }

        private void UpdateDPI()
        {
            DPIX = writeableBitmap.DpiX;
            DPIY = writeableBitmap.DpiY;
        }

        private void AllocateContents()
        {
            var size = writeableBitmap.PixelWidth * writeableBitmap.PixelHeight;
            contents.Allocate(size);
        }

        private int GetTailAddressOffset(WriteableBitmap wb)
        {
            return (int)(4 - wb.BackBufferStride % 4) % 4;
        }

        protected void DecodeImageBgr32()
        {
            try
            {
                writeableBitmap.Lock();
                unsafe
                {
                    byte* pBackBuffer = (byte*)(writeableBitmap.BackBuffer);
                    int currentPixelIndex = 0; 
                    byte[] r = contents.R;
                    byte[] g = contents.G;
                    byte[] b = contents.B;
                    int tailAddressOffset = GetTailAddressOffset(writeableBitmap);
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
                writeableBitmap.Unlock();
            }
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
