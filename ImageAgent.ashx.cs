using System;
using System.Web;
using System.Drawing;
using System.Web.Caching;
using System.Drawing.Imaging;

namespace ImageAgent
{
    /// <summary>
    /// ImageAgent 的摘要说明
    /// </summary>
    public class ImageAgent : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            string imgUrl = context.Request.QueryString["img"];
            int width = context.Request.QueryString["w"] != null ? int.Parse(context.Request.QueryString["w"]) : 0;
            int height = context.Request.QueryString["h"] != null ? int.Parse(context.Request.QueryString["h"]) : 0;
            bool square = context.Request.QueryString["s"] != null ? true : false;

            string key = "img_" + imgUrl + "_" + width.ToString() + "_" + height.ToString() + "_" + square.ToString();

            Cache cache = System.Web.HttpContext.Current.Cache;
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            if (cache[key] == null)
            {
                Image image = null;
                string imgPath = context.Server.MapPath(imgUrl);
                image = Image.FromFile(imgPath);

                int _width = image.Size.Width;
                int _height = image.Size.Height;

                if (height == 0)
                {
                    double _zoom_factor = (double)_width / width;
                    height = Convert.ToInt32(_height / _zoom_factor);
                }
                else if (width == 0)
                {
                    double _zoom_factor = (double)_height / height;
                    width = Convert.ToInt32(_width / _zoom_factor);
                }
                else
                {
                    double _zoom_factor = (double)_width / width;
                    if (_height / height > _zoom_factor) _zoom_factor = (double)_height / height;
                    width = Convert.ToInt32(_width / _zoom_factor);
                    height = Convert.ToInt32(_height / _zoom_factor);
                }

                if (width == 0 && height == 0)
                {
                    width = _width;
                    height = _height;
                }

                if (square)
                {
                    image = squareImage(image);
                    if (width < height)
                        width = height;
                    if (height < width)
                        height = width;
                }

                image = resizeImage(image, new Size(width, height));

                System.Drawing.Imaging.EncoderParameters encoderParams = new System.Drawing.Imaging.EncoderParameters();
                long[] quality = new long[1];

                quality[0] = 100; //0 to 100 最高质量为100 
                System.Drawing.Imaging.EncoderParameter encoderParam = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                encoderParams.Param[0] = encoderParam;

                ImageCodecInfo[] arrayICI = ImageCodecInfo.GetImageEncoders();//获得包含有关内置图像编码解码器的信息的ImageCodecInfo 对象。 
                ImageCodecInfo jpegICI = null;
                for (int x = 0; x < arrayICI.Length; x++)
                {
                    if (arrayICI[x].FormatDescription.Equals("JPEG"))
                    {
                        jpegICI = arrayICI[x];//设置JPEG编码 
                        break;
                    }
                }
                image.Save(ms, jpegICI, encoderParams);

                cache.Insert(key, ms);
                image.Dispose();
            }
            else
            {
                ms = (System.IO.MemoryStream)cache[key];
            }

            context.Response.ClearContent();
            context.Response.ContentType = "image/jpeg";
            context.Response.Cache.SetCacheability(HttpCacheability.ServerAndPrivate);
            context.Response.Cache.SetMaxAge(new TimeSpan(DateTime.Now.AddDays(30).Ticks - DateTime.Now.Ticks));
            context.Response.BinaryWrite(ms.ToArray());

        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        public static Image resizeImage(Image imgToResize, Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }

        public static Image squareImage(Image imgToSquare)
        {
            Bitmap b = (new Bitmap(imgToSquare));
            int size = (b.Width >= b.Height) ? b.Height : b.Width;
            Rectangle r = new Rectangle((b.Width - size) / 2, (b.Height - size) / 2, size, size);
            return (Image)b.Clone(r, b.PixelFormat);

        }

    }
}