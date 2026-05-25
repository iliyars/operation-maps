using System.Text;
using System.Windows;
using System.Windows.Controls;
using OperationMaps.Infrastructure.Persistence;
using Serilog;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OperationMaps.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(OperationMapsDbContext db)
    {
        InitializeComponent();

        // проверка: контейнер дал контекст, БД доступна
        var canConnect = db.Database.CanConnect();
        Log.Information("DB CanConnect = {CanConnect}", canConnect);
        Title = $"OperationMaps — DB connected: {canConnect}";
    }
}
