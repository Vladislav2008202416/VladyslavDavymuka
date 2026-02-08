using Avalonia.Controls;

namespace MyFirstApp;
// 2 мітки main dev github робити це все через git push
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    // Цей метод спрацює, коли натиснеш кнопку
public void Button_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    // Знаходимо текстовий блок на ім'я "MyText" і змінюємо його
    var textBlock = this.FindControl<TextBlock>("MyText");
    if (textBlock != null)
    {
        textBlock.Text = "Ти натиснув кнопку!";
    }


}
}