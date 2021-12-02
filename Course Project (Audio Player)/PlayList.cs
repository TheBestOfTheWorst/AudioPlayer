//библиотека, описывающая базовый функционал компонентов (для привязки)
using System.ComponentModel;

namespace AudioPlayer.Models
{
    //данный класс описывает аудиофайл
    public class PlayList : INotifyPropertyChanged
    {
        //поля
        private string _icon;
        private string _name;
        private string _fullName;
        private string _performers;
        private string _album;
        private string _releaseYear;
        private string _genres;
        private string _country;

        //стандартное событие, которое уведомляет о изменении свойств компонента
        public event PropertyChangedEventHandler PropertyChanged;

        public PlayList() { }
        public PlayList(string icon, string name, string fullname, string performers, string album, string releaseYear, string genres, string country)
        {
            Icon = icon;
            Name = name;
            FullName = fullname;
            Performers = performers;
            Album = album;
            ReleaseYear = releaseYear;
            Genres = genres;
            Country = country;
        }

        //свойства
        public string Icon
        {
            get { return _icon; }
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    OnPropertyChanged(nameof(Icon));
                }
            }
        }
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        public string FullName
        {
            get { return _fullName; }
            set
            {
                if (_fullName != value)
                {
                    _fullName = value;
                    OnPropertyChanged(nameof(FullName));
                }
            }
        }
        public string Performers
        {
            get { return _performers; }
            set
            {
                if (_performers != value)
                {
                    _performers = value;
                    OnPropertyChanged(nameof(Performers));
                }
            }
        }
        public string Album
        {
            get { return _album; }
            set
            {
                if (_album != value)
                {
                    _album = value;
                    OnPropertyChanged(nameof(Album));
                }
            }
        }
        public string ReleaseYear
        {
            get { return _releaseYear; }
            set
            {
                if (_releaseYear != value)
                {
                    _releaseYear = value;
                    OnPropertyChanged(nameof(ReleaseYear));
                }
            }
        }
        public string Genres
        {
            get { return _genres; }
            set
            {
                if (_genres != value)
                {
                    _genres = value;
                    OnPropertyChanged(nameof(Genres));
                }
            }
        }
        public string Country
        {
            get { return _country; }
            set
            {
                if (_country != value)
                {
                    _country = value;
                    OnPropertyChanged(nameof(Country));
                }
            }
        }

        //метод-обработчик события 
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
