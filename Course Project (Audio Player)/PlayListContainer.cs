using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayer.Models
{
    //наследуем класс от List<PlayList>
    public class PlayListContainer: INotifyPropertyChanged
    {
        //коллекция, где хранятся аудиофайлы
        private List<PlayList> _playListData = new List<PlayList>();
        public List<PlayList> PlayListData
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
        public PlayListContainer(List<PlayList> playListData) 
        {
            _playListData = new List<PlayList>(playListData);
            OnPropertyChanged(nameof(PlayListData));
        }

        //создание своих методов, чтобы в них вызывалось событие PropertyChanged
        //функционал аналогичен методам Add, Clear, RemoveAt
        public void AddSong(PlayList playList)
        {
            PlayListData.Add(playList);
            OnPropertyChanged(nameof(PlayListData));
        }
        public void ClearAllSongs()
        {
            PlayListData.Clear();
            OnPropertyChanged(nameof(PlayListData));
        }
        public void RemoveSongAt(int index)
        {
            PlayListData.RemoveAt(index);
            OnPropertyChanged(nameof(PlayListData));
        }
        
        //функция-обработчик события
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
