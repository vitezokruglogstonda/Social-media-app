using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Instakilogram.RequestResponse
{
    public class ImageAsBase64
    {
        public string FileName { get; set; }
        public string Base64Content { get; set; }

        public string? CallerEmail { get; set; }
        public ImageAsBase64()
        {
        }
        public ImageAsBase64(ImageAsBase64 copy)
        {
            this.FileName = copy.FileName;
            this.Base64Content = copy.Base64Content;
        }
    }
}
