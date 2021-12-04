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
            tbName.Text = "Name: " + playList.Name;
            tbFullName.Text = "Full path: " + playList.FullName;
            tbPerformers.Text = "Artists: " + playList.Performers;
            tbAlbum.Text = "Album: " + playList.Album;
            tbReleaseYear.Text = "Release year: " + playList.ReleaseYear;
            tbGenres.Text = "Genres: " + playList.Genres;
            tbCountry.Text = "Country: " + playList.Country;
        }
    }
}
