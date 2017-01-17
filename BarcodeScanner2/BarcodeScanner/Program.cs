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

        private static int Delay;
        private static string Machine;
        private static int Interval;

        private static string LastBarcode;
        private static int PreviousCounter;
        private static bool MachineIsRunning;

        static void Main(string[] args)
        {
            readFromIni();
            loadMachine();
            resetPLCCounter();
            runWatchDog();
            startMachineMotor();
            readCounter();

            while (true)
            {
                if (MachineIsRunning)
                {
                    LastBarcode = Console.ReadLine();

                    if (LastBarcode == Template)
                    {
                        //Console.Write($"{DateTime.Now} : ");
                        Console.Write(string.Format("{0} : ",DateTime.Now));
                        Console.WriteLine("OK", Color.Green);
                        Console.WriteLine("-----------------------------------------");
                        writeToCSV(LastBarcode, ReadType.OK);
                    }
                }
                
            }
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
            Machine = data["Default"]["Machine"];
            Delay = Convert.ToInt32(data["Default"]["Delay"]);
            Interval = Convert.ToInt32(data["Default"]["Interval"]);
        }

        private static void loadMachine()
        {
            if (Machine == "1")
            {
                MachineMotor = Statics.Machine1Motor;
                CounterDB = Statics.Counter1DB;
                ResetCounter = Statics.Counter1Reset;
            }
            else if (Machine == "2")
            {
                MachineMotor = Statics.Machine2Motor;
                CounterDB = Statics.Counter2DB;
                ResetCounter = Statics.Counter2Reset;
            }
        }

        private static void readCounter()
        {
            LastBarcode = Template;

            Thread thread = new Thread(() =>
              {
                  while (true)
                  {
                      if (MachineIsRunning)
                      {
                          PLCInt plcInt1 = new PLCInt(CounterDB);
                          int realCounter = plcInt1.Value;

                          if (realCounter != PreviousCounter)
                          {
                              // fire counter changed
                              counterChanged();
                          }

                          if (realCounter > 900)
                          {
                              resetPLCCounter();
                          }

                          PreviousCounter = realCounter;

                          Thread.Sleep(Interval);
                      }

                  }

              });

            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }

        private static void counterChanged()
        {
            ThreadPool.QueueUserWorkItem(obj =>
            {
                Thread.Sleep(Delay);

                if (LastBarcode != Template)
                {
                    stopMachineMotor();

                    if (LastBarcode == "")
                    {
                        //Console.Write($"{DateTime.Now} : ");
                        Console.Write(string.Format("{0} : ", DateTime.Now));
                        Console.WriteLine("Blank", Color.Yellow);
                        Console.WriteLine("-----------------------------------------");
                        writeToCSV(LastBarcode, ReadType.Blank);
                    }
                    else
                    {
                        //Console.Write($"{DateTime.Now} : ");
                        Console.Write(string.Format("{0} : ", DateTime.Now));
                        Console.WriteLine("Mismatch", Color.PaleVioletRed);
                        Console.WriteLine("-----------------------------------------");
                        writeToCSV(LastBarcode, ReadType.Mismatch);
                    }

                    Console.WriteLine("Continue? (Y/N)");
                    var answer = Console.ReadLine();
                    if (answer == "y" || answer == "Y")
                    {
                        startMachineMotor();
                    }
                }

                // at the end empty barcode;

                LastBarcode = "";
            });
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

            Thread.Sleep(10);

            counterReset.Value = false;
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
                    output=string.Format("{0},Blank,{1},{2}",barcode,currentDate,currentTime);
                }
                else if (readType == ReadType.OK)
                {
                    //output = $"{barcode},OK,{currentDate},{currentTime}";
                    output=string.Format("{0},OK,{1},{2}",barcode,currentDate,currentTime);
                }
                else if (readType == ReadType.Mismatch)
                {
                    //output = $"{barcode},Mismatch,{currentDate},{currentTime}";
                    output = string.Format("{0},Mismatch,{1},{2}", barcode, currentDate, currentTime);
                }


                File.AppendAllText(file.FullName, output + Environment.NewLine);
            });

        }

        public static void startMachineMotor()
        {
            PLCBool plcVariable = new PLCBool(MachineMotor);
            plcVariable.Value = false;
            MachineIsRunning = true;
        }

        public static void stopMachineMotor()
        {
            PLCBool plcVariable = new PLCBool(MachineMotor);
            plcVariable.Value = true;
            MachineIsRunning = false;
        }

    }
}
