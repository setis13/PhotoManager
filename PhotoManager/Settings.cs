using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoManager {
    public class Settings {
        public static List<string> PhotoExtensions = new List<string>() { ".jpg", ".jpeg" };
        public static List<string> VideoExtensions = new List<string>() { ".avi", ".mp4", ".mov", ".mts" };
        public static string SqlCmdLocation = @"C:\Program Files\Microsoft SQL Server\110\Tools\Binn\SQLCMD.EXE";
        public static string BackupLocation = @"C:\Program Files\Microsoft SQL Server\MSSQL12.SQLEXPRESS\MSSQL\Backup";
        public static double FolderWidth = 600;
        public static double ImgWidth = 170;
        public static double ImgHeight = 100;
    }
}
