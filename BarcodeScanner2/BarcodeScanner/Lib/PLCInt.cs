using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.NetworkVariable;

namespace BarcodeScanner.Lib
{
    public class PLCInt
    {
        private NetworkVariableBufferedSubscriber<UInt16> _subscriber;
        private NetworkVariableBufferedWriter<UInt16> _bufferedWriter; 
        private string _location;
        private int _value;


        public NetworkVariableBufferedSubscriber<UInt16> Subscriber
        {
            get { return _subscriber; }
            set { _subscriber = value; }
        }

        public string Location
        {
            get { return _location; }
            set { _location = value; }
        }

        public NetworkVariableBufferedWriter<UInt16> BufferedWriter
        {
            get { return _bufferedWriter; }
            set { _bufferedWriter = value; }
        }

        public UInt16 Value
        {
            get
            {
                Subscriber.Connect();
                _value = Subscriber.ReadData().GetValue();
                return (ushort) _value;
            }
            set
            {
                Subscriber.Connect();
                BufferedWriter.WriteValue(value);
                _value = value;
                Subscriber.Disconnect();
            }
        }

        public PLCInt(string location)
        {
            this.Location = location;
            Subscriber = new NetworkVariableBufferedSubscriber<UInt16>(this.Location);
            BufferedWriter = new NetworkVariableBufferedWriter<UInt16>(this.Location);
        }
    }
}
