using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;
using Console = Colorful.Console;

namespace BarcodeScanner
{
    class Program
    {
        private static string Template;

        static void Main(string[] args)
        {
            readFromIni();

            while (true)
            {
                var barcode = Console.ReadLine();

                if (barcode == Template)
                {
                    Console.Write($"{DateTime.Now} : ");
                    Console.WriteLine("OK", Color.GreenYellow);
                    Console.WriteLine("-----------------------------------------");
                    writeToCSV(barcode,ReadType.OK);
                }
                else
                {
                    Console.Write($"{DateTime.Now} : ");
                    Console.WriteLine("Mismatch", Color.PaleVioletRed);
                    Console.WriteLine("-----------------------------------------");
                    writeToCSV(barcode,ReadType.Mismatch);
                }
            }
        }


        private static void readFromIni()
        {
            var current = Directory.GetCurrentDirectory();
            var defaultDir = Path.Combine(current, "Default");
            var d = new DirectoryInfo(defaultDir);
            var files = d.GetFiles("*.ini");
            var file = files[0];

            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(file.FullName);

            var template = data["Default"]["Template"];

            Template = template;
        }

        private static void writeToCSV(string barcode, ReadType readType)
        {
            var current = Directory.GetCurrentDirectory();
            var defaultDir = Path.Combine(current, "Default");
            var d = new DirectoryInfo(defaultDir);
            var files = d.GetFiles("*.csv");
            var file = files[0];

            var output = "";
            var currentDate = DateTime.Now.ToShortDateString();
            var currentTime = DateTime.Now.ToLongTimeString();

            if (readType == ReadType.Blank)
            {
                output = $"{barcode},Blank,{currentDate},{currentTime}";
            }
            else if (readType == ReadType.OK)
            {
                output = $"{barcode},OK,{currentDate},{currentTime}";
            }
            else if (readType == ReadType.Mismatch)
            {
                output = $"{barcode},Mismatch,{currentDate},{currentTime}";
            }


            File.AppendAllText(file.FullName, output + Environment.NewLine);
        }
    }
}
