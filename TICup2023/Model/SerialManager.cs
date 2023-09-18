using System.Collections.Generic;
using System.IO.Ports;
using TICup2023.Tool.Helper;

namespace TICup2023.Model;

public class SerialManager
{
    private static readonly SerialManager Instance = new();
    public static SerialManager GetInstance() => Instance;

    public string[] PortNameList { get; private set; } = SerialPort.GetPortNames();
    public List<Parity> ParityList { get; set; } = EnumHelper<Parity>.ToList();
    public List<StopBits> StopBitsList { get; set; } = EnumHelper<StopBits>.ToList();
    public string PortName { get; set; } = string.Empty;
    public int BaudRate { get; set; } = 57600;
    public int DataBits { get; set; } = 8;
    public Parity Parity { get; set; } = Parity.Odd;
    public StopBits StopBits { get; set; } = StopBits.One;
    public SerialPort SerialPort { get; private set; } = new();
    
    public void UpdatePortNameList()
    {
        PortNameList = SerialPort.GetPortNames();
    }
    
    public void OpenPort()
    {
        SerialPort = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
        SerialPort.Open();
    }
    
    public void ClosePort()
    {
        SerialPort.Close();
    }
}