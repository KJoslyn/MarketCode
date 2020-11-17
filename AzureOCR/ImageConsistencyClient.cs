using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV.OCR;
using Serilog;
#nullable enable

namespace AzureOCR
{
    public class ImageConsistencyClient
    {
        private Image<Bgr, Byte>? LastImage { get; set; }

        public void Init(string imagePath)
        {
            LastImage = new Image<Bgr, Byte>(imagePath);
        }

        public bool UpdateImageAndCheckHasChanged(string imagePath, double threshold = 0.99, bool? groundTruthChanged = null)
        {
            Image<Bgr, Byte> current = new Image<Bgr, Byte>(imagePath);

            //Tesseract ocr = new Tesseract();
            //ocr.SetImage(new Image<Bgr, Byte>(imagePath).Copy());
            //ocr.Recognize();
            //string boxText = ocr.GetBoxText();

            if (LastImage == null)
            {
                LastImage = current;
                return true;
            }
            Image<Gray, float> resultImage = LastImage.MatchTemplate(current, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed);
            double[] minValues, maxValues;
            Point[] minLocations, maxLocations;
            resultImage.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
            LastImage = current;

            string imageFile = imagePath.Substring(imagePath.LastIndexOf("/") + 1);
            double minValue = Math.Round(minValues[0], 4);
            //Log.Information("ImageFile: {ImageFile}, MinValue: {MinValue}", imageFile, minValues[0].ToString("0.0000"));
            if (groundTruthChanged != null)
            {
                Log.Information("ImageFile: {ImageFile}, MinValue: {MinValue}, GroundTruthChanged: {GroundTruthChanged}: ", imageFile, minValue, groundTruthChanged);
            } else
            {
                Log.Information("ImageFile: {ImageFile}, MinValue: {MinValue}", imageFile, minValue);
            }

            // TODO ????
            return minValues[0] <= threshold;
        }
    }
}
