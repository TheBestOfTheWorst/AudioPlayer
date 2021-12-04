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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
        #region поля
        //общее и текущее время трека
        public TimeSpan TotalTimer { get; private set; }
        public TimeSpan ProgressTimer { get; private set; }
        //таймер для подсчета времени
        public DispatcherTimer Timer { get; private set; }
        //контейнер для всех загруженных треков
        public PlayListContainer PLContainer { get; private set; }
        //путь к картинке кнопки играть и пауза (класс Uri для удобства)
        public Uri PlayUri { get; private set; } = new Uri(@"Icons\Play.png", UriKind.Relative);
        public Uri PauseUri { get; private set; } = new Uri(@"Icons\Pause.png", UriKind.Relative);
        //текущий индекс листбокса
        public int CurrentSelectedIndex { get; private set; } = 0;
        //на паузе ли текущий трек
        public bool IsPaused { get; private set; } = false;
        //путь к располоению хранения текущего трека
        public string CurrentlyPlayedFileName { get; private set; } = "";
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            //подтягиваем контейнер с ресурсов окна
            PLContainer = TryFindResource("playListContainer") as PlayListContainer;
        }

        #region вспомогательные методы обработки
        //метод для обработки истечения тика таймера
        private void Timer_Tick(object sender, EventArgs e)
        {
            //запонимаем сколько времени прошло от начала трека
            ProgressTimer = mediaElementMain.Position;
            //если трек еще не закончился, обновляем UI
            if (ProgressTimer.TotalSeconds <= TotalTimer.TotalSeconds)
            {
                //обновляем слайдер
                sliderDuration.Value = ProgressTimer.TotalSeconds;
                textBlockProgress.Text = string.Format("{0:hh\\:mm\\:ss}", ProgressTimer);
            }
        }
        //асинхронный метод, который ждет пока у этого медиа будет доступен TimeSpan
        private Task<bool> DetectTimespan()
        {
            while (true)
            {
                if (mediaElementMain.NaturalDuration.HasTimeSpan)
                {
                    //возвращаем результат при успешном завершении
                    return Task.FromResult(true);
                }
            }
        }
        //метод для воспроизведения трека (async показывает, что используем await)
        private async void PlayMedia(string fileName)
        {
            try
            {
                //если трек не на паузе и путь верный, записываем в поля инфо о нем
                if (!IsPaused && fileName != "")
                {
                    CurrentlyPlayedFileName = fileName;
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
                    Timer.Start();

                    //сохраняем длительность трека
                    TotalTimer = mediaElementMain.NaturalDuration.TimeSpan;
                    sliderDuration.Maximum = TotalTimer.TotalSeconds;
                    //обновляем текст
                    textBlockDuration.Text = string.Format("{0:hh\\:mm\\:ss}", mediaElementMain.NaturalDuration.TimeSpan);
                    textBlockMediaStatus.Text = $"Playing from {CurrentlyPlayedFileName}";
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
                        Timer.IsEnabled = false;
                        Timer.Stop();
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
                CurrentlyPlayedFileName = "";

                if (await DetectTimespan())
                {
                    Timer.IsEnabled = false;
                    Timer.Stop();
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
            if (CurrentSelectedIndex - 1 >= 0)
                CurrentSelectedIndex--;
            else
                //если предыдущего файла нет, то играем последний
                CurrentSelectedIndex = PLContainer.PlayListData.Count - 1;
            
            return PLContainer.PlayListData[CurrentSelectedIndex].FullName;
        }
        //метод выдает имя следующего файла
        private string GetNextMediaFileName(bool next = false)
        {
            //next создан для останова, если нужен текущий трек
            if (next)
            {
                if (CurrentSelectedIndex + 1 < PLContainer.PlayListData.Count)
                    CurrentSelectedIndex++;
                else
                    //если следующего файла нет, вернем первый
                    CurrentSelectedIndex = 0;
            }

            return PLContainer.PlayListData[CurrentSelectedIndex].FullName;
        }
        #endregion

        #region методы обработки событий
        //метод вызывается после загрузки окна
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //создаем таймер на заднем фоне
            Timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            //подписываем событие таймера на нашу функцию
            Timer.Tick += Timer_Tick;

            try
            {
                using (StreamReader file = File.OpenText(@"Data.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    PLContainer.PlayListData = (List<PlayList>)serializer.Deserialize(file, typeof(List<PlayList>));
                }
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show($"Сохраненные данные не обнаружены", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сериализации данных \"{ex.Message}\" в файл", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //метод вызывается после закрытия окна
        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                using (StreamWriter file = File.CreateText(@"Data.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, PLContainer.PlayListData);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сериализации данных \"{ex.Message}\" в файл", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            Timer.Stop();
            //если в контейнере что-то есть, включаем следущий трек
            if (PLContainer.PlayListData.Count > 0)
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
            CurrentSelectedIndex = listBoxPlaylist.SelectedIndex > -1? listBoxPlaylist.SelectedIndex : 0;
            //запускаем его
            PlayMedia(GetNextMediaFileName());
            IsPaused = false;

            //пробуем подгрузить картинку
            try
            {
                BitmapImage image = new BitmapImage(PauseUri);
                imagePlayPause.Source = image;
            }
            catch { buttonPlayPause.Content = "Pause"; }
            buttonPlayPause.ToolTip = "Pause (CTRL+P)";
        }
        //обработка слайдера изменения точки воспроизведения медиа
        private void sliderDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //сначала проверим, верный ли путь к файлу
            if (mediaElementMain.Source != null)
            {
                //аналогично
                if (mediaElementMain.NaturalDuration.HasTimeSpan)
                {
                    ProgressTimer = TimeSpan.FromSeconds(sliderDuration.Value);
                    mediaElementMain.Position = ProgressTimer;
                }
            }
        }
        #endregion

        #region методы обработки кнопок и пунктов меню
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
                    //загружаем выбранные файлы по одному
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

                        PLContainer.AddSong(newList);
                    }
                }

                listBoxPlaylist.Items.Refresh();

                //меняем выбранный элемент если сейчас трек не играет
                if (CurrentlyPlayedFileName.Length == 0)
                {
                    listBoxPlaylist.SelectedIndex = PLContainer.PlayListData.Count - 1;
                    CurrentSelectedIndex = PLContainer.PlayListData.Count - 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //нажимать пауза и продолжить можно только если есть хоть один выбранный трек в контейнере
        private void cmdPlayPause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
        }
        //метод обработки паузы и продолжить
        private void cmdPlayPause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //удобно проверять состояние кнопки по подсказке
            if (buttonPlayPause.ToolTip.ToString() == "Play (CTRL+P)")
            {
                //если трек на паузе, включаем его (трек тот же)
                if (IsPaused)
                    PlayMedia("");
                //если трек просто не играет, то включаем текущий
                else
                    PlayMedia(GetNextMediaFileName());

                IsPaused = false;

                try
                {
                    imagePlayPause.Source = new BitmapImage(PauseUri);
                }
                catch { buttonPlayPause.Content = "Pause"; }
                buttonPlayPause.ToolTip = "Pause (CTRL+P)";
            }
            else if (buttonPlayPause.ToolTip.ToString() == "Pause (CTRL+P)")
            {
                //иначе ставим трек на паузу
                IsPaused = true;
                PauseMedia();

                try
                {
                    imagePlayPause.Source = new BitmapImage(PlayUri);
                }
                catch { buttonPlayPause.Content = "Play"; }
                buttonPlayPause.ToolTip = "Play (CTRL+P)";
            }
        }
        //нажимать предыдущий трек можно только если есть хоть один выбранный трек в контейнере
        private void cmdPrevious_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
        }
        //метод обработки предыдущий трек
        private void cmdPrevious_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //трек запускается сразу
            PlayMedia(GetPrevMediaFileName());
            IsPaused = false;

            //изменяем выбранный элемент в листбоксе
            if (listBoxPlaylist.SelectedIndex - 1 >= 0)
                listBoxPlaylist.SelectedIndex--;
            else
                listBoxPlaylist.SelectedIndex = PLContainer.PlayListData.Count - 1;

            try
            {
                imagePlayPause.Source = new BitmapImage(PauseUri);
            }
            catch { buttonPlayPause.Content = "Pause"; }
            buttonPlayPause.ToolTip = "Pause (CTRL+P)";
        }
        //аналогично
        private void cmdNext_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
        }
        //аналогично
        private void cmdNext_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PlayMedia(GetNextMediaFileName(true));

            //изменяем выбранный элемент в листбоксе
            if (listBoxPlaylist.SelectedIndex + 1 < PLContainer.PlayListData.Count)
                listBoxPlaylist.SelectedIndex++;
            else
                listBoxPlaylist.SelectedIndex = 0;

            IsPaused = false;

            try
            {
                imagePlayPause.Source = new BitmapImage(PauseUri);
            }
            catch { buttonPlayPause.Content = "Pause"; }
            buttonPlayPause.ToolTip = "Pause (CTRL+P)";
        }
        //для останова трек должен быть выбран, и он должен играть
        private void cmdStop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0 &&
                (CurrentlyPlayedFileName.Length > 0 || IsPaused);
        }
        //метод обработки кнопки стоп
        private void cmdStop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StopMedia();

            //изменяем картинку на продолжить
            try
            {
                imagePlayPause.Source = new BitmapImage(PlayUri);
            }
            catch { buttonPlayPause.Content = "Play"; }
            buttonPlayPause.ToolTip = "Play (CTRL+P)";
            
            IsPaused = false;
        }
        //аналогично
        private void cmdMute_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
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
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
        }
        private void cmdMoreInfo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //создаем окно с информацией по выбранному треку
            MoreInfoWindow moreInfoWindow = new MoreInfoWindow(PLContainer.PlayListData[CurrentSelectedIndex]);
            moreInfoWindow.ShowDialog();
        }
        //метод проверки возможности удаления трека
        private void cmdRemoveItem_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0 && listBoxPlaylist.SelectedItems.Count > 0;
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
                        if (CurrentlyPlayedFileName != "" || IsPaused)
                            cmdStop_Executed(sender,e);

                        PLContainer.RemoveSongAt(CurrentSelectedIndex);
                        textBlockDuration.Text = "00:00:00";

                        listBoxPlaylist.Items.Refresh();

                        if (listBoxPlaylist.Items.Count > 0)
                        {
                            listBoxPlaylist.SelectedIndex = 0;
                            CurrentSelectedIndex = 0;
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
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
        }
        //метод удаления всех треков
        private void cmdClearAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure want to clear all items?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.Yes)
                {
                    PLContainer.ClearAllSongs();
                    if (CurrentlyPlayedFileName != "" || IsPaused)
                        cmdStop_Executed(sender, e);
                    textBlockDuration.Text = "00:00:00";

                    listBoxPlaylist.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error {ex.Message} clearing items", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region сортировки
        //логика работы функций CanExecute аналогична
        private void cmdSortByName_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
        }
        private void cmdSortByName_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PLContainer.PlayListData = new List<PlayList>(PLContainer.PlayListData.OrderBy(item => item.Name));
            listBoxPlaylist.Items.Refresh();
        }
        private void cmdSortByPerformers_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
        }
        private void cmdSortByPerformers_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PLContainer.PlayListData = new List<PlayList>(PLContainer.PlayListData.OrderBy(item => item.Performers));
            listBoxPlaylist.Items.Refresh();
        }
        private void cmdSortByAlbum_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
        }
        private void cmdSortByAlbum_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PLContainer.PlayListData = new List<PlayList>(PLContainer.PlayListData.OrderBy(item => item.Album));
            listBoxPlaylist.Items.Refresh();
        }
        private void cmdSortByReleaseYear_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
        }
        private void cmdSortByReleaseYear_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PLContainer.PlayListData = new List<PlayList>(PLContainer.PlayListData.OrderBy(item => item.ReleaseYear));
            listBoxPlaylist.Items.Refresh();
        }
        private void cmdSortByGenre_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
        }
        private void cmdSortByGenre_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PLContainer.PlayListData = new List<PlayList>(PLContainer.PlayListData.OrderBy(item => item.Genres));
            listBoxPlaylist.Items.Refresh();
        }
        private void cmdSortByCountry_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PLContainer == null ? false : PLContainer.PlayListData.Count > 0;
        }
        private void cmdSortByCountry_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PLContainer.PlayListData = new List<PlayList>(PLContainer.PlayListData.OrderBy(item => item.Country));
            listBoxPlaylist.Items.Refresh();
        }
        #endregion
        #endregion
    }
}
