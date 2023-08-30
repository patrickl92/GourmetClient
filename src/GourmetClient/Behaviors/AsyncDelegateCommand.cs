namespace GourmetClient.Behaviors
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class AsyncDelegateCommand : ICommand
	{
		private readonly Func<Task> _executeMethod;

		private readonly Func<bool> _canExecuteMethod;

		private bool _executing;

		public AsyncDelegateCommand(Func<Task> executeMethod) : this(executeMethod, () => true)
		{
		}

		public AsyncDelegateCommand(Func<Task> executeMethod, Func<bool> canExecuteMethod)
		{
			_executeMethod = executeMethod ?? throw new ArgumentNullException(nameof(executeMethod));
			_canExecuteMethod = canExecuteMethod ?? throw new ArgumentNullException(nameof(canExecuteMethod));
		}

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
		{
			return _canExecuteMethod();
		}

		public async void Execute(object parameter)
		{
            if (_executing || !CanExecute(parameter))
            {
                return;
            }

            try
            {
                _executing = true;
                await _executeMethod();
            }
            finally
            {
                _executing = false;
            }
        }
	}
}
