using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using TICup2023.Tool.Helper;

namespace TICup2023.Model;

public delegate void DataReceived(string msg);

public delegate void DataSent(string msg);

public class SerialManager
{
    private static readonly SerialManager Instance = new();
    public static SerialManager GetInstance() => Instance;

    public string[] PortNameList { get; private set; } = SerialPort.GetPortNames();
    public List<Parity> ParityList { get; } = EnumHelper<Parity>.ToList();
    public List<StopBits> StopBitsList { get; } = EnumHelper<StopBits>.ToList();

    public SerialPort SerialPort { get; } = new(" ", 57600, Parity.Odd, 8, StopBits.One)
    {
        WriteTimeout = 1000,
        ReadTimeout = 1000
    };

    public DataReceived? DataReceived { get; set; }
    public DataSent? DataSent { get; set; }

    private SerialManager()
    {
        SerialPort.DataReceived += (sender, _) =>
        {
            var sp = (SerialPort)sender;
            var indata = sp.ReadExisting();
            DataReceived?.Invoke(indata);
        };
    }

    public void UpdatePortNameList()
    {
        PortNameList = SerialPort.GetPortNames();
    }

    public void OpenPort()
    {
        SerialPort.Open();
    }
    
    public async void SendMsgAsync(string msg)
    {
        await Task.Run(() => { SerialPort.Write(msg);});
        DataSent?.Invoke(msg);
    }

    public void ClosePort()
    {
        SerialPort.Close();
    }
}