using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace MyFirstApp
{
    public partial class MainWindow : Window
    {
        private readonly IFaceServiceClient faceServiceClient = new FaceServiceClient(Constants.AZURE_FACE_API_KEY);

        public MainWindow()
        {
            InitializeComponent();
        }

        private async Task<FaceRectangle[]> UploadAndDetectFaces(string imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    var requiredFaceAttributes = new FaceAttributeType[] {
                        FaceAttributeType.Age,
                        FaceAttributeType.Gender,
                        FaceAttributeType.Smile,
                        FaceAttributeType.FacialHair,
                        FaceAttributeType.HeadPose,
                        FaceAttributeType.Glasses
                    };

                    var faces = await faceServiceClient.DetectAsync(imageFileStream,
                        returnFaceLandmarks: true,
                        returnFaceAttributes: requiredFaceAttributes);

                    var formattedFaces = faces.Select(face => new FaceFormatter(face)).OrderBy(p => p.pos);

                    foreach (var formattedFace in formattedFaces) {
                        Console.WriteLine(formattedFace.toString());
                    }

                    var faceRects = faces.Select(face => face.FaceRectangle);
                    var landmarks = faces.Select(face => face.FaceLandmarks);
                    return faceRects.ToArray();
                }
            }
            catch (Exception)
            {
                return new FaceRectangle[0];
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";
            bool? result = openDlg.ShowDialog(this);

            if (!(bool)result)
            {
                return;
            }

            string filePath = openDlg.FileName;

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            FacePhoto.Source = bitmapSource;

            Title = "Detecting...";
            FaceRectangle[] faceRects = await UploadAndDetectFaces(filePath);
            Title = String.Format("Detection Finished. {0} face(s) detected", faceRects.Length);

            if (faceRects.Length > 0)
            {
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;
                double resizeFactor = 96 / dpi;

                foreach (var faceRect in faceRects)
                {
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brushes.Red, 2),
                        new Rect(
                            faceRect.Left * resizeFactor,
                            faceRect.Top * resizeFactor,
                            faceRect.Width * resizeFactor,
                            faceRect.Height * resizeFactor
                            )
                    );
                }

                drawingContext.Close();
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);
                FacePhoto.Source = faceWithRectBitmap;
            }
        }

        class FaceFormatter
        {
            public FaceAttributes attributes;
            public double age;
            public String gender;
            public double smile;
            public FacialHair facialHair;
            public HeadPose headPose;
            public Glasses glasses;
            public int pos;

            public FaceFormatter(Face face)
            {
                attributes = face.FaceAttributes;
                age = attributes.Age;
                gender = attributes.Gender;
                smile = attributes.Smile;
                facialHair = attributes.FacialHair;
                headPose = attributes.HeadPose;
                glasses = attributes.Glasses;
                pos = face.FaceRectangle.Left + face.FaceRectangle.Width / 2;
            }

            public String toString()
            {
                return $"Age = {age}, gender = {gender}, smile = {attributes.Smile}, beard = {attributes.FacialHair.Beard}, glasses = {glasses}, position from left = {pos}";
            }
        }

    }
}
