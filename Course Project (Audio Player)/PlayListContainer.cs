using System.ComponentModel;
using System.Collections.ObjectModel;

namespace AudioPlayer.Models
{
    public class PlayListContainer : INotifyPropertyChanged
    {
        //коллекция, где хранятся аудиофайлы
        private ObservableCollection<PlayList> _playListData = new ObservableCollection<PlayList>();
        public ObservableCollection<PlayList> PlayListData
        {
            get { return _playListData; }
            set
            {
                _playListData = value;
                OnPropertyChanged(nameof(PlayListData));
            }
        }
        //аналогичное событие, которое уведомляет о изменении объектов коллекции
        public event PropertyChangedEventHandler PropertyChanged;

        public PlayListContainer() { }
        public PlayListContainer(ObservableCollection<PlayList> playListData) => _playListData = playListData;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
