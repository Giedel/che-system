using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace che_system.custom_controls
{
    public partial class Card_1_Clickable : UserControl
    {
        public Card_1_Clickable()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(Card_1_Clickable));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(Card_1_Clickable));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        private void ExecuteCommand()
        {
            if (Command != null && Command.CanExecute(CommandParameter))
                Command.Execute(CommandParameter);
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ExecuteCommand();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter or Key.Space)
            {
                ExecuteCommand();
                e.Handled = true;
            }
        }
    }
}