using System;
using System.IO;

namespace ProclickEmployeeLib.Models
{
    class RecorderModel
    {
        public static readonly string RecordingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "recordings");

        public static string Filename = RecordingsPath + "\\" + DateTime.Now.ToString("MM_dd_yyyy__HH_mm_ss");
        public static string VideoFilename = Filename + ".avi";
        public static string AudioFilename = Filename + ".wav";

        public static void ResetFilename()
        {            
            Filename = RecordingsPath + "\\" + DateTime.Now.ToString("MM_dd_yyyy__HH_mm_ss");
            VideoFilename = Filename + ".avi";
            AudioFilename = Filename + ".wav";
        }
    }

}
