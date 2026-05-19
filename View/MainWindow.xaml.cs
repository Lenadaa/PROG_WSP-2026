using System.Windows;
using System.Windows.Controls;
using ViewModel;

namespace View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
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
}