using System.Windows.Input;

namespace che_system.view_model
{
    public class View_Model_Command : ICommand
    {
        // Fields
        private readonly Action<object?> _execute_action;
        private readonly Predicate<object?>? _can_execute_action;

        // Constructors
        public View_Model_Command(Action<object?> execute_action)
        {
            _execute_action = execute_action ?? throw new ArgumentNullException(nameof(execute_action));
            _can_execute_action = null;
        }

        public View_Model_Command(Action<object?> execute_action, Predicate<object?> can_execute_action)
        {
            _execute_action = execute_action ?? throw new ArgumentNullException(nameof(execute_action));
            _can_execute_action = can_execute_action ?? throw new ArgumentNullException(nameof(can_execute_action));
        }

        // Events
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Methods
        public bool CanExecute(object? parameter)
        {
            return _can_execute_action == null || _can_execute_action(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute_action(parameter);
        }
    }
}
