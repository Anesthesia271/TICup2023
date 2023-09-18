using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using TICup2023.Model;

namespace TICup2023.ViewModel;

public partial class SerialContentViewModel : ObservableObject
{
    [ObservableProperty] private SerialManager _serialManager = SerialManager.GetInstance();
    
    [RelayCommand]
    private void UpdatePortNameList()
    {
        SerialManager.UpdatePortNameList();
        OnPropertyChanged(nameof(SerialManager));
    }
    
    [RelayCommand]
    private void OpenPort()
    {
        SerialManager.OpenPort();
        OnPropertyChanged(nameof(SerialManager));
    }
    
    [RelayCommand]
    private void ClosePort()
    {
        SerialManager.ClosePort();
        OnPropertyChanged(nameof(SerialManager));
    }
}