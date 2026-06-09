using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Data;
using ViewModel;

namespace View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private double _offsetX;
    private double _offsetY;
    
    public MainWindow()
    {
        InitializeComponent();
        ToolTipService.SetInitialShowDelay(this, 100);
        ToolTipService.SetShowDuration(this, 10000);
    }
    
    private void SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Width = e.NewSize.Width;
            viewModel.Height = e.NewSize.Height;
        }
    }
    
    private void Ball_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not FrameworkElement fe) return;
        if (fe.DataContext is not IBall ball) return;
        
        var pos    = e.GetPosition(BallsControl);
        _offsetX   = pos.X - ball.Position.X;
        _offsetY   = pos.Y - ball.Position.Y;

        if (DataContext is MainViewModel vm)
            vm.StartDrag(ball);
        
        Mouse.Capture(BallsControl);
        e.Handled = true;
    }
    
    private void BallsControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (DataContext is not MainViewModel vm) return;

        var pos = e.GetPosition(BallsControl);
        vm.MoveDrag(pos.X - _offsetX, pos.Y - _offsetY);
    }
    
    private void BallsControl_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.EndDrag();

        Mouse.Capture(null);
    }
}