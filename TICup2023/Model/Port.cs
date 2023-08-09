using System;

namespace TICup2023.Model;

public class Port
{
    /// <summary>
    /// 利用端口名来构造一个端口的实例
    /// </summary>
    /// <param name="name">要实例化的端口名</param>
    public Port(string name)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 打开端口
    /// </summary>
    public void Open()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 关闭端口
    /// </summary>
    public void Close()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 向当前端口中写入一行数据，自动为数据添加换行符
    /// </summary>
    /// <param name="data">要写入的数据</param>
    /// <exception cref="Exception">写入失败时抛出异常</exception>
    public void WriteLine(string data)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 获取到从实例化端口或上一次调用Clear()方法之后，端口接收到的所有数据
    /// </summary>
    public void Read()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 清除端口目前获取到的数据
    /// </summary>
    public void Clear()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 获取当前所有可用端口的名称
    /// </summary>
    /// <returns>当前所有可用端口的名称</returns>
    public static string[] GetPortNames()
    {
        throw new NotImplementedException();
    }
}