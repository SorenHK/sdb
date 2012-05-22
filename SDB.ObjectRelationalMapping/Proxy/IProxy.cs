using System.ComponentModel;

namespace SDB.ObjectRelationalMapping.Proxy
{
    public interface IProxy : INotifyPropertyChanged
    {
        int SDBId { get; set; }
        void OnPropertyChanged(string propertyName);
    }
}
