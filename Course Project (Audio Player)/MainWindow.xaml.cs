using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AudioPlayer.Models;
using AudioPlayer.MoreInfo;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Linq;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
        #region поля
        //общее и текущее время трека
        private TimeSpan _totalTimer, _progressTimer;
        //таймер для подсчета времени
        private DispatcherTimer _timer;
        //контейнер для всех загруженных треков
        private PlayListContainer _playListContainer;
        //путь к картинке кнопки играть (класс Uri для удобства)
        private Uri _playUri = new Uri(@"Icons\Play.png", UriKind.Relative);
        //путь к картинке на кнопки пауза
        private Uri _pauseUri = new Uri(@"Icons\Pause.png", UriKind.Relative);
        //текущий индекс листбокса
        private int _currentSelectedIndex = 0;
        //на паузе ли текущий трек
        private bool _isPaused = false;
        //путь к располоению хранения текущего трека
        private string _currentlyPlayedFileName = "";
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            //подтягиваем контейнер с ресурсов окна
            _playListContainer = TryFindResource("playListContainer") as PlayListContainer;
        }

        #region вспомогательные методы обработки
        //метод для обработки истечения тика таймера
        private void _timer_Tick(object sender, EventArgs e)
        {
            //запонимаем сколько времени прошло от начала трека
            _progressTimer = mediaElementMain.Position;
            //если трек еще не закончился, обновляем UI
            if (_progressTimer.TotalSeconds <= _totalTimer.TotalSeconds)
            {
                //если все хорошо, обновляем слайдер
                sliderDuration.Value = _progressTimer.TotalSeconds;
                textBlockProgress.Text = string.Format("{0:hh\\:mm\\:ss}", _progressTimer);
            }
        }
        //асинхронный метод, который ждет, пока у этого объекта будет медиа TimeSpan
        private Task<bool> DetectTimespan()
        {
            bool hasTimespan = false;
            while (true)
            {
                if (mediaElementMain.NaturalDuration.HasTimeSpan)
                {
                    hasTimespan = true;
                    break;
                }
            }
            //возвращаем результат при успешном завершении
            return Task.FromResult(hasTimespan);
        }
        //метод для воспроизведения трека (async показывает, что используем await)
        private async void PlayMedia(string fileName)
        {
            try
            {
                //если трек не на паузе и путь верный, записываем в поля инфо о нем
                if (!_isPaused && fileName != "")
                {
                    _currentlyPlayedFileName = fileName;
                    mediaElementMain.Source = new Uri(fileName, UriKind.Absolute);
                    sliderDuration.Value = 0;
                }
                //активируем сладер если он выключен
                if (!sliderDuration.IsEnabled)
                    sliderDuration.IsEnabled = true;

                //включаем трек
                mediaElementMain.Play();

                //когда у объекта будет Timespan
                if (await DetectTimespan())
                {
                    //запускаем таймер
                    _timer.Start();

                    //сохраняем длительность трека
                    _totalTimer = mediaElementMain.NaturalDuration.TimeSpan;
                    sliderDuration.Maximum = _totalTimer.TotalSeconds;
                    //обновляем текст
                    textBlockDuration.Text = string.Format("{0:hh\\:mm\\:ss}", mediaElementMain.NaturalDuration.TimeSpan);
                    textBlockMediaStatus.Text = $"Playing from {_currentlyPlayedFileName}";
                }
            }
            //отлавливаем все ошибки
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
        //метод для паузы трека
        private void PauseMedia()
        {
            //сначала проверим, можно ли трек ставить на паузу
            if (mediaElementMain.CanPause)
            {
                try
                {
                    //ставим на паузу
                    mediaElementMain.Pause();
                    //на всякий случай проверим наличие HasTimeSpan у медиа еще раз
                    if (mediaElementMain.NaturalDuration.HasTimeSpan)
                    {
                        _timer.IsEnabled = false;
                        _timer.Stop();
                    }
                    textBlockMediaStatus.Text = $"Media Paused";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }
        //метод для остановки трека
        private async void StopMedia()
        {
            try
            {
                //останавливаем трек
                mediaElementMain.Stop();
                _currentlyPlayedFileName = "";

                if (await DetectTimespan())
                {
                    _timer.IsEnabled = false;
                    _timer.Stop();
                }
                //очищаем все UI элементы
                sliderDuration.IsEnabled = false;
                sliderDuration.Value = 0;
                mediaElementMain.Position = TimeSpan.FromSeconds(0);
                textBlockProgress.Text = "00:00:00";
                textBlockMediaStatus.Text = $"Media Stopped";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
        //метод выдает имя предыдущего файла
        private string GetPrevMediaFileName()
        {
            if (_currentSelectedIndex - 1 >= 0)
                _currentSelectedIndex--;
            else
                //если предыдущего файла нет, то играем последний
                _currentSelectedIndex = _playListContainer.PlayListData.Count - 1;
            
            return _playListContainer.PlayListData[_currentSelectedIndex].FullName;
        }
        //метод выдает имя следующего файла
        private string GetNextMediaFileName(bool next = false)
        {
            //если следующего файла нет, вернем первый
            //next нужен для останова, если просто играем текущий трек
            if (next)
            {
                if (_currentSelectedIndex + 1 < _playListContainer.PlayListData.Count)
                    _currentSelectedIndex++;
                else
                    _currentSelectedIndex = 0;
            }

            return _playListContainer.PlayListData[_currentSelectedIndex].FullName;
        }
        #endregion

        #region методы обработки событий
        //метод вызывается по загрузке окна
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //создаем таймер на заднем фоне
            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            //подписываем событие таймера на нашу функцию
            _timer.Tick += _timer_Tick;
        }
        //обработка слайдера изменения громкости
        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaElementMain.Volume = sliderVolume.Value;
        }
        //обработка события окончания трека
        private void mediaElementMain_MediaEnded(object sender, RoutedEventArgs e)
        {
            //останавливаем таймер
            _timer.Stop();
            //если в контейнере что-то есть, включаем следущий трек
            if (_playListContainer.PlayListData.Count > 0)
            {
                PlayMedia(GetNextMediaFileName(true));
                //переключаем выделенный элемент в листбоксе
                if (listBoxPlaylist.SelectedIndex + 1 < listBoxPlaylist.Items.Count)
                    listBoxPlaylist.SelectedIndex++;
                else
                    listBoxPlaylist.SelectedIndex = 0;
            }
            else
                StopMedia();
        }
        //обработка выбора трека в листбоксе
        private void listBoxPlaylist_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //если элементов нет, просто выходим
            if (listBoxPlaylist.Items.Count == 0)
                return;

            //иначе запоминаем номер выбранного трека
            _currentSelectedIndex = listBoxPlaylist.SelectedIndex > -1? listBoxPlaylist.SelectedIndex : 0;
            //запускаем его
            PlayMedia(GetNextMediaFileName());
            _isPaused = false;
            //пробуем подгрузить картинку
            try
            {
                BitmapImage image = new BitmapImage(_pauseUri);
                imagePlayPause.Source = image;
            }
            catch { buttonPlayPause.Content = "Pause"; }
            buttonPlayPause.ToolTip = "Pause (CTRL+P)";
        }
        //обработка перетаскивания указателя слайдера
        private void sliderDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //сначала проверим, верный ли путь к файлу
            if (mediaElementMain.Source != null)
            {
                //аналогично
                if (mediaElementMain.NaturalDuration.HasTimeSpan)
                {
                    _progressTimer = TimeSpan.FromSeconds(sliderDuration.Value);
                    mediaElementMain.Position = _progressTimer;
                }
            }
        }
        #endregion

        #region методы обработки кнопок
        //CanExecute - проверка на возможность выполнения действия
        //Execute - соответственно само его выполнение

        //загружать файлы можно всегда
        private void cmdLoad_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        //метод обработки загрузки нового файла
        private void cmdLoad_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                //открываем диалоговое окно (ищем только *.mp3 файлы)
                OpenFileDialog fileDlg = new OpenFileDialog()
                {
                    FileName = "",
                    Filter = "Audio Files (*.mp3)|*.mp3",
                    Title = "Choose Media",
                    Multiselect = true,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    ReadOnlyChecked = true
                };
                //если нажали открыть в диалоге
                if (fileDlg.ShowDialog().Value)
                {
                    //загружаем выбранные файлы
                    foreach (string file in fileDlg.FileNames)
                    {
                        FileInfo fi = new FileInfo(file);
                        //используем библиотеку Taglib для ID3 v1 и v2 тегов
                        var tfile = TagLib.File.Create(file);

                        PlayList newList = new PlayList()
                        {
                            //иконка всегда одинаковая
                            Icon = @"Icons\Music.ico",
                            //расположение файла
                            FullName = fi.FullName,
                            //название трека
                            Name = tfile.Tag.Title == null || tfile.Tag.Title.Length == 0? "No title": tfile.Tag.Title,
                            //информация о исполнителях
                            Performers = string.Join(", ", tfile.Tag.Performers),
                            //информация о альбоме
                            Album = tfile.Tag.Album == null || tfile.Tag.Album.Length == 0 ? "No album" : tfile.Tag.Album,
                            //информация о годе выпуска
                            ReleaseYear = tfile.Tag.Year.ToString() == "0" ? "No year" : tfile.Tag.Year.ToString(),
                            //информация о жанрах
                            Genres = string.Join(", ", tfile.Tag.Genres),
                            //информация о стране выпуска
                            Country = tfile.Tag.MusicBrainzReleaseCountry == null || tfile.Tag.MusicBrainzReleaseCountry.Length == 0 ? "No country" : tfile.Tag.Title
                        };

                        if (newList.Performers == "")
                            newList.Performers = "No artists";
                        if (newList.Genres == "")
                            newList.Genres = "No genres";

                        _playListContainer.PlayListData.Add(newList);
                    }
                }
                //меняем выбранный элемент если сейчас трек не играет
                if (_currentlyPlayedFileName.Length == 0)
                {
                    listBoxPlaylist.SelectedIndex = _playListContainer.PlayListData.Count - 1;
                    _currentSelectedIndex = _playListContainer.PlayListData.Count - 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //нажимать пауза и продолжить можно только если есть хоть один трек в контейнере
        private void cmdPlayPause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        //метод обработки паузы и продолжить
        private void cmdPlayPause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //удобно проверять состояние кнопки по подсказке
            if (buttonPlayPause.ToolTip.ToString() == "Play (CTRL+P)")
            {
                //если трек на паузе, включаем его (инфо так же)
                if (_isPaused)
                    PlayMedia("");
                //если трек просто не играет, то просто включаем его
                else
                    PlayMedia(GetNextMediaFileName());

                _isPaused = false;

                try
                {
                    imagePlayPause.Source = new BitmapImage(_pauseUri);
                }
                catch { buttonPlayPause.Content = "Pause"; }
                buttonPlayPause.ToolTip = "Pause (CTRL+P)";
            }
            else if (buttonPlayPause.ToolTip.ToString() == "Pause (CTRL+P)")
            {
                //иначе ставим трек на паузу
                _isPaused = true;
                PauseMedia();

                try
                {
                    imagePlayPause.Source = new BitmapImage(_playUri);
                }
                catch { buttonPlayPause.Content = "Play"; }
                buttonPlayPause.ToolTip = "Play (CTRL+P)";
            }
        }
        //нажимать предыдущий трек можно только если есть хоть один трек в контейнере
        private void cmdPrevious_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        //метод обработки предыдущий трек
        private void cmdPrevious_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //трек запускается сразу
            PlayMedia(GetPrevMediaFileName());
            _isPaused = false;

            //изменяем выбранный элемент в листбоксе
            if (listBoxPlaylist.SelectedIndex - 1 >= 0)
                listBoxPlaylist.SelectedIndex--;
            else
                listBoxPlaylist.SelectedIndex = _playListContainer.PlayListData.Count - 1;

            try
            {
                imagePlayPause.Source = new BitmapImage(_pauseUri);
            }
            catch { buttonPlayPause.Content = "Pause"; }
            buttonPlayPause.ToolTip = "Pause (CTRL+P)";
        }
        //аналогично
        private void cmdNext_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        //аналогично
        private void cmdNext_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PlayMedia(GetNextMediaFileName(true));

            //изменяем выбранный элемент в листбоксе
            if (listBoxPlaylist.SelectedIndex + 1 < _playListContainer.PlayListData.Count)
                listBoxPlaylist.SelectedIndex++;
            else
                listBoxPlaylist.SelectedIndex = 0;

            _isPaused = false;

            try
            {
                imagePlayPause.Source = new BitmapImage(_pauseUri);
            }
            catch { buttonPlayPause.Content = "Pause"; }
            buttonPlayPause.ToolTip = "Pause (CTRL+P)";
        }
        //для останова трек должен быть выбран, и он должен играть
        private void cmdStop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0 &&
                (_currentlyPlayedFileName.Length > 0 || _isPaused);
        }
        //метод обработки кнопки стоп
        private void cmdStop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StopMedia();
            //изменяем картинку на продолжить
            try
            {
                imagePlayPause.Source = new BitmapImage(_playUri);
            }
            catch { buttonPlayPause.Content = "Play"; }
            buttonPlayPause.ToolTip = "Play (CTRL+P)";
            
            _isPaused = false;
        }
        //аналогично
        private void cmdMute_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        //метод обработки отключения звука
        private void cmdMute_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //автоматически меняем громкость на слайдре
            sliderVolume.Value = mediaElementMain.IsMuted ? 0.5 : 0;
            mediaElementMain.IsMuted = !mediaElementMain.IsMuted;
        }
        //для информации о треке он должен быть выбран
        private void cmdMoreInfo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        private void cmdMoreInfo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //создаем окно с информацией по выбранному треку
            MoreInfoWindow moreInfoWindow = new MoreInfoWindow(_playListContainer.PlayListData[_currentSelectedIndex]);
            moreInfoWindow.ShowDialog();
        }
        //метод проверки возможности удаления трека
        private void cmdRemoveItem_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0 && listBoxPlaylist.SelectedItems.Count > 0;
        }
        //метод реализации удаления трека
        private void cmdRemoveItem_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure want to remove selected item?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question)
                 == MessageBoxResult.Yes)
            {
                if (listBoxPlaylist.SelectedIndex > -1)
                {
                    try
                    {
                        if (_currentlyPlayedFileName != "" || _isPaused)
                            cmdStop_Executed(sender,e);

                        _playListContainer.PlayListData.RemoveAt(_currentSelectedIndex);
                        textBlockDuration.Text = "00:00:00";

                        if (listBoxPlaylist.Items.Count > 0)
                        {
                            listBoxPlaylist.SelectedIndex = 0;
                            _currentSelectedIndex = 0;
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error {ex.Message} removing the item", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        //метод проверки возможности удаления всех треков
        private void cmdClearAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        //метод удаления всех треков
        private void cmdClearAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure want to clear all items?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.Yes)
                {
                    _playListContainer.PlayListData.Clear();
                    if (_currentlyPlayedFileName != "" || _isPaused)
                        cmdStop_Executed(sender, e);
                    textBlockDuration.Text = "00:00:00";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error {ex.Message} clearing items", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region сортировки
        //логика работы функций аналогична
        private void cmdSortByName_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        private void cmdSortByName_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var temp = _playListContainer.PlayListData.OrderBy(item => item.Name);
            _playListContainer.PlayListData = new ObservableCollection<PlayList>(temp);

            listBoxPlaylist.Items.Refresh();
        }
        private void cmdSortByPerformers_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        private void cmdSortByPerformers_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var temp = _playListContainer.PlayListData.OrderBy(item => item.Performers);
            _playListContainer.PlayListData = new ObservableCollection<PlayList>(temp);

            listBoxPlaylist.Items.Refresh();
        }
        private void cmdSortByAlbum_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        private void cmdSortByAlbum_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var temp = _playListContainer.PlayListData.OrderBy(item => item.Album);
            _playListContainer.PlayListData = new ObservableCollection<PlayList>(temp);

            listBoxPlaylist.Items.Refresh();
        }
        private void cmdSortByReleaseYear_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        private void cmdSortByReleaseYear_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var temp = _playListContainer.PlayListData.OrderBy(item => item.ReleaseYear);
            _playListContainer.PlayListData = new ObservableCollection<PlayList>(temp);

            listBoxPlaylist.Items.Refresh();
        }
        private void cmdSortByGenre_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        private void cmdSortByGenre_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var temp = _playListContainer.PlayListData.OrderBy(item => item.Genres);
            _playListContainer.PlayListData = new ObservableCollection<PlayList>(temp);

            listBoxPlaylist.Items.Refresh();
        }
        private void cmdSortByCountry_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _playListContainer == null ? false : _playListContainer.PlayListData.Count > 0;
        }
        private void cmdSortByCountry_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var temp = _playListContainer.PlayListData.OrderBy(item => item.Country);
            _playListContainer.PlayListData = new ObservableCollection<PlayList>(temp);

            listBoxPlaylist.Items.Refresh();
        }
        #endregion
        #endregion
    }
}
