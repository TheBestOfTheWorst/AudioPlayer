using System.Windows;
using AudioPlayer.Models;

namespace AudioPlayer.MoreInfo
{
    //класс описывает окно, которое показывает детальную информацию о треке
    public partial class MoreInfoWindow : Window
    {
        public MoreInfoWindow(PlayList playList)
        {
            InitializeComponent();

            //заполняем все текстовые поля
            lbName.Content = "Name: " + playList.Name;
            tbFullName.Text = "Full path: " + playList.FullName;
            lbPerformers.Content = "Artists: " + playList.Performers;
            lbAlbum.Content = "Album: " + playList.Album;
            lbReleaseYear.Content = "Release year: " + playList.ReleaseYear;
            lbGenres.Content = "Genres: " + playList.Genres;
            lbCountry.Content = "Country: " + playList.Country;
        }
    }
}
