using System.Reflection;
using System.Windows;
namespace FmStereoModulator;
public partial class InfoWindow : Window
{
    public InfoWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Versione {version?.Major}.{version?.Minor}.{version?.Build}";
    }
}
