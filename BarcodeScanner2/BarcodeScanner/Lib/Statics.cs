using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodeScanner.Lib
{
    public class Statics
    {
        public static bool ShouldExit = false;

        public static string Machine1Motor = @"\\localhost\Project01\OPC\PLCS71200\Device1\Machine1Motor";
        public static string Machine2Motor = @"\\localhost\Project01\OPC\PLCS71200\Device1\Machine2Motor";
        public static string Counter1DB = @"\\localhost\Project01\OPC\PLCS71200\Device1\Counter1DB";
        public static string Counter2DB = @"\\localhost\Project01\OPC\PLCS71200\Device1\Counter2DB";
        public static string Counter1Reset = @"\\localhost\Project01\OPC\PLCS71200\Device1\Counter1Reset";
        public static string Counter2Reset = @"\\localhost\Project01\OPC\PLCS71200\Device1\Counter2Reset";
        public static string Watchdog = @"\\localhost\Project01\OPC\PLCS71200\Device1\Watchdog";
    }
}
