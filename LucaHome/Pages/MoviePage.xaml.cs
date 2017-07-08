﻿using Common.Common;
using Common.Dto;
using Common.Tools;
using Data.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

/*
 * Really helpful link
 * https://www.dotnetperls.com/listview-wpf
 */

namespace LucaHome.Pages
{
    public partial class MoviePage : Page
    {
        private const string TAG = "MoviePage";
        private readonly Logger _logger;

        private readonly NavigationService _navigationService;
        private readonly MovieService _movieService;

        private readonly Notifier _notifier;

        public MoviePage(NavigationService navigationService)
        {
            _logger = new Logger(TAG, Enables.LOGGING);

            _navigationService = navigationService;
            _movieService = MovieService.Instance;

            InitializeComponent();

            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: Application.Current.MainWindow,
                    corner: Corner.BottomRight,
                    offsetX: 15,
                    offsetY: 15);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(3));

                cfg.Dispatcher = Application.Current.Dispatcher;

                cfg.DisplayOptions.TopMost = true;
                cfg.DisplayOptions.Width = 250;
            });

            _notifier.ClearMessages();
        }

        private void Page_Loaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _logger.Debug(string.Format("Page_Loaded with sender {0} and routedEventArgs {1}", sender, routedEventArgs));

            _movieService.OnMovieDownloadFinished += _movieListDownloadFinished;
            _movieService.OnMovieStartFinished += _movieStartFinished;
            
            if (_movieService.MovieList == null)
            {
                _movieService.LoadMovieList();
                return;
            }

            setList();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _logger.Debug(string.Format("Page_Unloaded with sender {0} and routedEventArgs {1}", sender, routedEventArgs));

            _movieService.OnMovieDownloadFinished -= _movieListDownloadFinished;
            _movieService.OnMovieStartFinished -= _movieStartFinished;
        }

        private void setList()
        {
            _logger.Debug("setList");

            MovieList.Items.Clear();

            foreach (MovieDto entry in _movieService.MovieList)
            {
                MovieList.Items.Add(entry);
            }
        }

        private void _movieListDownloadFinished(IList<MovieDto> movieList, bool success)
        {
            _logger.Debug(string.Format("_movieListDownloadFinished with model {0} was successful: {1}", movieList, success));
            setList();
        }

        private void _movieStartFinished(bool success)
        {
            _logger.Debug(string.Format("_movieStartFinished was successful: {0}", success));
            if (success)
            {
                _notifier.ShowSuccess("Successfully started movie");
            }
            else
            {
                _notifier.ShowError("Failed to start movie");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            _logger.Debug(string.Format("Received click of sender {0} with arguments {1}", sender, routedEventArgs));
            if (sender is Button)
            {
                Button senderButton = (Button)sender;
                _logger.Debug(string.Format("Tag is {0}", senderButton.Tag));

                string movieTitle = (String)senderButton.Tag;
                _movieService.StartMovieOnPc(movieTitle);
            }
        }

        private void ButtonReload_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            _logger.Debug(string.Format("ButtonReload_Click with sender {0} and routedEventArgs {1}", sender, routedEventArgs));

            _movieService.LoadMovieList();
        }
    }
}
