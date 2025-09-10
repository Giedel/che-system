using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace system_project.custom_controls
{
    /// <summary>
    /// Interaction logic for Bindable_Password_Box.xaml
    /// </summary>
    public partial class Bindable_Password_Box : UserControl
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(SecureString), typeof(Bindable_Password_Box));

        public SecureString Password
        {
            get { return (SecureString)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public Bindable_Password_Box()
        {
            InitializeComponent();
            password_text.PasswordChanged += on_password_changed;
        }

        private void on_password_changed(object sender, RoutedEventArgs e)
        {
            Password = password_text.SecurePassword;
        }
    }
}
