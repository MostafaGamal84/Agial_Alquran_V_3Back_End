

using Microsoft.AspNetCore.Http;
using static Orbits.GeneralProject.BLL.Constants.DXConstants;

namespace NewProject.Shared;

public class ImageHelper
{

    public static bool CheckImageFileExtension(IFormFile img)
    {
        var format_ext_image = Path.GetExtension(img.FileName);
        string[] format_images = new string[] { ".jpg", ".jpeg", ".png" };
        if (!format_images.Contains(format_ext_image.ToLower()))
        {
            return false;
        }
        return true;
    }

    public static bool CheckImageExist(string Path)
    {
        return File.Exists($"wwwroot/{Constanties.IMAGE_UPLOAD_PATH}/{Path}");
    }

    public static string? UploadImage(IFormFile img, string? dirName)
    {
        Random rand = new Random();
        var format_ext_image = Path.GetExtension(img.FileName);
        var r = rand.Next(1000, int.Parse(DateTime.Now.ToString("yyyyyMMmmss"))).ToString();
        // string uniqueImge = r + "." + img.FileName.Split(format_ext_image)[1];
        string uniqueImge = r + format_ext_image;
        var dir = @$"./wwwroot/{Constanties.IMAGE_UPLOAD_PATH}/{dirName}";

        if (!System.IO.Directory.Exists(@$"{dir}"))
        {
            System.IO.Directory.CreateDirectory(@$"{dir}");
        }
        using (var obj = new FileStream(@$".\wwwroot\{Constanties.IMAGE_UPLOAD_PATH}\{dirName}\" + uniqueImge, FileMode.Create))
        {
            img.CopyTo(obj);
        }
        return dirName != null ? @$"{dirName}/{uniqueImge}" : uniqueImge;
    }

    public static string? ImageUrl(string? image)
    {
        if (image == null)
            return @"images/DefaultImage.jpeg";
        if (image.StartsWith("http") || image.StartsWith("https"))
        {
            return image;
        }

        return $@"{Constanties.IMAGE_UPLOAD_PATH}/{image}";
    }
}