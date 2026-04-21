using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Data;
/// <summary>
/// @brief Class for a 2D vector.
/// </summary>
public class Vector : INotifyPropertyChanged
{
    // @brief X coordinate of the vector
    private double _x;
    // @brief Y coordinate of the vector
    private double _y;

    // @brief Event for property changed
    public event PropertyChangedEventHandler? PropertyChanged;

    public double X
    {
        get => _x;
        set { _x = value; OnPropertyChanged(); } // @brief Property changed event
    }

    public double Y
    {
        get => _y;
        set { _y = value; OnPropertyChanged(); } // @brief Property changed event
    }

    
    // @brief Constructor for a 2D vector
    public Vector(double x, double y)
    {
        _x = x;
        _y = y;
    }
    
    // @brief Updates the position of the vector
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