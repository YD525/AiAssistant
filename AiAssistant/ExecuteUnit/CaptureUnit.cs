using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AiAssistant.ExecuteSandbox;
using static AiAssistant.ExecuteUnit.UnitHelper;
using System.Collections.Generic;

namespace AiAssistant.ExecuteUnit
{
    public class CaptureUnit
    {
        public bool Enable = false;

        #region Capability Manifest (AI readable)

        public static List<CapabilityInfo> CapabilityManifest = new List<CapabilityInfo>
        {
        new CapabilityInfo
        {
            Name        = "CaptureScreenToBase64",
            Description = "Capture the current screen and return a JPEG image encoded as Base64. Use this if you need to visually inspect the screen before deciding further actions such as mouse operations.",
            Params      = new List<ParameterInfo>
            {
                new ParameterInfo { Name = "Quality", Type = "int", Description = "JPEG quality (1-100). Recommended 80-90 for balance." }
            }
        }
        };

        #endregion

        #region Public API (sandboxed)

        /// <summary>
        /// Captures the full virtual screen and returns a Base64-encoded JPEG image.
        /// AI can analyze this image and decide whether to perform mouse actions.
        /// </summary>
        public string CaptureScreenToBase64(int Quality = 90)
            => Sandbox.Exec(nameof(CaptureScreenToBase64), () =>
            {
                var Bounds = SystemInformation.VirtualScreen;

                using (var Bmp = new Bitmap(Bounds.Width, Bounds.Height))
                {
                    using (var G = Graphics.FromImage(Bmp))
                    {
                        G.CopyFromScreen(Bounds.Left, Bounds.Top, 0, 0, Bounds.Size);
                    }

                    using (var Memory = new MemoryStream())
                    {
                        var JpegCodec = ImageCodecInfo.GetImageEncoders()
                            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);

                        if (JpegCodec == null)
                            throw new Exception("JPEG codec not found");

                        var EncoderParams = new EncoderParameters(1);
                        EncoderParams.Param[0] =
                            new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, Quality);

                        Bmp.Save(Memory, JpegCodec, EncoderParams);

                        return Convert.ToBase64String(Memory.ToArray());
                    }
                }
            }, Quality);

        #endregion
    }
}
