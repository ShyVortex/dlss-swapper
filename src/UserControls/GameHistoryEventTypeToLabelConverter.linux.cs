using System;
using Avalonia.Data.Converters;
using DLSS_Swapper.Data;

namespace DLSS_Swapper.Converters;

public class GameHistoryEventTypeToLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        GameHistoryEventType? type = null;

        if (value is GameHistoryEventType eventType)
        {
            type = eventType;
        }
        else if (value is int intVal && Enum.IsDefined(typeof(GameHistoryEventType), intVal))
        {
            type = (GameHistoryEventType)intVal;
        }
        else if (value is long longVal && Enum.IsDefined(typeof(GameHistoryEventType), (int)longVal))
        {
            type = (GameHistoryEventType)(int)longVal;
        }
        else if (value != null && Enum.TryParse<GameHistoryEventType>(value.ToString(), out var parsed))
        {
            type = parsed;
        }

        if (type.HasValue)
        {
            return type.Value switch
            {
                GameHistoryEventType.DLLSwapped => "DLL swapped",
                GameHistoryEventType.DLLReset => "DLL reset",
                GameHistoryEventType.DLLDetected => "DLL detected",
                GameHistoryEventType.DLLChangedExternally => "DLL changed externally",
                GameHistoryEventType.DLLBackupRemoved => "DLL backup removed",
                _ => "Unknown"
            };
        }

        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
