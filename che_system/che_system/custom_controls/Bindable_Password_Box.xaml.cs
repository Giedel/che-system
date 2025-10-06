using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace che_system.custom_controls
{
    /// <summary>
    /// Interaction logic for Bindable_Password_Box.xaml
    /// </summary>
    public partial class Bindable_Password_Box : UserControl
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(string), typeof(Bindable_Password_Box));

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public Bindable_Password_Box()
        {
            InitializeComponent();
            password_text.PasswordChanged += on_password_changed;
        }

        private void on_password_changed(object sender, RoutedEventArgs e)
        {
            Password = password_text.Password;
        }
    }
}
