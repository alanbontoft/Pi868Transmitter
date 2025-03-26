using System.IO.Ports;
using System.Diagnostics;
using System.Device.Gpio;


namespace Pi868Transmitter
{
    class Program
    {
        ////////////////////////////////
        /// Main entry point
        ////////////////////////////////
        static async Task Main(string[] args)
        {
            SerialPort? port = null;

            Stopwatch stopwatch = new();

            const int GPIO0 = 17;
            const int GPIO1 = 18;

            byte channel = 6;
            UInt16 address = 0xFFFF;

            try
            {

                var paramsSupplied = args.Length >= 2;


                if (paramsSupplied)
                {
                    channel = byte.Parse(args[0]);
                    address = UInt16.Parse(args[1]);
                }
                
                int freq = 862 + channel;

                // report channel/frequency
                var msg = $"Channel used: {channel} [{freq}MHz]";
                if (!paramsSupplied) msg += " (default)";
                Console.WriteLine(msg);

                // report address
                msg = $"Address used: {address:X4}";
                if (!paramsSupplied) msg += " (default)";
                Console.WriteLine(msg);

                int bytesReady;

                var controller = new GpioController();

                controller.OpenPin(GPIO0, PinMode.Output);
                controller.OpenPin(GPIO1, PinMode.Output);

                Console.WriteLine("Entering Config mode");

                // put into config mode
                controller.Write(GPIO0, PinValue.High);
                controller.Write(GPIO1, PinValue.High);

                var cmdReadParams = new byte[] { 0xC1, 0xC1, 0xC1 };

                var cmdReadVer = new byte[] { 0xC3, 0xC3, 0xC3 };

                var cmdWriteParams = new byte[] { 0xC2, 0x00, 0x00, 0x1A, 0x00, 0x44 };

                // replace channel (frequency) with arg or default
                cmdWriteParams[4] = channel;

                // replace address
                cmdWriteParams[1] = BitConverter.GetBytes(address)[1];
                cmdWriteParams[2] = BitConverter.GetBytes(address)[0];

                var message = string.Empty;

                var rxbuffer = new byte[50];
                var portName = "/dev/serial0";

                port = new SerialPort()
                {
                    PortName = portName,
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One
                };

                port.Open();

                // read version
                port.Write(cmdReadVer, 0, cmdReadVer.Length);

                await Task.Delay(100);

                bytesReady = port.BytesToRead;

                if (bytesReady > 0)
                {
                    // limit chars read to buffer size
                    var bytes = (bytesReady > rxbuffer.Length) ? rxbuffer.Length : bytesReady;
                    
                    port.Read(rxbuffer, 0, bytes);
                    
                    Console.WriteLine($"Version: {formatData(rxbuffer, bytes)}");

                }

                // read params
                port.Write(cmdReadParams, 0, cmdReadParams.Length);

                await Task.Delay(100);

                bytesReady = port.BytesToRead;

                if (bytesReady > 0)
                {
                    // limit chars read to buffer size
                    var bytes = (bytesReady > rxbuffer.Length) ? rxbuffer.Length : bytesReady;
                    
                    port.Read(rxbuffer, 0, bytes);

                    Console.WriteLine($"Params: {formatData(rxbuffer, bytes)}");
                }

                // write params
                port.Write(cmdWriteParams, 0, cmdWriteParams.Length);

                await Task.Delay(100);

                bytesReady = port.BytesToRead;

                if (bytesReady > 0)
                {
                    // limit chars read to buffer size
                    var bytes = (bytesReady > rxbuffer.Length) ? rxbuffer.Length : bytesReady;
                    
                    port.Read(rxbuffer, 0, bytes);
                }

                Console.WriteLine("Entering Normal mode");

                // put into normal mode
                controller.Write(GPIO0, PinValue.Low);
                controller.Write(GPIO1, PinValue.Low);
                await Task.Delay(100);

                Console.WriteLine("Transmit started...");
 
                while (true)
                {

                    // bytesReady = port.BytesToRead;

                    // if (bytesReady > 0)
                    // {
                    //     // limit chars read to buffer size
                    //     var bytes = (bytesReady > rxbuffer.Length) ? rxbuffer.Length : bytesReady;
                        
                    //     port.Read(rxbuffer, 0, bytes);
                        
                    //     Console.WriteLine($"Data received: {formatData(rxbuffer, bytes)}");

                    // }

                    msg = DateTime.Now.ToString();
                    Console.WriteLine(msg);
                    port.Write(msg);

                    await Task.Delay(1000);

                }


 


                // await Task.Run(() => 
                // {
                //     for (int i=0; i < 10; i++)
                //         Console.WriteLine($"{i}:Hello 64");
                // });


            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // serialPort?.Close();

        }

        static string formatData(byte[] data, int count)
        {
            var s = string.Empty;
            for (int i=0; i < count; i++)
            {
                s += $"{data[i]:X2} ";
            }

            return s;
        }
    }
}
