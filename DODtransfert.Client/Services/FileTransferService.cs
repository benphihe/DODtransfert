using System.IO;
using DODtransfert.Shared;
using DODtransfert.Shared.Models;

namespace DODtransfert.Client.Services;

public class FileTransferService
{
    public List<FileItem> GetFilesFromPaths(List<string> filePaths)
    {
        var files = new List<FileItem>();

        foreach (var path in filePaths)
        {
            if (!File.Exists(path)) continue;

            var fileInfo = new FileInfo(path);
            var extension = fileInfo.Extension.ToLower();
            
            var isImage = Constants.AllowedImageExtensions.Contains(extension);
            var isPdf = Constants.AllowedPdfExtensions.Contains(extension);

            if (!isImage && !isPdf) continue; // Skip unsupported files

            if (fileInfo.Length > Constants.MaxFileSize) continue; // Skip too large files

            files.Add(new FileItem
            {
                FilePath = path,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                FileType = extension,
                IsImage = isImage,
                IsPdf = isPdf
            });
        }

        return files;
    }

    public bool ValidateFilesForProductTransfer(List<FileItem> files, out string errorMessage)
    {
        errorMessage = string.Empty;

        var photos = files.Where(f => f.IsImage).ToList();
        var pdfs = files.Where(f => f.IsPdf).ToList();

        if (photos.Count == 0)
        {
            errorMessage = "Au moins une photo est requise";
            return false;
        }

        if (pdfs.Count == 0)
        {
            errorMessage = "Au moins un PDF est requis";
            return false;
        }

        return true;
    }
}
