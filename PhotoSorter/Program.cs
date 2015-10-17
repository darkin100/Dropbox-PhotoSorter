using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Directory = System.IO.Directory;

namespace PhotoSync
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // enumerate files in camera uploads folder
            //Copy them into new folder
            //photo/year/year-month-day/photo

            var dropBoxPath = ConfigurationManager.AppSettings["dropbox-folder"];
            var targetPhotosFolder = Path.Combine(dropBoxPath, "Photos");
            var sourceCameraUploadsFolder = Path.Combine(dropBoxPath, "Camera Uploads");


            var counter = EnumerateFilesInDirectory(sourceCameraUploadsFolder, targetPhotosFolder);

            counter += EnumerateDirectoriesInRootRecursive(sourceCameraUploadsFolder, targetPhotosFolder);

            Console.WriteLine($"Processed {counter} files");
        }

        private static int EnumerateDirectoriesInRootRecursive(string sourceCameraUploadsFolder, string targetPhotosFolder)
        {
            var directories = Directory.EnumerateDirectories(sourceCameraUploadsFolder, "*", SearchOption.AllDirectories);

            return directories.Sum(directory => EnumerateFilesInDirectory(directory, targetPhotosFolder));
        }

        private static int EnumerateFilesInDirectory(string sourceCameraUploadsFolder, string targetPhotosFolder)
        {
            var files = Directory.EnumerateFiles(sourceCameraUploadsFolder);

            var counter = 0;
            foreach (var file in files)
            {
                if (Path.GetExtension(file) == ".dropbox")
                {
                    continue;
                }

                var time = File.GetCreationTime(file);
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
                counter++;
            }

            return counter;
        }
    }
}