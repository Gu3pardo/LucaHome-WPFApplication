﻿using Common.Dto;
using Common.Tools;
using Data.Controller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows.Media.Imaging;

namespace Data.Services
{
    public class NovelService
    {
        public delegate void NovelListDownloadEventHandler(IList<NovelDto> novelList, bool success, string response);

        private const string TAG = "NovelService";
        private readonly Logger _logger;

        private const int TIMEOUT = 6 * 60 * 60 * 1000;

        private readonly LocalDriveController _localDriveController;

        private static NovelService _instance = null;
        private static readonly object _padlock = new object();

        private DriveInfo _libraryDrive;
        private string _novelDir = string.Empty;
        private IList<NovelDto> _novelList = new List<NovelDto>();

        private Timer _reloadTimer;

        NovelService()
        {
            _logger = new Logger(TAG);

            _localDriveController = new LocalDriveController();

            _libraryDrive = _localDriveController.GetLibraryDrive();
            _novelDir = _libraryDrive.Name + "Books\\Romane";

            _reloadTimer = new Timer(TIMEOUT);
            _reloadTimer.Elapsed += _reloadTimer_Elapsed;
            _reloadTimer.AutoReset = true;
            _reloadTimer.Start();
        }

        public event NovelListDownloadEventHandler OnNovelListDownloadFinished;
        private void publishOnNovelListDownloadFinished(IList<NovelDto> novelList, bool success, string response)
        {
            OnNovelListDownloadFinished?.Invoke(novelList, success, response);
        }

        public static NovelService Instance
        {
            get
            {
                lock (_padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new NovelService();
                    }

                    return _instance;
                }
            }
        }

        public string NovelDir
        {
            get
            {
                return _novelDir;
            }
        }

        public IList<NovelDto> NovelList
        {
            get
            {
                return _novelList;
            }
        }

        public NovelDto GetByAuthor(string author)
        {
            NovelDto foundNovelDtos = _novelList
                        .Where(entry => entry.Author.Contains(author))
                        .Select(entry => entry)
                        .FirstOrDefault();

            return foundNovelDtos;
        }

        public IList<NovelDto> FoundNovelDtos(string searchKey)
        {
            List<NovelDto> foundNovelDtos = _novelList
                        .Where(entry =>
                            entry.Author.Contains(searchKey)
                            || entry.Icon.ToString().Contains(searchKey)
                            || entry.Books.Select(book => book).Contains(searchKey))
                        .Select(magazin => magazin)
                        .ToList();

            return foundNovelDtos;
        }

        public void LoadNovelList()
        {
            string[] extensionArray = new string[] { ".pdf", ".epub" };
            _novelList.Clear();
            string[] authorList = _localDriveController.ReadDirInDir(_novelDir);

            foreach (string authorDir in authorList)
            {
                string[] bookList = _localDriveController.ReadFileNamesInDir(authorDir, extensionArray);
                Uri iconUri = new Uri(string.Format("{0}\\_icon.jpg", authorDir), UriKind.Absolute);

                bookList = bookList
                    .Select(fileName => fileName.Replace(authorDir, "").Replace("\\", ""))
                    .OrderBy(fileName => fileName)
                    .ToArray();
                string author = authorDir.Replace(_novelDir, "").Replace("\\", "");
                BitmapImage icon = new BitmapImage(iconUri);

                NovelDto newEntry = new NovelDto(
                    author,
                    bookList,
                    icon);
                _novelList.Add(newEntry);
            }

            publishOnNovelListDownloadFinished(_novelList, true, "Success");
        }

        private void _reloadTimer_Elapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            _logger.Debug(string.Format("_reloadTimer_Elapsed with sender {0} and elapsedEventArgs {1}", sender, elapsedEventArgs));
            LoadNovelList();
        }

        public void Dispose()
        {
            _logger.Debug("Dispose");

            _reloadTimer.Elapsed -= _reloadTimer_Elapsed;
            _reloadTimer.AutoReset = false;
            _reloadTimer.Stop();
        }
    }
}