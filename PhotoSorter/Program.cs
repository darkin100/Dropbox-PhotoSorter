using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Tiff;
using Directory = System.IO.Directory;

namespace PhotoSync
{
    class Program
    {
        static void Main(string[] args)
        {
            // enumerate files in camera uploads folder
            //Copy them into new folder
            //photo/year/year-month-day/photo

            var dropBoxPath = ConfigurationManager.AppSettings["dropbox-folder"];
            var targetPhotosFolder = Path.Combine(dropBoxPath, "Photos");
            var sourceCameraUploadsFolder = Path.Combine(dropBoxPath, "Camera Uploads");

            var files = Directory.EnumerateFiles(sourceCameraUploadsFolder);
           
            foreach (var file in files)
            {
                if (Path.GetExtension(file) == ".dropbox")
                {
                    continue;
                }

                DateTime time = File.GetCreationTime(file);
                try
                {
                    var directories = ImageMetadataReader.ReadMetadata(file);

                    var subIfdDirectory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                    var dateTime = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTime);

                    if (!string.IsNullOrEmpty(dateTime))
                    {
                        time = DateTime.ParseExact(dateTime, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);

                        Console.WriteLine(time);

                        Console.WriteLine(time.Year);
                    }
                }
                catch (ImageProcessingException ex)
                {
                }

                var yearPath = Path.Combine(targetPhotosFolder, time.Year.ToString());

                Directory.CreateDirectory(yearPath);

                var dayFolderName = time.ToString("yyyy-MM-dd");

                var dayPath = Path.Combine(yearPath, dayFolderName);

                Directory.CreateDirectory(dayPath);

                var fileName = Path.GetFileName(file);

                var targetFilePath = Path.Combine(dayPath, fileName);

                File.Move(file, targetFilePath);


            }
        }
    }
}
