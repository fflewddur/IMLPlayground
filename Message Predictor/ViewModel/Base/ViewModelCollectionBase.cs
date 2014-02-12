using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor
{
    // Abstract class to support models implementing INotifyPropertyChanged
    [Serializable]
    public abstract class ViewModelCollectionBase<T1> : ObservableCollection<T1>, INotifyPropertyChanged
    {

        // Returns 'true' if the property value changed
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value))
                return false;

            storage = value;
            PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
            this.OnPropertyChanged(e);
            return true;
        }


    }
}
