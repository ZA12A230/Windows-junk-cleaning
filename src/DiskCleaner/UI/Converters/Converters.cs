using Microsoft.UI.Xaml.Data;

namespace DiskCleaner.UI.Converters;

public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is long bytes)
        {
            return FormatSize(bytes);
        }
        
        return "0 B";
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
    
    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }
}

public class BoolNegationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return !b;
        return false;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return !b;
        return false;
    }
}

public class FileTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // TODO: Implement color conversion based on file type
        return "#000000";
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
