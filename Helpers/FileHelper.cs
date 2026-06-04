namespace ProductivityApi.Helpers
{
    public static class FileHelper
    {

        public static async Task<(bool Success, string? FileName, string? Error)> SaveBase64FileAsync(string basePath, string fileName,string base64, string[] allowedExtensions, int maxSizeInMb = 5)
        {
            if (string.IsNullOrWhiteSpace(base64) || string.IsNullOrWhiteSpace(fileName))
                return (false, null, "File is empty");

            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
                return (false, null, "Invalid file format");

            try
            {
                var base64Data = base64.Contains(",")
                    ? base64.Split(',')[1]
                    : base64;

                var bytes = Convert.FromBase64String(base64Data);

                if (bytes.Length > maxSizeInMb * 1024 * 1024)
                    return (false, null, "File size exceeds limit");

                Directory.CreateDirectory(basePath);

                var fullPath = Path.Combine(basePath, fileName);

                await File.WriteAllBytesAsync(fullPath, bytes);

                return (true, fileName, null);
            }
            catch
            {
                return (false, null, "Invalid Base64 string");
            }
        }
        public static async Task<byte[]?> ReadFileAsync(string basePath, string fileName)
        {
            var fullPath = Path.Combine(basePath, fileName);

            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllBytesAsync(fullPath);
        }

        public static bool FileExists(string basePath, string fileName)
        {
            return File.Exists(Path.Combine(basePath, fileName));
        }

        public static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLower();

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }
    }
}