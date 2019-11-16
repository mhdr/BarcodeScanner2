using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BarcodeScanner.Lib;
using IniParser;
using IniParser.Model;
using Console = Colorful.Console;

namespace BarcodeScanner
{
    class Program
    {
        private static string Template;
        private static string MachineMotor;
        private static string CounterDB;
        private static string ResetCounter;

        private static int Counter;
        private static int CounterPrev;
        private static int CounterDelay;

        private static int Interval;

        private static int ResetDelay = -1;

        private static bool MachineIsRunning;


        private static string barcodeValue;
        private static int barcodeCounter = 0;

        static void ReadBarcode()
        {
            Thread thread = new Thread(() =>
            {

                while (MachineIsRunning)
                {
                    barcodeValue = Console.ReadLine();
                    barcodeCounter++;
                    DoWork();
                }

            });

            thread.Priority = ThreadPriority.AboveNormal;
            thread.Start();
        }

        static void DoWork()
        {
            if (MachineIsRunning)
            {
                Thread.Sleep(Interval);

                PLCInt plcInt1 = new PLCInt(CounterDB);
                int plcCounter = plcInt1.Value;

                if (barcodeCounter == plcCounter)
                {
                    if (barcodeValue == Template)
                    {
                        //Console.Write($"{DateTime.Now} : ");
                        Console.Write(string.Format("{0} : ", DateTime.Now));
                        Console.WriteLine("OK", Color.Green);
                        Console.WriteLine("-----------------------------------------");
                        writeToCSV(barcodeValue, ReadType.OK);
                    }
                    else
                    {
                        stop();
                        Console.Write(string.Format("{0} : ", DateTime.Now));
                        Console.WriteLine("Mismatch", Color.PaleVioletRed);
                        Console.WriteLine("-----------------------------------------");
                        writeToCSV(barcodeValue, ReadType.Mismatch);
                    }
                }
                else
                {
                    if (plcCounter > barcodeCounter)
                    {
                        stop();
                        Console.Write(string.Format("{0} : ", DateTime.Now));
                        Console.WriteLine("Blank", Color.Yellow);
                        Console.WriteLine("-----------------------------------------");
                        writeToCSV(barcodeValue, ReadType.Blank);
                    }
                    else if (plcCounter < barcodeCounter)
                    {
                        stop();
                        Console.Write(string.Format("{0} : ", DateTime.Now));
                        Console.WriteLine("Counter Error", Color.Purple);
                        Console.WriteLine("-----------------------------------------");
                        //writeToCSV(LastBarcode, ReadType.Blank);
                    }
                }

                //#if DEBUG

                //                Console.WriteLine("Barcode : " + barcodeCounter);
                //                Console.WriteLine("Counter : " + plcCounter);

                //#endif

                if (!MachineIsRunning)
                {
                    if (ResetDelay == -1)
                    {
                        Console.WriteLine("Continue? (Y/N)");
                        var answer = Console.ReadLine();
                        if (answer == "y" || answer == "Y")
                        {
                            start();
                        }
                    }
                    else
                    {
                        string msg = string.Format("Auto start in {0}ms", ResetDelay);
                        Console.WriteLine(msg);
                        Console.WriteLine();
                        Thread.Sleep(ResetDelay);
                        start();
                    }

                }
            }
        }

        static void Main(string[] args)
        {
            readFromIni();
            loadMachine();
            resetPLCCounter();
            runWatchDog();
            start();
        }


        private static void readFromIni()
        {
            var current = Directory.GetCurrentDirectory();
            var defaultDir = Path.Combine(current, "Data");
            var d = new DirectoryInfo(defaultDir);
            var files = d.GetFiles("*.ini");
            var file = files[0];

            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(file.FullName);

            Template = data["Default"]["Template"];
            // Machine = data["Default"]["Machine"];
            Interval = Convert.ToInt32(data["Default"]["Interval"]);
            ResetDelay = Convert.ToInt32(data["Default"]["ResetDelay"]);
            CounterDelay = Convert.ToInt32(data["Default"]["CounterDelay"]);
        }

        private static void loadMachine()
        {
            MachineMotor = Statics.Machine1Motor;
            CounterDB = Statics.Counter1DB;
            ResetCounter = Statics.Counter1Reset;
        }

        private static void runWatchDog()
        {
            Thread thread = new Thread(() =>
              {
                  while (true)
                  {
                      PLCBool plcBool = new PLCBool(Statics.Watchdog);
                      plcBool.Start();
                      Thread.Sleep(10);
                      plcBool.Stop();
                      Thread.Sleep(500);
                  }
              });

            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }

        private static void resetPLCCounter()
        {
            PLCBool counterReset = new PLCBool(ResetCounter);
            counterReset.Value = true;

            Thread.Sleep(200);

            counterReset.Value = false;

            Thread.Sleep(200);

            PLCInt plcInt1 = new PLCInt(CounterDB);
            int plcCounter = plcInt1.Value;
            barcodeCounter = plcCounter;
        }

        private static void writeToCSV(string barcode, ReadType readType)
        {
            ThreadPool.QueueUserWorkItem(obj =>
            {
                var current = Directory.GetCurrentDirectory();
                var defaultDir = Path.Combine(current, "Data");
                var d = new DirectoryInfo(defaultDir);
                var files = d.GetFiles("*.csv");
                var file = files[0];

                var output = "";
                var currentDate = DateTime.Now.ToShortDateString();
                var currentTime = DateTime.Now.ToLongTimeString();

                if (readType == ReadType.Blank)
                {
                    //output = $"{barcode},Blank,{currentDate},{currentTime}";
                    output = string.Format("{0},Blank,{1},{2}", barcode, currentDate, currentTime);
                }
                else if (readType == ReadType.OK)
                {
                    //output = $"{barcode},OK,{currentDate},{currentTime}";
                    output = string.Format("{0},OK,{1},{2}", barcode, currentDate, currentTime);
                }
                else if (readType == ReadType.Mismatch)
                {
                    //output = $"{barcode},Mismatch,{currentDate},{currentTime}";
                    output = string.Format("{0},Mismatch,{1},{2}", barcode, currentDate, currentTime);
                }


                File.AppendAllText(file.FullName, output + Environment.NewLine);
            });

        }

        public static void start()
        {
            startMotor();
            MachineIsRunning = true;
            ReadBarcode();
        }

        public static void stop()
        {
            stopMotor();
            MachineIsRunning = false;
            resetPLCCounter();
        }

        public static void startMotor()
        {
            PLCBool plcVariable = new PLCBool(MachineMotor);
            plcVariable.Value = false;
        }

        public static void stopMotor()
        {
            PLCBool plcVariable = new PLCBool(MachineMotor);
            plcVariable.Value = true;
        }

    }
}
