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
#nullable enable

namespace AzureOCR
{
    public class ImageConsistencyClient
    {
        private Image<Bgr, Byte>? LastImage { get; set; }

        public bool TestAndSetCurrentImage(string imagePath)
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
            Console.WriteLine("minValues[0]: " + minValues[0]);
            LastImage = current;

            // TODO ????
            return minValues[0] >= 0.995;
        }
    }
}
