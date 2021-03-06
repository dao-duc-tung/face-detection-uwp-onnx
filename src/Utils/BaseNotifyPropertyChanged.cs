using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FaceDetection.Utils
{
    public class BaseNotifyPropertyChanged
    {
		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (object.Equals(storage, value)) return false;
			storage = value;
			this.OnPropertyChanged(propertyName);
			return true;
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = this.PropertyChanged;
			if (eventHandler != null)
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
