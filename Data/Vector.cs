using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Data;

public class Vector : INotifyPropertyChanged
{
    private double _x;
    private double _y;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double X
    {
        get => _x;
        set { _x = value; OnPropertyChanged(); }
    }

    public double Y
    {
        get => _y;
        set { _y = value; OnPropertyChanged(); }
    }

    public Vector(double x, double y)
    {
        _x = x;
        _y = y;
    }
    
    public void Update(double x, double y)
    {
        _x = x;
        _y = y;
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}