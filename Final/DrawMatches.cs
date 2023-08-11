using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;

namespace Final
{
    public static class DrawMatches
    {
        public static void FindMatch(Mat modelImage, Mat observedImage, out long matchTime, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography)
        {
            int k = 2;
            double uniquenessThreshold = 0.80;

            Stopwatch watch;
            homography = null;

            modelKeyPoints = new VectorOfKeyPoint();
            observedKeyPoints = new VectorOfKeyPoint();

            using (UMat uModelImage = modelImage.GetUMat(AccessType.Read))
            using (UMat uObservedImage = observedImage.GetUMat(AccessType.Read))
            {
                KAZE featureDetector = new KAZE();

                //extract features from the object image
                Mat modelDescriptors = new Mat();
                featureDetector.DetectAndCompute(uModelImage, null, modelKeyPoints, modelDescriptors, false);

                watch = Stopwatch.StartNew();

                // extract features from the observed image
                Mat observedDescriptors = new Mat();
                featureDetector.DetectAndCompute(uObservedImage, null, observedKeyPoints, observedDescriptors, false);

                // Bruteforce, slower but more accurate
                // You can use KDTree for faster matching with slight loss in accuracy
                using (Emgu.CV.Flann.LinearIndexParams ip = new Emgu.CV.Flann.LinearIndexParams())
                using (Emgu.CV.Flann.SearchParams sp = new SearchParams())
                using (DescriptorMatcher matcher = new FlannBasedMatcher(ip, sp))
                {
                    matcher.Add(modelDescriptors);
                    try
                    {
                        matcher.KnnMatch(observedDescriptors, matches, k, null);
                    }
                    catch
                    {

                    }
                    mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(255));
                    Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                    int nonZeroCount = CvInvoke.CountNonZero(mask);
                    if (nonZeroCount >= 4)
                    {
                        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints,
                            matches, mask, 1.5, 20);
                        if (nonZeroCount >= 4)
                            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints,
                                observedKeyPoints, matches, mask, 2);
                    }
                }
                watch.Stop();
            }
            matchTime = watch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Draw the model image and observed image, the matched features and homography projection.
        /// </summary>
        /// <param name="modelImage">The model image</param>
        /// <param name="observedImage">The observed image</param>
        /// <param name="matchTime">The output total time for computing the homography matrix.</param>
        /// <returns>The model image and observed image, the matched features and homography projection.</returns>
        public static Mat Draw(Mat modelImage, Mat observedImage, out long matchTime, out double CenX, out double CenY, out double angle)
        {
            CenX = 0; CenY = 0; angle = 0;
            Mat homography;
            VectorOfKeyPoint modelKeyPoints;
            VectorOfKeyPoint observedKeyPoints;
            using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
            {
                Mat mask;
                FindMatch(modelImage, observedImage, out matchTime, out modelKeyPoints, out observedKeyPoints, matches,
                   out mask, out homography);

                //Draw the matched keypoints
                Mat result = new Mat();
                Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints,
                   matches, result, new MCvScalar(0, 255, 0), new MCvScalar(0, 0, 255), mask);

                #region draw the projected region on the image

                if (homography != null)
                {
                    //draw a rectangle along the projected model
                    Rectangle rect = new Rectangle(Point.Empty, modelImage.Size);
                    PointF[] pts = new PointF[]
                    {
                        new PointF(rect.Left, rect.Bottom),
                        new PointF(rect.Right, rect.Bottom),
                        new PointF(rect.Right, rect.Top),
                        new PointF(rect.Left, rect.Top)
                    };
                    pts = CvInvoke.PerspectiveTransform(pts, homography);

#if NETFX_CORE
               Point[] points = Extensions.ConvertAll<PointF, Point>(pts, Point.Round);
#else
                    Point[] points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
#endif
                    using (VectorOfPoint vp = new VectorOfPoint(points))
                    {
                        CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
                    }
                    //Console.WriteLine(Convert.ToString(points[0]));
                    //Console.WriteLine(Convert.ToString(points[1]));
                    //Console.WriteLine(Convert.ToString(points[2]));
                    //Console.WriteLine(Convert.ToString(points[3]));
                    CenX = (points[0].X + points[1].X + points[2].X + points[3].X) / 4;
                    CenY = (points[0].Y + points[1].Y + points[2].Y + points[3].Y) / 4;
                    if (points[1].Y - points[0].Y < 0)
                    {
                        angle = 360 * Math.Acos((points[1].X - points[0].X)
                            / (Math.Pow((points[1].X - points[0].X) * (points[1].X - points[0].X)
                            + (points[1].Y - points[0].Y) * (points[1].Y - points[0].Y), 0.5))) / (2 * 3.141592);
                    }
                    else
                    {
                        angle = -360 * Math.Acos((points[1].X - points[0].X)
                            / (Math.Pow((points[1].X - points[0].X) * (points[1].X - points[0].X)
                            + (points[1].Y - points[0].Y) * (points[1].Y - points[0].Y), 0.5))) / (2 * 3.141592);
                    }
                    //double angle1 = 360 * Math.Acos((points[1].X - points[0].X)
                    //    / (Math.Pow((points[1].X - points[0].X) * (points[1].X - points[0].X)
                    //    + (points[1].Y - points[0].Y) * (points[1].Y - points[0].Y), 0.5))) / (2 * 3.141592);
                    //double angle2 = 360 * Math.Acos(-(points[2].Y - points[1].Y)
                    //    / (Math.Pow((points[2].X - points[1].X) * (points[2].X - points[1].X)
                    //    + (points[2].Y - points[1].Y) * (points[2].Y - points[1].Y), 0.5))) / (2 * 3.141592);
                    //double angle3 = 360 * Math.Acos(-(points[3].X - points[2].X)
                    //    / (Math.Pow((points[3].X - points[2].X) * (points[3].X - points[2].X)
                    //    + (points[3].Y - points[2].Y) * (points[3].Y - points[2].Y), 0.5))) / (2 * 3.141592);
                    //double angle4 = 360 * Math.Acos(-(points[3].Y - points[0].Y)
                    //    / (Math.Pow((points[3].X - points[0].X) * (points[3].X - points[0].X)
                    //    + (points[3].Y - points[0].Y) * (points[3].Y - points[0].Y), 0.5))) / (2 * 3.141592);
                    //angle = (angle1 + angle2 + angle3 + angle4) / 4;
                }

                #endregion

                return result;

            }
        }
    }
}
