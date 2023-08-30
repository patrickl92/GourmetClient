// <copyright file="AsyncDelegateCommand.cs" company="Trumpf Maschinen Austria">Trumpf 2018. All rights reserved.</copyright>
// <summary>
//    $HeadURL: https://srv07svn2.corp.trumpf.com/svn/SCM/MMI/trunk/src/Common/DotNetExtensions/DotNetExtensions/Commands/AsyncDelegateCommand%7BT%7D.cs $
//    $Author: langpa $
//    $Date: 2018-11-13 16:47:02 +0100 (Di., 13 Nov 2018) $
//    $Revision: 45109 $
// </summary>

namespace GourmetClient.Behaviors
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;

	public class AsyncDelegateCommand<T> : ICommand
	{
		private readonly Func<T, Task> _executeMethod;

		private readonly Func<T, bool> _canExecuteMethod;

		private bool _executing;

		public AsyncDelegateCommand(Func<T, Task> executeMethod) : this(executeMethod, _ => true)
		{
		}

		public AsyncDelegateCommand(Func<T, Task> executeMethod, Func<T, bool> canExecuteMethod)
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
			return _canExecuteMethod((T)parameter);
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
                await _executeMethod((T)parameter);
            }
            finally
            {
                _executing = false;
            }
        }
	}
}
