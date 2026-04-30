namespace Tablitsya3.Models.Scanning
{
    /// <summary>
    /// Модель сканера, підключеного до станції
    /// </summary>
    public enum ScannerModel
    {
        /// <summary>Звичайний ручний сканер (HID-клавіатура)</summary>
        Generic = 0,
        /// <summary>Newland FS-40 (стаціонарний presentation-сканер)</summary>
        NewlandFS40 = 1,
        /// <summary>Honeywell Voyager (USB)</summary>
        HoneywellVoyager = 2,
        /// <summary>Datalogic QuickScan</summary>
        DatalogicQuickScan = 3,
        /// <summary>Zebra DS</summary>
        ZebraDS = 4,
        /// <summary>Інша модель</summary>
        Other = 99
    }

    /// <summary>
    /// Тип фізичного підключення сканера до станції
    /// </summary>
    public enum ScannerConnectionType
    {
        /// <summary>Ручний ввід / HID-клавіатура без налаштувань (focus + Enter)</summary>
        Manual = 0,
        /// <summary>USB HID Keyboard Wedge (VID/PID опціонально)</summary>
        UsbHid = 1,
        /// <summary>USB / RS-232 Serial (COM-порт + baud rate)</summary>
        UsbSerial = 2,
        /// <summary>Bluetooth HID (як клавіатура)</summary>
        BluetoothHid = 3,
        /// <summary>Bluetooth SPP (MAC-адреса)</summary>
        BluetoothSpp = 4,
        /// <summary>Wi-Fi / TCP socket (IP:port)</summary>
        WifiTcp = 5,
        /// <summary>HTTP webhook (POST на endpoint)</summary>
        NetworkHttp = 6,
        /// <summary>Web Serial API (з браузера)</summary>
        WebSerial = 7,
        /// <summary>Web HID API (з браузера)</summary>
        WebHid = 8
    }

    public static class ScannerEnumExtensions
    {
        public static string GetDisplayName(this ScannerModel model) => model switch
        {
            ScannerModel.Generic => "Звичайний ручний",
            ScannerModel.NewlandFS40 => "Newland FS-40",
            ScannerModel.HoneywellVoyager => "Honeywell Voyager",
            ScannerModel.DatalogicQuickScan => "Datalogic QuickScan",
            ScannerModel.ZebraDS => "Zebra DS",
            ScannerModel.Other => "Інша модель",
            _ => model.ToString()
        };

        public static string GetDisplayName(this ScannerConnectionType t) => t switch
        {
            ScannerConnectionType.Manual => "Ручний / HID-клавіатура (без налаштувань)",
            ScannerConnectionType.UsbHid => "USB HID (клавіатурна емуляція)",
            ScannerConnectionType.UsbSerial => "USB / RS-232 Serial (COM-порт)",
            ScannerConnectionType.BluetoothHid => "Bluetooth HID",
            ScannerConnectionType.BluetoothSpp => "Bluetooth SPP",
            ScannerConnectionType.WifiTcp => "Wi-Fi / TCP socket",
            ScannerConnectionType.NetworkHttp => "HTTP webhook",
            ScannerConnectionType.WebSerial => "Web Serial API (браузер)",
            ScannerConnectionType.WebHid => "Web HID API (браузер)",
            _ => t.ToString()
        };
    }
}
