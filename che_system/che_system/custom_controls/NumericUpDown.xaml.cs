using System.Windows;
using System.Windows.Controls;

namespace che_system.custom_controls
{
    public partial class NumericUpDown : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(NumericUpDown), new PropertyMetadata(99));

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public NumericUpDown()
        {
            InitializeComponent();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as NumericUpDown;
            if (control != null)
            {
                var newValue = (int)e.NewValue;
                var coerced = Math.Max(control.Minimum, Math.Min(control.Maximum, newValue));
                if (coerced != newValue)
                {
                    control.Value = coerced;
                }
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            Value = Math.Min(Maximum, Value + 1);
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            Value = Math.Max(Minimum, Value - 1);
        }

        private void ValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (int.TryParse(textBox.Text, out int newValue))
                {
                    Value = newValue; // Will coerce
                }
                else if (string.IsNullOrEmpty(textBox.Text))
                {
                    Value = Minimum;
                }
                // Else ignore invalid input
            }
        }
    }
}
