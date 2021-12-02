//пространство имен для обработки кнопок управления
using System.Windows.Input;

namespace AudioPlayer.Commands
{
    //класс-обработчик всех команд
    public static class PlayerCoreCommands
    {
        //RoutedUICommand помогает обрабатывать кнопки (там есть полезные методы
        //CanExecute, Execute и т.п.

        //кнопка загрузить файл
        public static RoutedUICommand LoadCommand { get; set; } =
                new RoutedUICommand("Load Files", "LoadFiles", //описание и имя комманды
                typeof(PlayerCoreCommands), //тип комманды - мой класс
                new InputGestureCollection()
                {
                    //решил добавить еще эту фичу (не принципиально)  
                    new KeyGesture(Key.O,ModifierKeys.Control)
                });
        //кнопка продолжить/поставить на паузу
        public static RoutedUICommand PlayPauseCommand { get; set; } =
            new RoutedUICommand("Play/Pause", "PlayPause",
                typeof(PlayerCoreCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.P,ModifierKeys.Control)
                });
        //кнопка предыдущий трек
        public static RoutedUICommand PreviousCommand { get; set; } =
            new RoutedUICommand("Previous", "Previous",
                typeof(PlayerCoreCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.OemComma,ModifierKeys.Shift)
                });
        //кнопка следующий трек
        public static RoutedUICommand NextCommand { get; set; } =
            new RoutedUICommand("Next", "Next",
                typeof(PlayerCoreCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.OemPeriod,ModifierKeys.Shift)
                });
        //кнопка остановить трек
        public static RoutedUICommand StopCommand { get; set; } =
            new RoutedUICommand("Stop", "Stop",
                typeof(PlayerCoreCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.S,ModifierKeys.Control)
                });
        //кнопка выключить звук трека
        public static RoutedUICommand MuteCommand { get; set; } =
            new RoutedUICommand("Mute", "Mute",
                typeof(PlayerCoreCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.M,ModifierKeys.Control)
                });
        //обработка пункта меню удалить трек
        public static RoutedUICommand RemoveItemPlaylistCommand { get; set; } =
            new RoutedUICommand("Remove Selected Item", "RemoveSelectedItem",
                typeof(PlayerCoreCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.Delete,ModifierKeys.Control)
                });
        //обработка пункта меню удалить все треки
        public static RoutedUICommand ClearAllCommand { get; set; } =
            new RoutedUICommand("Clear All", "ClearAll",
                typeof(PlayerCoreCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.R,ModifierKeys.Control)
                });
        //обработка пункта меню информация о треке
        public static RoutedUICommand MoreInfoCommand { get; set; } =
            new RoutedUICommand("More info", "MoreInfo",
                typeof(PlayerCoreCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.I,ModifierKeys.Control)
                });
        //обработка пункта меню сортировка по выбранному критерию
        public static RoutedUICommand SortByNameCommand { get; set; } =
            new RoutedUICommand("Sort By Name", "SortByName",
            typeof(PlayerCoreCommands),
            new InputGestureCollection()
            {
                    new KeyGesture(Key.F1,ModifierKeys.Shift)
            });
        public static RoutedUICommand SortByPerformersCommand { get; set; } =
            new RoutedUICommand("Sort By Performers", "SortByPerformers",
            typeof(PlayerCoreCommands),
            new InputGestureCollection()
            {
                    new KeyGesture(Key.F2,ModifierKeys.Shift)
            });
        public static RoutedUICommand SortByAlbumCommand { get; set; } =
            new RoutedUICommand("Sort By Album", "SortByAlbum",
            typeof(PlayerCoreCommands),
            new InputGestureCollection()
            {
                    new KeyGesture(Key.F3,ModifierKeys.Shift)
            });
        public static RoutedUICommand SortByReleaseYearCommand { get; set; } =
            new RoutedUICommand("Sort By Release Year", "SortByReleaseYear",
            typeof(PlayerCoreCommands),
            new InputGestureCollection()
            {
                    new KeyGesture(Key.F5,ModifierKeys.Shift)
            });
        public static RoutedUICommand SortByGenreCommand { get; set; } =
            new RoutedUICommand("Sort By Genre", "SortByGenre",
            typeof(PlayerCoreCommands),
            new InputGestureCollection()
            {
                    new KeyGesture(Key.F6,ModifierKeys.Shift)
            });
        public static RoutedUICommand SortByCountryCommand { get; set; } =
            new RoutedUICommand("Sort By Country", "SortByCountry",
            typeof(PlayerCoreCommands),
            new InputGestureCollection()
            {
                    new KeyGesture(Key.F7,ModifierKeys.Shift)
            });
    }
}
