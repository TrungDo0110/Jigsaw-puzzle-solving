using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;



public class ImageUtils
{
    public static void CopyToBitmap(smcs.IImageBitmap srcBitmap, ref Bitmap bitmap, ref BitmapData bd, ref PixelFormat pixelFormat, ref Rectangle rect, ref UInt32 pixelType)
    {
        UInt32 sizeX, sizeY;
        srcBitmap.GetSize(out sizeX, out sizeY);
        UInt32 newPixelType;
        srcBitmap.GetPixelType(out newPixelType);

        if (bitmap == null || pixelType != newPixelType || (bitmap.Height != (int)sizeY) || (bitmap.Width != (int)sizeX))
        {
            pixelType = newPixelType;

            if (((smcs.GVSP_PIXEL_TYPES)pixelType == smcs.GVSP_PIXEL_TYPES.GVSP_PIX_BGR8_PACKED) || ((smcs.GVSP_PIXEL_TYPES)pixelType == smcs.GVSP_PIXEL_TYPES.GVSP_PIX_RGB8_PACKED))
            {
                pixelFormat = PixelFormat.Format24bppRgb;
            }
            else if (((smcs.GVSP_PIXEL_TYPES)pixelType == smcs.GVSP_PIXEL_TYPES.GVSP_PIX_BGRA8_PACKED) || ((smcs.GVSP_PIXEL_TYPES)pixelType == smcs.GVSP_PIXEL_TYPES.GVSP_PIX_RGBA8_PACKED))
            {
                pixelFormat = PixelFormat.Format32bppRgb;
            }
            else if (smcs.CameraSuite.GvspGetBitsPerPixel((smcs.GVSP_PIXEL_TYPES)pixelType) == 8)
            {
                pixelFormat = PixelFormat.Format8bppIndexed;
            }
            else if (smcs.CameraSuite.GvspGetBitsDepth((smcs.GVSP_PIXEL_TYPES)pixelType) == 16)
            {
                pixelFormat = PixelFormat.Format8bppIndexed;
            }
            else
            {
                return;
            }

            // create bitmap
            bitmap = new Bitmap((int)sizeX, (int)sizeY, pixelFormat);
            rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            if (pixelFormat == PixelFormat.Format8bppIndexed)
            {
                // set palette
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bitmap.Palette = palette;
            }
        }

        // copy camera image to bitmap
        bd = bitmap.LockBits(rect, ImageLockMode.ReadWrite, pixelFormat);
        byte[] rgbValues = new byte[bd.Stride];
        if (smcs.CameraSuite.GvspGetBitsDepth((smcs.GVSP_PIXEL_TYPES)newPixelType) == 16)
        {
            ChangeBitDepth(srcBitmap, ref bd);
        }
        else
        {
            for (UInt32 y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(srcBitmap.GetRawData(y), rgbValues, 0, (int)srcBitmap.GetLineSize());
                Marshal.Copy(rgbValues, 0, (IntPtr)((Int64)bd.Scan0 + (y * bd.Stride)), (int)srcBitmap.GetLineSize());
            }
        }
        if (((smcs.GVSP_PIXEL_TYPES)pixelType == smcs.GVSP_PIXEL_TYPES.GVSP_PIX_RGB8_PACKED)
            || ((smcs.GVSP_PIXEL_TYPES)pixelType == smcs.GVSP_PIXEL_TYPES.GVSP_PIX_RGBA8_PACKED))
            SwitchRB(pixelType, ref bd);
    }


    // to show 16 bit images
    public static void ChangeBitDepth(smcs.IImageBitmap srcBitmap, ref BitmapData bd)
    {
        UInt32 pixNew, pixOld, x, y;
        byte[] oldValues = new byte[(int)srcBitmap.GetLineSize()];
        byte[] newValues = new byte[bd.Width];

        for (y = 0; y < bd.Height; y++)
        {
            pixNew = 0;
            pixOld = 1;
            Marshal.Copy(srcBitmap.GetRawData(y), oldValues, 0, (int)srcBitmap.GetLineSize());
            for (x = 0; x < bd.Width; x++)
            {
                newValues[pixNew] = oldValues[pixOld];
                pixNew++;
                pixOld += 2;
            }
            Marshal.Copy(newValues, 0, (IntPtr)((int)bd.Scan0 + (y * bd.Stride)), bd.Width);
        }
    }



    public static void SwitchRB(UInt32 pixelType, ref BitmapData bd)
    {
        UInt32 bytePerPixel = 3;
        if ((smcs.GVSP_PIXEL_TYPES)pixelType == smcs.GVSP_PIXEL_TYPES.GVSP_PIX_RGBA8_PACKED)
            bytePerPixel = 4;

        UInt32 pix, x, y;
        Int32 totalSize = (bd.Stride * bd.Height);
        byte[] oldValues = new byte[totalSize];
        byte[] newValues = new byte[totalSize];

        Marshal.Copy(bd.Scan0, oldValues, 0, totalSize);

        pix = 0;
        for (y = 0; y < bd.Height; y++)
        {
            for (x = 0; x < bd.Width; x++)
            {
                newValues[pix] = oldValues[pix + 2];
                newValues[pix + 1] = oldValues[pix + 1];
                newValues[pix + 2] = oldValues[pix];
                if (bytePerPixel == 4)
                    newValues[pix + 3] = oldValues[pix + 3];
                pix += bytePerPixel;
            }
        }
        Marshal.Copy(newValues, 0, bd.Scan0, totalSize);
    }
}