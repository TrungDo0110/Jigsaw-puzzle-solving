using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Emgu.CV.CvEnum;

namespace Final
{
    public partial class Form1 : Form
    {
        //Variables for Image processing
        double resolution;
        Bitmap bitmap;
        Image<Bgr, byte> imgInput;
        Image<Bgr, byte> imgTemp;
        List<Image<Bgr, byte>> img = new List<Image<Bgr, byte>>();
        List<Label> la1 = new List<Label>();
        List<Label> la2 = new List<Label>();
        List<Label> la3 = new List<Label>();
        List<Label> la4 = new List<Label>();
        double totalTime = 0;
        int piecesQuan = 0;

        //Variables for Camera
        smcs.IDevice m_device;
        Rectangle m_rect;
        smcs.IImageProcAPI m_imageProcApi;
        PixelFormat m_pixelFormat;

        smcs.IAlgorithm m_colorPipelineAlg;
        smcs.IParams m_colorPipelineParams;
        smcs.IResults m_colorPipelineResults;
        smcs.IImageBitmap m_colorPipelineBitmap;

        smcs.IAlgorithm m_changeBitDepthAlg;
        smcs.IParams m_changeBitDepthParams;
        smcs.IResults m_changeBitDepthResults;
        smcs.IImageBitmap m_changeBitDepthBitmap;

        bool m_defaultGainNotSet;
        double m_defaultGain;

        //Variable for Calibration
        Matrix<float> mapx = new Matrix<float>(3120, 2164);  //mapping matrix
        Matrix<float> mapy = new Matrix<float>(3120, 2164);
        Image<Bgr, byte> orig_image;
        IntrinsicCameraParameters intrinsicParam = new IntrinsicCameraParameters(5);

        //Variable for Robot


        public Form1()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            double caliReal = Convert.ToDouble(textBox2.Text);
            double caliPixel = Convert.ToDouble(textBox3.Text); //in lab: 370. In robot 224
            resolution = caliReal / caliPixel;
            // initialize GigEVision API
            smcs.CameraSuite.InitCameraAPI();
            smcs.ICameraAPI smcsVisionApi = smcs.CameraSuite.GetCameraAPI();

            if (!smcsVisionApi.IsUsingKernelDriver())
            {
                Text = Text + " (Warning: Smartek Filter Driver not loaded.)";
            }

            // initialize ImageProcessing API
            smcs.CameraSuite.InitImageProcAPI();
            m_imageProcApi = smcs.CameraSuite.GetImageProcAPI();

            m_colorPipelineAlg = m_imageProcApi.GetAlgorithmByName("ColorPipeline");
            m_colorPipelineAlg.CreateParams(ref m_colorPipelineParams);
            m_colorPipelineAlg.CreateResults(ref m_colorPipelineResults);
            m_imageProcApi.CreateBitmap(ref m_colorPipelineBitmap);

            m_changeBitDepthAlg = m_imageProcApi.GetAlgorithmByName("ChangeBitDepth");
            m_changeBitDepthAlg.CreateParams(ref m_changeBitDepthParams);
            m_changeBitDepthAlg.CreateResults(ref m_changeBitDepthResults);
            m_imageProcApi.CreateBitmap(ref m_changeBitDepthBitmap);


            label1.Text = "No camera connected";

            // discover all devices on network
            smcsVisionApi.FindAllDevices(3.0);
            smcs.IDevice[] devices = smcsVisionApi.GetAllDevices();
            if (devices.Length <= 0) return;

            m_device = devices[0];

            // to change number of images in image buffer from default 10 images 
            // call SetImageBufferFrameCount() method before Connect() method
            //m_device.SetImageBufferFrameCount(20);

            if (m_device == null || !m_device.Connect()) return;

            // disable trigger mode
            bool status = m_device.SetStringNodeValue("TriggerMode", "Off");
            // set continuous acquisition mode
            status = m_device.SetStringNodeValue("AcquisitionMode", "Continuous");
            // start acquisition
            status = m_device.SetIntegerNodeValue("TLParamsLocked", 1);
            status = m_device.CommandNodeExecute("AcquisitionStart");

            timer1.Enabled = true;

            label1.Text = "Camera address:";
            label2.Text = Common.IpAddrToString(m_device.GetIpAddress());

            double exposure, gain;
            m_device.GetFloatNodeValue("Gain", out gain);
            label3.Text = "Gain:";
            label4.Text = String.Format("{0:0.00}", gain);


            m_device.GetFloatNodeValue("ExposureTime", out exposure);
            label5.Text = "Exposure:";
            label6.Text = String.Format("{0:0.00}", exposure);

            m_defaultGainNotSet = true;
            m_defaultGain = 0.0;
        }

        private void OnTimer(object sender, EventArgs e)
        {
            smcs.IImageInfo imageInfo = null;
            if (!m_device.GetImageInfo(ref imageInfo)) return;

            UInt32 pixelType;
            imageInfo.GetPixelType(out pixelType);
            var depth = smcs.CameraSuite.GvspGetBitsDepth((smcs.GVSP_PIXEL_TYPES)pixelType);
            var image = (smcs.IImageBitmap)imageInfo;

            if (depth > 8)
            {
                // to show image we change bit depth to 8 bit
                m_changeBitDepthParams.SetIntegerNodeValue("BitDepth", 8);
                m_imageProcApi.ExecuteAlgorithm(m_changeBitDepthAlg, image, m_changeBitDepthBitmap, m_changeBitDepthParams, m_changeBitDepthResults);
                image = m_changeBitDepthBitmap;
            }

            m_imageProcApi.ExecuteAlgorithm(m_colorPipelineAlg, image, m_colorPipelineBitmap, m_colorPipelineParams, m_colorPipelineResults);

            bitmap = (Bitmap)pictureBox.Image;
            BitmapData bd = null;

            ImageUtils.CopyToBitmap(m_colorPipelineBitmap, ref bitmap, ref bd, ref m_pixelFormat, ref m_rect, ref pixelType);

            if (bitmap != null)
            {
                //pictureBox.Height = bitmap.Height;
                //pictureBox.Width = bitmap.Width;
                pictureBox.Image = bitmap;
            }


            // display image
            if (bd != null)
                bitmap.UnlockBits(bd);

            pictureBox.Invalidate();

            // remove (pop) image from image buffer
            m_device.PopImage(imageInfo);
            // empty buffer
            m_device.ClearImageBuffer();
        }

        private void OnClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Enabled = false;
            if (m_device != null && m_device.IsConnected())
            {
                m_device.CommandNodeExecute("AcquisitionStop");
                m_device.SetIntegerNodeValue("TLParamsLocked", 0);
                m_device.Disconnect();
            }

            m_colorPipelineAlg.DestroyParams(m_colorPipelineParams);
            m_colorPipelineAlg.DestroyResults(m_colorPipelineResults);
            m_imageProcApi.DestroyBitmap(m_colorPipelineBitmap);

            m_changeBitDepthAlg.DestroyParams(m_changeBitDepthParams);
            m_changeBitDepthAlg.DestroyResults(m_changeBitDepthResults);
            m_imageProcApi.DestroyBitmap(m_changeBitDepthBitmap);

            smcs.CameraSuite.ExitImageProcAPI();
            smcs.CameraSuite.ExitCameraAPI();
        }
        private void SnapshotImage()
        {
            if (m_device.IsConnected())
            {
                m_device.ClearImageBuffer();
                // execute software trigger
                m_device.CommandNodeExecute("TriggerSoftware");
            }
        }
        private void Calibrate_Click(object sender, EventArgs e)
        {
            SnapshotImage();
            timer1.Enabled = false;
            orig_image = new Image<Bgr, byte>(bitmap);
            StreamReader sr = new StreamReader(@"C:\Users\quoct\Desktop\0713\instrinc.txt");
            string[] line = new string[9];
            for (int i = 0; i < 9; i++)
            {
                line[i] = sr.ReadLine();
                intrinsicParam.IntrinsicMatrix[i / 3, i % 3] = Convert.ToDouble(line[i]);
            }

            StreamReader sb = new StreamReader(@"C:\Users\quoct\Desktop\0713\distortion.txt");
            string[] line1 = new string[5];
            for (int i = 0; i < 5; i++)
            {
                line1[i] = sb.ReadLine();
                intrinsicParam.DistortionCoeffs[i, 0] = Convert.ToDouble(line1[i]);
            }

            intrinsicParam.InitUndistortMap(3120, 2164, out mapx, out mapy);
            CvInvoke.Remap(orig_image, orig_image, mapx, mapy, Inter.Cubic, BorderType.Constant, new MCvScalar(0));
            pictureBox.Image = orig_image.Bitmap;
            orig_image.Save(@"0SaveImage0.jpg");
            MessageBox.Show("Calibrate Complete");
            /*
            SnapshotImage();
            timer1.Enabled = false;
            orig_image = new Image<Bgr, byte>(bitmap);
            XmlDocument doc = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            XmlReader reader;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "*.xml|*.xml";
            if (DialogResult.OK == dlg.ShowDialog())
            {
                string str = dlg.FileName.Trim();
                reader = XmlReader.Create(str, settings);
                doc.Load(reader);
            }
            else
                return;

            //root node
            XmlNode xmlNode = doc.SelectSingleNode("opencv_storage");
            //chile node
            XmlNodeList xmlList = xmlNode.ChildNodes;
            foreach (XmlNode xmlnode in xmlList)
            {
                XmlNodeList xn = xmlnode.ChildNodes;
                //intrinsic and distortion param
                if ("matrix_left" == xmlnode.Name)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        intrinsicParam.IntrinsicMatrix[i / 3, i % 3] = Convert.ToDouble(xn.Item(i).InnerText);
                    }
                }
                else if ("distortion_left" == xmlnode.Name)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        intrinsicParam.DistortionCoeffs[i, 0] = Convert.ToDouble(xn.Item(i).InnerText);
                    }
                }
            }
            intrinsicParam.InitUndistortMap(1321, 911, out mapx, out mapy);
            CvInvoke.Remap(orig_image, orig_image, mapx, mapy, Inter.Cubic, BorderType.Constant, new MCvScalar(0));
            pictureBox.Image = orig_image.Bitmap;
            bitmap.Save(@"0SaveImage0.jpg");
            MessageBox.Show("Calibrate Complete");
              */
        }

        private void Detech_Click(object sender, EventArgs e)
        {
            double caliReal = Convert.ToDouble(textBox2.Text);
            double caliPixel = Convert.ToDouble(textBox3.Text); //in lab: 370. In robot 224
            resolution = caliReal / caliPixel;
            totalTime = 7000;
            for (int i = 0; i < piecesQuan; i++)
            {
                this.Controls.Remove(la1[i]); this.Controls.Remove(la2[i]);
                this.Controls.Remove(la3[i]); this.Controls.Remove(la4[i]);
            }

            //Robot
            //string filepath = @"C:\Users\quoct\Desktop\test.txt";
            string filepath = @"0test.txt";
            FileStream fs = new FileStream(filepath, FileMode.Create);
            StreamWriter sWriter = new StreamWriter(fs, Encoding.UTF8);

            //Image initialization
            imgInput = new Image<Bgr, byte>(@"0PuzzleAll1.jpg");
            //imgTemp = new Image<Bgr, byte>(@"C:\Users\quoct\Pictures\SaveImages\p1.bmp");
            imgTemp = new Image<Bgr, byte>(@"0SaveImage0.jpg");
            pictureBox.Image = imgTemp.Bitmap;

            Image<Gray, byte> imgOut = imgTemp.Convert<Gray, byte>().ThresholdBinary(new Gray(50), new Gray(255));
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            Mat hier = new Mat();
            CvInvoke.FindContours(imgOut, contours, hier, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
            piecesQuan = contours.Size;
            List<double> listArea = new List<double>();
            List<int> listIndex = new List<int>();
            if (contours.Size > 0)
            {
                for (int i = 0; i < contours.Size; i++)
                {
                    double area = CvInvoke.ContourArea(contours[i]);
                    if (area >= 10000)
                    {
                        listArea.Add(area);
                        //Console.WriteLine(area);
                        listIndex.Add(i);
                    }
                }
            }

            label13.Text = "Detech " + listIndex.Count + " pieces";
            piecesQuan = listIndex.Count;

            for (int i = 0; i < listIndex.Count; i++)
            {
                Label laNew1 = new Label(); Label laNew2 = new Label();
                Label laNew3 = new Label(); Label laNew4 = new Label();
                la1.Add(laNew1); la2.Add(laNew2);
                la3.Add(laNew3); la4.Add(laNew4);
                this.Controls.Add(la1[i]); this.Controls.Add(la2[i]);
                this.Controls.Add(la3[i]); this.Controls.Add(la4[i]);
                la1[i].Location = new System.Drawing.Point(850, 180 + i * 15); la1[i].AutoSize = true;
                la2[i].Location = new System.Drawing.Point(950, 180 + i * 15); la2[i].AutoSize = true;
                la3[i].Location = new System.Drawing.Point(1050, 180 + i * 15); la3[i].AutoSize = true;
                la4[i].Location = new System.Drawing.Point(1150, 180 + i * 15); la4[i].AutoSize = true;
            }
            for (int i = 0; i < listIndex.Count; i++)
            {
                Rectangle rect = CvInvoke.BoundingRectangle(contours[listIndex[i]]);
                imgTemp.ROI = rect;
                img.Add(imgTemp.Copy());
                imgTemp.ROI = Rectangle.Empty;


                double CenX, CenY, angle;
                Mat result; long matchTime;
                Detection(imgInput, img[i], out CenX, out CenY, out angle, out result, out matchTime);
                if ((CenX <= 1321) && (CenY <= 911) && (CenX > 0) && (CenY > 0))
                {
                    //double noPos = NoPos(CenX, CenY);
                    //la1[i].Text = Convert.ToString(noPos);
                    //la2[i].Text = Convert.ToString(Math.Round(angle, 2));
                    //la3[i].Text = Convert.ToString(matchTime);
                    //double X = ((rect.X + rect.Width + rect.X) / 2);
                    //double Y = ((rect.Y + rect.Height + rect.Y) / 2);
                    ////double X = Math.Round(resolution * (rect.X + rect.Width + rect.X) / 2);
                    ////double Y = Math.Round(resolution * (rect.Y + rect.Height + rect.Y) / 2);
                    //la4[i].Text = "X=" + X + " ;Y=" + Y;
                    //sWriter.WriteLine(noPos + " " + Math.Round(angle, 2) + " " + X + " " + Y);

                    double noPos = NoPos(CenX, CenY);
                    int noPosInt = Convert.ToInt16(noPos);
                    la1[noPosInt - 1].Text = Convert.ToString(noPos);
                    la2[noPosInt - 1].Text = Convert.ToString(Math.Round(angle, 2));
                    la3[noPosInt - 1].Text = Convert.ToString(matchTime);
                    double X = ((rect.X + rect.Width + rect.X) / 2);
                    double Y = ((rect.Y + rect.Height + rect.Y) / 2);
                    //double X = Math.Round(resolution * (rect.X + rect.Width + rect.X) / 2);
                    //double Y = Math.Round(resolution * (rect.Y + rect.Height + rect.Y) / 2);
                    //la4[noPosInt-1].Text = "X=" + X + "; Y=" + Y;
                    la4[noPosInt - 1].Text = X + " " + Y;
                    //sWriter.WriteLine(noPos + " " + Math.Round(angle, 2) + " " + X + " " + Y);
                }
                else
                {
                    MessageBox.Show("Can't detech pieces." + NoPos(CenX, CenY));
                }

                totalTime += matchTime;
            }
            label14.Text = "Toal Time: " + Convert.ToString(totalTime) + "ms";
            for (int j = 0; j < listIndex.Count; j++)
            {
                sWriter.WriteLine(la1[j].Text + " " + la2[j].Text + " " + la4[j].Text);
            }
            sWriter.Close();
            fs.Close();
            MessageBox.Show("Detech Complete");
        }
        private void Detection(Image<Bgr, byte> imgInput1, Image<Bgr, byte> img1, out double CenX, out double CenY, out double angle, out Mat result, out long matchTime)
        {
            Image<Gray, byte> imgInputGray = imgInput1.Convert<Gray, byte>();
            Image<Gray, byte> imgGray = img1.Convert<Gray, byte>();
            Mat matInputGray = imgInputGray.Mat;
            Mat matGray = imgGray.Mat;
            result = DrawMatches.Draw(matGray, matInputGray, out matchTime, out CenX, out CenY, out angle);
        }
        private double NoPos(double x, double y)
        {
            double NoPos = 0;
            double i = Math.Floor(x / 188.7) + 1;
            double j = Math.Floor(y / 182.2) + 1;
            NoPos = 7 * j - (7 - i);
            //Console.WriteLine(i+ " "+ j);
            return NoPos;
        }

        private void Capture_Click(object sender, EventArgs e)
        {
            SnapshotImage();
            timer1.Enabled = !timer1.Enabled;
            bitmap.Save(@"0SaveImage0.jpg");
            if (timer1.Enabled == false)
            {
                MessageBox.Show("Capture Complete");
            }
            else
            {
                MessageBox.Show("Recording");
            }
        }

        private void RobotControl_Click(object sender, EventArgs e)
        {
            Form2 f = new Form2();
            f.Show();
        }
    }
}
