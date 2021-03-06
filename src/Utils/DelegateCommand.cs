using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FaceDetection.Utils
{
	public class DelegateCommand : ICommand
	{
		private Action _execute;
		private Func<bool> _canExecute;

		public DelegateCommand(Action execute)
			: this(execute, null) { }

		public DelegateCommand(Action execute, Func<bool> canExecute)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			if (_canExecute == null)
				return true;
			return _canExecute();
		}

		public void Execute(object parameter)
		{
			_execute();
		}

		public event EventHandler CanExecuteChanged;
		public void RaiseExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public class DelegateCommand<T> : ICommand
	{
		//private Func<bool, T> _canExecute;
		private Predicate<T> _canExecute;
		private Action<T> _execute;

		public DelegateCommand(Action<T> execute)
			: this(execute, null)
		{
		}

		public DelegateCommand(Action<T> execute, Predicate<T> canExecute)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			if (_canExecute == null) return true;
			return _canExecute(parameter == null ? default(T) : (T)Convert.ChangeType(parameter, typeof(T)));
		}

		public void Execute(object parameter)
		{
			_execute(parameter == null ? default(T) : (T)Convert.ChangeType(parameter, typeof(T)));
		}

		public event EventHandler CanExecuteChanged;
		public void RaiseExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}

	}
}
