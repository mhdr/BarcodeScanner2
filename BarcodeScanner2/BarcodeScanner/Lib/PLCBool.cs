using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.NetworkVariable;

namespace BarcodeScanner.Lib
{
    public class PLCBool
    {
        private NetworkVariableBufferedSubscriber<Boolean> _subscriberBool;
        private NetworkVariableBufferedWriter<Boolean> _bufferedWriterBool; 
        private string _location;
        private bool _value;


        public NetworkVariableBufferedSubscriber<bool> SubscriberBool
        {
            get { return _subscriberBool; }
            set { _subscriberBool = value; }
        }

        public string Location
        {
            get { return _location; }
            set { _location = value; }
        }

        public NetworkVariableBufferedWriter<bool> BufferedWriterBool
        {
            get { return _bufferedWriterBool; }
            set { _bufferedWriterBool = value; }
        }

        public bool Value
        {
            set
            {
                BufferedWriterBool.Connect();
                BufferedWriterBool.WriteValue(value);
                _value = value;
            }
        }

        public PLCBool(string location)
        {
            this.Location = location;
            SubscriberBool = new NetworkVariableBufferedSubscriber<bool>(this.Location);
            BufferedWriterBool=new NetworkVariableBufferedWriter<bool>(this.Location);
        }

        

        public void Stop()
        {
            BufferedWriterBool.Connect();
            BufferedWriterBool.WriteValue(false);
        }

        public void Start()
        {
            BufferedWriterBool.Connect();
            BufferedWriterBool.WriteValue(true);
        }
    }
}
