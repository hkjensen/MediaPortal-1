﻿#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Data;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Management;
using MediaPortal.Configuration;
using MediaPortal.Database;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Util;
using SQLite.NET;
using MediaPortal.Profile;


namespace MediaPortal.Video.Database.SqlServer
{
  /// <summary>
  /// Summary description for VideoDatabaseADO.
  /// </summary>
  public class VideoDatabaseADO : IVideoDatabase, IDisposable
  {
    private bool _currentCreateVideoThumbs;
    private Databases.videodatabaseEntities _connection;
    private bool _dbHealth = false;

    class custom1
    {
      public string IX { get; set; }
      public string CNT { get; set; }
    }

    #region ctor

    public VideoDatabaseADO()
    {
      Log.Info("VideoDatabaseADO");

      ConnectDb();
      CreateDb();
      SetLatestMovieProperties();
    }

    #endregion

    private void ConnectDb()
    {
      try
      {
        if (_connection != null)
        {
          return;
        }
      
        string host;
        string userName;
        string password;
        using (Settings reader = new MPSettings())
        {
          host = reader.GetValueAsString("mpdatabase", "hostname", string.Empty);
          userName = reader.GetValueAsString("mpdatabase", "username", "root");
          password = reader.GetValueAsString("mpdatabase", "password", "MediaPortal");

          if (host == string.Empty)
          {
            host = reader.GetValueAsString("tvservice", "hostname", "localhost");
          }
        }

        string ConnectionString = string.Format(
          "metadata=res://*/Model2.csdl|res://*/Model2.ssdl|res://*/Model2.msl;provider=MySql.Data.MySqlClient;provider connection string=\"server={0};user id={1};password={2};persistsecurityinfo=True;database=videodatabase;Convert Zero Datetime=True\"",
          host, userName, password);

         _connection = new Databases.videodatabaseEntities(ConnectionString);
      }
      catch (Exception ex)
      {
        Log.Error("VideodatabaseADO:ConnectDb exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
      }
    }

    private void CreateDb()
    {
      if (_connection == null)
      {
        return;
      }

      try
      {
        if (!_connection.DatabaseExists())
        {
          Log.Debug("VideoDatabaseADO: database is not exist, createing...");
          
          _connection.CreateDatabase();

          _connection.ExecuteStoreCommand("CREATE INDEX idxactors_idIMDB ON actors(IMDBActorID(500) ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxusergrouplinkmovie_idMovie ON usergrouplinkmovie (idMovie ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxusergrouplinkmovie_idGroup ON usergrouplinkmovie (idGroup ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxuserGroup_strGroup ON usergroup (strGroup(500) ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxpath_strPath ON path(strPath(500) ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxpath_idPath ON path(idPath ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxmovieinfo_strTitle ON movieinfo(strTitle(500) ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxmovieinfo_idIMDB ON movieinfo(IMDBID(500) ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxmovieinfo_idDirector ON movieinfo(idDirector ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxmovieinfo_iYear ON movieinfo(iYear ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxmovie_idPath ON movie(idPath ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxgenrelinkmovie_idMovie ON genrelinkmovie(idMovie ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxgenrelinkmovie_idGenre ON genrelinkmovie(idGenre ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxfiles_idPath ON files(idPath ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxfiles_idMovie ON files(idMovie ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxactors_strActor ON actors(strActor(500) ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxactorlinkmovie_idMovie ON actorlinkmovie(idMovie ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxactorlinkmovie_idActor ON actorlinkmovie(idActor ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxactorinfomovies_idActor ON actorinfomovies(idActor ASC)");
          //_connection.ExecuteStoreCommand("CREATE INDEX idxactorinfo_idActor ON actorinfo(idActor ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxVideoThumbBList_strPath ON VideoThumbBList(strPath(500) ASC, strExpires ASC)");
          _connection.ExecuteStoreCommand("CREATE INDEX idxVideoThumbBList_strExpires ON VideoThumbBList(strExpires ASC)");
          _connection.ExecuteStoreCommand("CREATE UNIQUE INDEX idximdbmovies_idIMDB ON IMDBmovies (idIMDB(500) ASC)");
          _connection.ExecuteStoreCommand("CREATE UNIQUE INDEX idxduration_idFile ON duration (idFile ASC)");
          _connection.ExecuteStoreCommand("CREATE UNIQUE INDEX idxfilesmediainfo_idFile ON filesmediainfo (idFile ASC)");
          _connection.ExecuteStoreCommand("CREATE UNIQUE INDEX idxmovieinfo_idMovie ON movieinfo(idMovie ASC)");
          //_connection.ExecuteStoreCommand("CREATE UNIQUE INDEX idxactors_idActor ON actors(idActor ASC)");
        }
        _dbHealth = true;
      }
      catch (Exception ex)
      {
        Log.Error("VideodatabaseADO:CreateDb exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
      }
    }

    public bool ClearDB()
    {
      if (_connection == null)
      {
        return false;
      }

      try
      {
        _connection.ExecuteStoreCommand("drop database videodatabase");
        Log.Debug("VideodatabaseADO:ClearDB videodatabase is dropped.");
        CreateDb();
      }
      catch (Exception ex)
      {
        Log.Error("VideodatabaseADO:ClearDB exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        return false;
      }
      return true;
    }

    public void Dispose()
    {
      if (_connection != null)
      {
        _connection.Dispose();
        _connection = null;
      }
    }

    #region Movie files and paths

    public int AddFile(int lMovieId, int lPathId, string strFileName)
    {
      Log.Debug("VideodatabaseADO AddFile:{0}", strFileName);

      if (null == _connection)
      {
        return -1;
      }
      try
      {
        int lFileId = -1;
        strFileName = strFileName.Trim();

        var query = (from sql in _connection.files
                      where sql.idMovie == lMovieId && sql.idPath == lPathId && sql.strFilename == strFileName
                      select sql).FirstOrDefault();

        if (query != null)
        {
          lFileId = query.idFile;
          CheckMediaInfo(strFileName, string.Empty, lPathId, lFileId, false);
          return lFileId;
        }

        Databases.file obj = new Databases.file()
        {
          strFilename = strFileName,
          idMovie = lMovieId,
          idPath = lPathId
        };

        _connection.files.AddObject(obj);
        _connection.SaveChanges();

        var query2 = (from sql in _connection.files
                      where sql.idMovie == lMovieId && sql.idPath == lPathId && sql.strFilename == strFileName
                      select sql).FirstOrDefault();

        lFileId = query2.idFile;

        CheckMediaInfo(strFileName, string.Empty, lPathId, lFileId, false);

        return lFileId;
      }
      catch (Exception ex)
      {
        Log.Error("VideodatabaseADO:Addfile exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
      return -1;
    }

    private int MovieDuration(ArrayList files)
    {
      int totalMovieDuration = 0;

      if (files == null || files.Count == 0)
      {
        return totalMovieDuration;
      }

      try
      {
        foreach (string file in files)
        {
          int fileID = VideoDatabase.GetFileId(file);
          int tempDuration = VideoDatabase.GetVideoDuration(fileID);

          totalMovieDuration += tempDuration;
        }
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex) 
      {
        Log.Error("VideodatabaseADO:MovieDuration exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }

      return totalMovieDuration;
    }

    public int GetFile(string strFilenameAndPath, out int lPathId, out int lMovieId, bool bExact)
    {
      lPathId = -1;
      lMovieId = -1;

      try
      {
        if (null == _connection)
        {
          return -1;
        }

        string strPath, strFileName;
        strFilenameAndPath = strFilenameAndPath.Trim();
        DatabaseUtility.Split(strFilenameAndPath, out strPath, out strFileName);
        RemoveInvalidChars(ref strPath);
        RemoveInvalidChars(ref strFileName);
        lPathId = GetPath(strPath);

        if (lPathId < 0)
        {
          return -1;
        }

        int lPathId2 = lPathId;

        var query = (from sql in _connection.files
                      where sql.idPath == lPathId2 && sql.strFilename == strFileName
                      select sql).FirstOrDefault();

        if (query != null)
        {
          lMovieId = (int)query.idMovie;

          return query.idFile;
        }
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("VideodatabaseADO:GetFile exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
      return -1;
    }

    public int GetTitleBDId(int iFileId, out byte[] resumeData)//, int bdtitle)
    {
      resumeData = null;

      try
      {
        var query = (from sql in _connection.resumes
                     where sql.idFile == iFileId
                     select sql).FirstOrDefault<Databases.resume>();

        if (query != null)
        {
          return (int)query.bdtitle;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetTitleBDId exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return 0;
    }

    public int AddMovieFile(string strFile)
    {
      bool bHassubtitles = false;

      if (strFile.ToLowerInvariant().IndexOf(".ifo") >= 0)
      {
        bHassubtitles = true;
      }

      if (strFile.ToLowerInvariant().IndexOf(".vob") >= 0)
      {
        bHassubtitles = true;
      }

      string strCDLabel = "";

      if (Util.Utils.IsDVD(strFile))
      {
        strCDLabel = Util.Utils.GetDriveSerial(strFile);
      }

      string[] sub_exts = {
                            ".utf", ".utf8", ".utf-8", ".sub", ".srt", ".smi", ".rt", ".txt", ".ssa", ".aqt", ".jss",
                            ".ass", ".idx", ".ifo"
                          };

      // check if movie has subtitles
      for (int i = 0; i < sub_exts.Length; i++)
      {
        string strSubTitleFile = strFile;
        strSubTitleFile = Path.ChangeExtension(strFile, sub_exts[i]);

        if (File.Exists(strSubTitleFile))
        {
          bHassubtitles = true;
          break;
        }
      }
      return VideoDatabase.AddMovie(strFile, bHassubtitles);
    }

    public int AddPath(string strPath)
    {
      try
      {
        if (null == _connection)
        {
          return -1;
        }

        string cdlabel = GetDVDLabel(strPath);
        RemoveInvalidChars(ref cdlabel);
        strPath = strPath.Trim();

        var query = (from sql in _connection.paths
                     where sql.strPath == strPath && sql.cdlabel == cdlabel
                     select sql).FirstOrDefault();

        if (query == null)
        {
          Log.Debug("VideodatabaseADO: AddPath doesnt exists, add it  {0}", strPath);

          Databases.path path = new Databases.path()
          {
            strPath = strPath,
            cdlabel = cdlabel
          };

          _connection.paths.AddObject(path);
          _connection.SaveChanges();

          var query2 = (from sql in _connection.paths
                       where sql.strPath == strPath && sql.cdlabel == cdlabel
                       select sql).FirstOrDefault();

          return query2.idPath;
        }
        else
        {
          return query.idPath;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddPath exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return -1;
    }

    public int GetPath(string strPath)
    {
      try
      {
        if (null == _connection || string.IsNullOrEmpty(strPath))
        {
          return -1;
        }

        string cdlabel = string.Empty;
        string strSQL = string.Empty;
        strPath = strPath.Trim();

        if (Util.Utils.IsDVD(strPath))
        {
          // It's a DVD! Any drive letter should be OK as long as the label and rest of the path matches
          cdlabel = GetDVDLabel(strPath);
          RemoveInvalidChars(ref cdlabel);
          strPath = strPath.Replace(strPath.Substring(0, 1), "_");

          var query = (from sql in _connection.paths
                        where sql.strPath == strPath && sql.cdlabel == cdlabel
                        select sql).FirstOrDefault();

          if (query != null)
            return query.idPath;
        }
        else
        {
          var query = (from sql in _connection.paths
                        where sql.strPath == strPath
                        select sql).FirstOrDefault();

          if (query != null)
            return query.idPath;
        }
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetPath exception err:{0} stack:{1}, {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
      return -1;
    }

    public void DeleteFile(int iFileId)
    {
      if (null == _connection)
      {
        return;
      }
      
      try
      {
        var delObj = (from u in _connection.files
                      where u.idFile == iFileId
                      select u).FirstOrDefault();

        if (delObj != null)
        {
          _connection.DeleteObject(delObj);
          _connection.SaveChanges();
        }

        var delObj2 = (from u in _connection.resumes
                      where u.idFile == iFileId
                      select u).FirstOrDefault();

        if (delObj2 != null)
        {
          _connection.DeleteObject(delObj2);
          _connection.SaveChanges();
        }

        var delObj3 = (from u in _connection.durations
                       where u.idFile == iFileId
                       select u).FirstOrDefault();

        if (delObj3 != null)
        {
          _connection.DeleteObject(delObj3);
          _connection.SaveChanges();
        }

        var delObj4 = (from u in _connection.filesmediainfoes
                       where u.idFile == iFileId
                       select u).FirstOrDefault();

        if (delObj4 != null)
        {
          _connection.DeleteObject(delObj4);
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:DeleteFile exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public void RemoveFilesForMovie(int lMovieId)
    {
      try
      {
        if (null == _connection)
        {
          return;
        }

        // Delete files data from other tables
        string strSQL = string.Empty;
        ArrayList files = new ArrayList();
        GetFilesForMovie(lMovieId, ref files);

        foreach (string file in files)
        {
          int idFile = GetFileId(file);

          var delObj = (from u in _connection.resumes
                        where u.idFile == idFile
                        select u).FirstOrDefault();

          if (delObj != null)
          {
            _connection.DeleteObject(delObj);
            _connection.SaveChanges();
          }

          var delObj2 = (from u in _connection.durations
                        where u.idFile == idFile
                        select u).FirstOrDefault();

          if (delObj2 != null)
          {
            _connection.DeleteObject(delObj2);
            _connection.SaveChanges();
          }

          var delObj3 = (from u in _connection.filesmediainfoes
                         where u.idFile == idFile
                         select u).FirstOrDefault();

          if (delObj3 != null)
          {
            _connection.DeleteObject(delObj3);
            _connection.SaveChanges();
          }
        }

        var delObj4 = (from u in _connection.files
                       where u.idMovie == lMovieId
                       select u).FirstOrDefault();

        if (delObj4 != null)
        {
          _connection.DeleteObject(delObj4);
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:RemoveFilesMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public int GetFileId(string strFilenameAndPath)
    {
      int lPathId;
      int lMovieId;
      return GetFile(strFilenameAndPath, out lPathId, out lMovieId, true);
    }

    private int GetFileId(int movieId)
    {
      int fileId = -1;

      try
      {
        if (null == _connection)
        {
          return -1;
        }

        var query = (from sql in _connection.files
                     where sql.idMovie == movieId
                     select sql).FirstOrDefault<Databases.file>();

        if (query != null)
        {
          fileId = query.idFile;
        }
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetFileId exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }

      return fileId;
    }

    public void GetFilesForMovie(int lMovieId, ref ArrayList files)
    {
      try
      {
        files.Clear();

        if (_connection == null)
        {
          return;
        }

        if (lMovieId < 0)
        {
          return;
        }

        var query = (from sql in _connection.paths
                     join sql2 in _connection.files on sql.idPath equals sql2.idPath
                     where sql2.idMovie == lMovieId
                     select new {sql.strPath, sql2.strFilename} ).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int i = 0; i < query.Count; ++i)
        {
          string strFile = query[i].strFilename;
          string strPath = query[i].strPath;

          if (strPath != Strings.Unknown)
          {
            strFile = strPath + strFile;
          }
          files.Add(strFile);
        }
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetFilesForMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    #endregion

    # region MediaInfo

    // Check and add, if necessary, media info for video files
    // Use (file, pathID and fileID) or (full filename with path and fileID)
    private void CheckMediaInfo(string file, string fullPathFilename, int pathID, int fileID, bool refresh)
    {
      try
      {
        string strSQL = string.Empty;
        string strFilenameAndPath = string.Empty;

        // Get path name from pathID

        var query = (from sql in _connection.paths
                     where sql.idPath == pathID
                     select sql).FirstOrDefault<Databases.path>();

        // No ftp or http videos
        if (query == null)
        {
          return;
        }

        string path = query.strPath;

        if (path.IndexOf("remote:") >= 0 || path.IndexOf("http:") >= 0)
        {
          return;
        }

        // We can use (path+file) or full path filename
        if (fullPathFilename == string.Empty)
        {
          strFilenameAndPath = path + file.Replace("''", "'");
        }
        else
        {
          strFilenameAndPath = fullPathFilename;
        }

        // Prevent empty database record for empty media scan
        if (!File.Exists(strFilenameAndPath))
        {
          return;
        }

        // Check if we processed file allready
        var query2 = (from sql in _connection.filesmediainfoes
                      where sql.idFile == fileID
                      select sql).FirstOrDefault();

        if (query2 == null || refresh)
        {
          Log.Info("VideoDatabaseADO media info scanning file: {0}", strFilenameAndPath);
          bool isImage = false;
          string drive = string.Empty;
          bool daemonAutoPlay = false;
          string autoplayVideo = string.Empty;

          if (VirtualDirectory.IsImageFile(Path.GetExtension(strFilenameAndPath)))
          {
            if (!DaemonTools.IsMounted(strFilenameAndPath))
            {
              using (Settings xmlreader = new MPSettings())
              {
                daemonAutoPlay = xmlreader.GetValueAsBool("daemon", "askbeforeplaying", false);
                autoplayVideo = xmlreader.GetValueAsString("general", "autoplay_video", "Ask");
                xmlreader.SetValueAsBool("daemon", "askbeforeplaying", false);
                xmlreader.SetValue("general", "autoplay_video", "No");
              }

              if (!DaemonTools.Mount(strFilenameAndPath, out drive))
              {
                return;
              }
            }
            isImage = true;
          }

          // Set currentMediaInfoFilePlaying for later use if it's the same media to play (it will cache mediainfo data)
          MediaInfoWrapper mInfo = null;
          if (!string.IsNullOrEmpty(g_Player.currentMediaInfoFilePlaying) && (g_Player.currentMediaInfoFilePlaying == strFilenameAndPath))
          {
            mInfo = g_Player._mediaInfo;
          }
          else
          {
            g_Player.currentMediaInfoFilePlaying = strFilenameAndPath;
            mInfo = g_Player._mediaInfo = new MediaInfoWrapper(strFilenameAndPath);
          }

          if (isImage && DaemonTools.IsMounted(strFilenameAndPath))
          {
            DaemonTools.UnMount();
            using (Settings xmlwriter = new MPSettings())
            {
              xmlwriter.SetValueAsBool("daemon", "askbeforeplaying", daemonAutoPlay);
              xmlwriter.SetValue("general", "autoplay_video", autoplayVideo);
            }
          }

          int subtitles = 0;

          if (mInfo.HasSubtitles)
          {
            subtitles = 1;
          }

          try
          {
            // Prevent empty record for future or unknown codecs
            if (mInfo.VideoCodec == string.Empty)
            {
              return;
            }

            if (query2 == null)
            {
              Databases.filesmediainfo obj = new Databases.filesmediainfo()
              {
                idFile = fileID,
                videoCodec = Util.Utils.MakeFileName(mInfo.VideoCodec),
                videoResolution = mInfo.VideoResolution,
                aspectRatio = mInfo.AspectRatio,
                hasSubtitles = Convert.ToBoolean(subtitles),
                audioCodec = Util.Utils.MakeFileName(mInfo.AudioCodec),
                audioChannels = mInfo.AudioChannelsFriendly
              };

              _connection.filesmediainfoes.AddObject(obj);
            }
            else
            {
              var query3 = (from sql in _connection.filesmediainfoes
                           where sql.idFile == fileID
                           select sql).ToList();

              foreach (Databases.filesmediainfo q in query3)
              {
                q.videoCodec = Util.Utils.MakeFileName(mInfo.VideoCodec);
                q.videoResolution = mInfo.VideoResolution;
                q.aspectRatio = mInfo.AspectRatio;
                q.hasSubtitles = Convert.ToBoolean(subtitles);
                q.audioCodec = Util.Utils.MakeFileName(mInfo.AudioCodec);
                q.audioChannels = mInfo.AudioChannelsFriendly;
              }
            }

            _connection.SaveChanges();

            SetVideoDuration(fileID, mInfo.VideoDuration / 1000);
            ArrayList movieFiles = new ArrayList();
            int movieId = VideoDatabase.GetMovieId(strFilenameAndPath);
            VideoDatabase.GetFilesForMovie(movieId, ref movieFiles);
            SetMovieDuration(movieId, MovieDuration(movieFiles));

            //Update movie subtitle field

            var query5 = (from sql in _connection.movies
                          where sql.idMovie == movieId
                          select sql).FirstOrDefault();

            if (query5 != null)
            {
              query5.hasSubtitles = subtitles;
              _connection.SaveChanges();
            }
          }
          catch (ThreadAbortException)
          {
            // Will be logged in thread main code
          }
          catch (Exception ex)
          {
            Log.Error("videodatabaseADO:CheckMediaInfo exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:CheckMediaInfo kulso exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public void GetVideoFilesMediaInfo(string strFilenameAndPath, ref VideoFilesMediaInfo mediaInfo, bool refresh)
    {
      try
      {
        if (strFilenameAndPath == String.Empty || !Util.Utils.IsVideo(strFilenameAndPath))
        {
          return;
        }

        if (strFilenameAndPath.IndexOf("remote:") >= 0 || strFilenameAndPath.IndexOf("http:") >= 0)
        {
          return;
        }

        int fileID = GetFileId(strFilenameAndPath);

        if (fileID < 1)
        {
          return;
        }

        // Get media info from database
        var query = (from sql in _connection.filesmediainfoes
                      where sql.idFile == fileID
                      select sql).FirstOrDefault();

        if (query == null)
        {
          return;
        }

        // Set mInfo for files already in db but not scanned before
        if (query != null && refresh) // Only refresh from context menu "Refresh media info" from shares view
        {
          try
          {
            CheckMediaInfo(string.Empty, strFilenameAndPath, -1, fileID, refresh);

            query = (from sql in _connection.filesmediainfoes
                     where sql.idFile == fileID
                     select sql).FirstOrDefault();
          }
          catch (Exception) { }
        }

        mediaInfo.VideoCodec = query.videoCodec;
        mediaInfo.VideoResolution = query.videoResolution;
        mediaInfo.AspectRatio = query.aspectRatio;

        int hasSubtitles = Convert.ToInt32(query.hasSubtitles);

        if (hasSubtitles != 0)
        {
          mediaInfo.HasSubtitles = true;
        }
        else
        {
          mediaInfo.HasSubtitles = false;
        }
        
        mediaInfo.AudioCodec = query.audioCodec;
        mediaInfo.AudioChannels = query.audioChannels;
        mediaInfo.Duration = GetVideoDuration(fileID);
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetVideoFilesMediaInfo mediainfo exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    private void GetVideoFilesMediaInfo(int movieId, ref VideoFilesMediaInfo mediaInfo)
    {
      try
      {
        if (movieId < 1)
        {
          return;
        }

        int fileID = GetFileId(movieId);

        if (fileID < 1)
        {
          return;
        }

        // Get media info from database
        var query = (from sql in _connection.filesmediainfoes
                 where sql.idFile == fileID
                 select sql).FirstOrDefault();

        if (query == null)
        {
          return;
        }

        mediaInfo.VideoCodec = query.videoCodec;
        mediaInfo.VideoResolution = query.videoResolution;
        mediaInfo.AspectRatio = query.aspectRatio;

        int hasSubtitles = Convert.ToInt32(query.hasSubtitles);

        if (hasSubtitles != 0)
        {
          mediaInfo.HasSubtitles = true;
        }
        else
        {
          mediaInfo.HasSubtitles = false;
        }

        mediaInfo.AudioCodec = query.audioCodec;
        mediaInfo.AudioChannels = query.audioChannels;
        mediaInfo.Duration = GetVideoDuration(fileID);
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetVideoFilesMediaInfo mediainfo exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public bool HasMediaInfo(string fileName)
    {
      try
      {
        if (fileName == String.Empty || !Util.Utils.IsVideo(fileName))
          return false;

        if (fileName.IndexOf("remote:") >= 0 || fileName.IndexOf("http:") >= 0)
          return false;

        int fileID = GetFileId(fileName);

        if (fileID < 1)
        {
          return false;
        }

        // Get media info from database
        var query = (from sql in _connection.filesmediainfoes
                     where sql.idFile == fileID
                     select sql).FirstOrDefault();

        if (query != null)
        {
          return true;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:HasMediaInfo mediainfo exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return false;
    }

    #endregion

    #region Genres

    public int AddGenre(string strGenre1)
    {
      try
      {
        string strGenre = strGenre1.Trim();
        RemoveInvalidChars(ref strGenre);

        if (_connection == null)
        {
          return -1;
        }

        var query = (from sql in _connection.genres
                     where sql.strGenre == strGenre
                     select sql).FirstOrDefault();

        if (query == null)
        {
          // doesnt exists, add it
          Databases.genre ins = new Databases.genre()
          {
            strGenre = strGenre
          };
          _connection.genres.AddObject(ins);
          _connection.SaveChanges();

          var query2 = (from sql in _connection.genres
                       where sql.strGenre == strGenre
                       select sql).FirstOrDefault();

          return query2.idGenre;
        }
        else
        {
          return query.idGenre;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddGenre exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return -1;
    }

    public void GetGenres(ArrayList genres)
    {
      if (_connection == null)
      {
        return;
      }
      try
      {
        genres.Clear();

        var query = (from sql in _connection.genres
                     orderby sql.strGenre
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          genres.Add(query[iRow].strGenre);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetGenres exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public string GetGenreById(int genreId)
    {
      if (_connection == null)
      {
        return string.Empty;
      }

      string strGenre = string.Empty;

      try
      {
        var query = (from sql in _connection.genres
                     where sql.idGenre == genreId
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return string.Empty;
        }

        strGenre = query.strGenre;
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetGenreById exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return strGenre;
    }

    public void AddGenreToMovie(int lMovieId, string strGenre)
    {
        AddGenreToMovie(lMovieId, AddGenre(strGenre));
    }

    public void AddGenreToMovie(int lMovieId, int lGenreId)
    {
      try
      {
        Log.Debug("videodatabaseADO:AddGenreToMovie begin {0} {1}", lMovieId, lGenreId);
        if (null == _connection)
        {
          return;
        }
//        var query = (from sql in _connection.genrelinkmovies
//                     where sql.idGenre == lGenreId && sql.idMovie == lGenreId
//                     select sql).FirstOrDefault();
//
//        if (query == null)
//        {
          // doesnt exists, add it
          Databases.genrelinkmovie u = new Databases.genrelinkmovie()
          {
            idGenre = lGenreId,
            idMovie = lMovieId
          };
          _connection.genrelinkmovies.AddObject(u);
          _connection.SaveChanges();
//        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddGenreToMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void DeleteGenre(string genre)
    {
      try
      {
        string genreFiltered = genre;
        RemoveInvalidChars(ref genreFiltered);

        var query = (from sql in _connection.genres
                     where sql.strGenre == genreFiltered
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return;
        }

        int idGenre = query.idGenre;

        var delObj = (from u in _connection.genrelinkmovies
                      where u.idGenre == idGenre
                      select u).FirstOrDefault();

        if (delObj != null)
        {
          _connection.DeleteObject(delObj);
          _connection.SaveChanges();
        }

        var delObj2 = (from u in _connection.genres
                      where u.idGenre == idGenre
                      select u).FirstOrDefault();

        if (delObj2 != null)
        {
          _connection.DeleteObject(delObj2);
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:DeleteGenre exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void RemoveGenreForMovie(int lMovieId, string strGenre)
    {
        try
        {
            if (_connection == null)
            {
                return;
            }

            strGenre = strGenre.Trim();
            RemoveInvalidChars(ref strGenre);
            var query = (from sql in _connection.genres
                     where sql.strGenre == strGenre
                     select sql).FirstOrDefault();

            if (query == null)
            {
              return;
            }
            int idGenre = query.idGenre;
            var delObj = (from u in _connection.genrelinkmovies
                          where (u.idMovie == lMovieId && u.idGenre == idGenre)
                          select u).FirstOrDefault();

            if (delObj != null)
            {
                _connection.DeleteObject(delObj);
                _connection.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Log.Error("videodatabaseADO:RemoveGenresForMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
            ConnectDb();
        }
    }


    public void RemoveGenresForMovie(int lMovieId)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var delObj = (from u in _connection.genrelinkmovies
                      where u.idMovie == lMovieId
                      select u).FirstOrDefault();

        if (delObj != null)
        {
          _connection.DeleteObject(delObj);
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:RemoveGenresForMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    #endregion

    #region UserGroups

    public int AddUserGroup(string userGroup)
    {
      try
      {
        string strUserGroup = userGroup.Trim();
        RemoveInvalidChars(ref strUserGroup);

        if (_connection == null)
        {
          return -1;
        }

        var query = (from sql in _connection.usergroups
                     where sql.strGroup == strUserGroup
                     select sql).FirstOrDefault();
        
        if (query == null)
        {
          // doesnt exists, add it
          Databases.usergroup obj = new Databases.usergroup()
          {
            strGroup = strUserGroup,
            strRule = string.Empty,
            strGroupDescription = string.Empty
          };

          _connection.usergroups.AddObject(obj);
          _connection.SaveChanges();

          var query2 = (from sql in _connection.usergroups
                       where sql.strGroup == strUserGroup
                       select sql).FirstOrDefault();

          return query2.idGroup;
        }
        else
        {
          return query.idGroup;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddUserGroup exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return -1;
    }

    public int GetUserGroupId(string userGroup)
    {
      try
      {
        string strUserGroup = userGroup.Trim();
        RemoveInvalidChars(ref strUserGroup);

        if (_connection == null)
        {
          return -1;
        }

        var query = (from sql in _connection.usergroups
                     where sql.strGroup == strUserGroup
                     select sql).FirstOrDefault();

        if (query != null)
        {
          return query.idGroup;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetUserGroupId exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return -1;
    }

    public void AddUserGroupDescription(string userGroup, string description)
    {
      try
      {
        string strUserGroup = userGroup.Trim();
        RemoveInvalidChars(ref strUserGroup);

        string strGroupDescription = description.Trim();
        RemoveInvalidChars(ref strGroupDescription);

        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.usergroups
                     where sql.strGroup == strUserGroup
                     select sql).FirstOrDefault();

        if (query != null)
        {
          int groupId = query.idGroup;

          if (!string.IsNullOrEmpty(strGroupDescription) && strGroupDescription != Strings.Unknown)
          {
            var query2 = (from sql in _connection.usergroups
                          where sql.idGroup == groupId
                         select sql).FirstOrDefault();

            query2.strGroupDescription = strGroupDescription;
            _connection.SaveChanges();
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddUserGroupDescription exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void AddUserGroupRuleByGroupId(int groupId, string rule)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.usergroups
                      where sql.idGroup == groupId
                      select sql).FirstOrDefault();

        if (query != null)
        {
          query.strRule = rule;
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddUserGroupRuleByGroupId exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void AddUserGroupRuleByGroupName(string groupName, string rule)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.usergroups
                     where sql.strGroup == groupName
                     select sql).FirstOrDefault();

        if (query != null)
        {
          query.strRule = rule;
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddUserGroupRuleByGroupName exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void GetUserGroups(ArrayList userGroups)
    {
      if (_connection == null)
      {
        return;
      }
      try
      {
        userGroups.Clear();

        var query = (from sql in _connection.usergroups
                     orderby sql.strGroup
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          userGroups.Add(query[iRow].strGroup);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetUserGroups exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public string GetUserGroupById(int groupId)
    {
      if (_connection == null)
      {
        return string.Empty;
      }

      string strGroup = string.Empty;

      try
      {
        var query = (from sql in _connection.usergroups
                     where sql.idGroup == groupId
                     select sql).FirstOrDefault();

        if (query != null)
        {
          strGroup = query.strGroup;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetUserGroupById exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return strGroup;
    }

    public string GetUserGroupDescriptionById(int groupId)
    {
      if (_connection == null)
      {
        return string.Empty;
      }

      string strGroup = string.Empty;

      try
      {
        var query = (from sql in _connection.usergroups
                     where sql.idGroup == groupId
                     select sql).FirstOrDefault();

        if (query != null)
        {
          strGroup = query.strGroupDescription;
        }

        if (strGroup == Strings.Unknown)
        {
          strGroup = string.Empty;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetUserGroupDescriptionById exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return strGroup;
    }

    public void GetMovieUserGroups(int movieId, ArrayList userGroups)
    {
      if (_connection == null)
      {
        return;
      }

      try
      {
        userGroups.Clear();

        var query = (from sql in _connection.usergrouplinkmovies
                     where sql.idMovie == movieId
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          int groupId = (int)query[iRow].idGroup;

          var query2 = (from sql in _connection.usergroups
                        where sql.idGroup == groupId
                        select sql).FirstOrDefault();

          if (query2 != null)
          {
            userGroups.Add(query2.strGroup);
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieUserGroups exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public string GetUserGroupRule(string groups)
    {
      try
      {
        if (_connection == null)
        {
          return string.Empty;
        }

        var query = (from sql in _connection.usergroups
                      where sql.strGroup == groups
                      select sql).FirstOrDefault();

        if (query != null)
        {
          return query.strRule;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetUserGroupRule exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return string.Empty;
    }

    public void AddUserGroupToMovie(int lMovieId, int lUserGroupId)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.usergrouplinkmovies
                     where sql.idMovie == lMovieId && sql.idGroup == lUserGroupId
                     select sql);

        if (query.FirstOrDefault() == null)
        {
          // doesnt exists, add it

          Databases.usergrouplinkmovie obj = new Databases.usergrouplinkmovie()
          {
            idGroup = lUserGroupId,
            idMovie = lMovieId
          };
          _connection.usergrouplinkmovies.AddObject(obj);
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddUserGroupToMovieAddUserGroupToMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void RemoveUserGroupFromMovie(int lMovieId, int lUserGroupId)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.usergrouplinkmovies
                     where sql.idMovie == lMovieId && sql.idGroup == lUserGroupId
                     select sql).FirstOrDefault();

        if (query != null)
        {
          _connection.DeleteObject(query);
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:RemoveUserGroupFromMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void DeleteUserGroup(string userGroup)
    {
      try
      {
        string userGroupFiltered = userGroup;
        RemoveInvalidChars(ref userGroupFiltered);

        var query = (from sql in _connection.usergroups
                     where sql.strGroup == userGroupFiltered
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return;
        }

        int idUserGroup = query.idGroup;

        var query2 = (from sql in _connection.usergroups
                     where sql.idGroup == idUserGroup
                     select sql).FirstOrDefault();

        _connection.DeleteObject(query2);
        _connection.SaveChanges();

        var query3 = (from sql in _connection.usergrouplinkmovies
                      where sql.idGroup == idUserGroup
                      select sql).ToList();

        foreach (var q in query3)
        {
          _connection.DeleteObject(q);
        }
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:DeleteUserGroup exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }

      string thumb = Util.Utils.GetCoverArtName(Thumbs.MovieUserGroups, userGroup);
      string largeThumb = Util.Utils.GetLargeCoverArtName(Thumbs.MovieUserGroups, userGroup);
      Util.Utils.FileDelete(thumb);
      Util.Utils.FileDelete(largeThumb);
    }

    public void RemoveUserGroupsForMovie(int lMovieId)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.usergrouplinkmovies
                      where sql.idMovie == lMovieId
                      select sql).ToList();

        foreach (var q in query)
        {
          _connection.DeleteObject(q);
        }
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:RemoveUserGroupsForMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void RemoveUserGroupRule(string groupName)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.usergroups
                     where sql.strGroup == groupName
                     select sql).ToList();

        foreach (var q in query)
        {
          q.strRule = string.Empty;
        }
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:RemoveUserGroupRule exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    #endregion

    #region Actors

    public int AddActor(string strActorImdbId, string strActorName)
    {
      try
      {
        RemoveInvalidChars(ref strActorImdbId);
        RemoveInvalidChars(ref strActorName);

        if (_connection == null)
        {
          return -1;
        }

        string strSQL = string.Empty;
        int lActorId = -1;

        if (string.IsNullOrEmpty(strActorImdbId) || strActorImdbId == Strings.Unknown)
        {
          var query = (from sql in _connection.actors
                       where sql.strActor == strActorName
                       select sql).FirstOrDefault();

          if (query != null)
          {
            lActorId = query.idActor;
          }
        }
        else
        {
          var query = (from sql in _connection.actors
                       where sql.IMDBActorID == strActorImdbId
                       select sql).FirstOrDefault();

          if (query != null)
          {
            lActorId = query.idActor;
          }
        }

        if (lActorId == -1)
        {
          // doesnt exists, add it but check first if it exists without ImdbId
          int idActor = CheckActorByName(strActorName);

          if (idActor != -1)
          {
            var query = (from sql in _connection.actors
                         where sql.idActor == idActor
                         select sql).FirstOrDefault();

            query.IMDBActorID = strActorImdbId;
            query.strActor = strActorName;

            _connection.SaveChanges();

            return idActor;
          }
          else
          {
            Databases.actor obj = new Databases.actor()
            {
              strActor = strActorName,
              IMDBActorID = strActorImdbId
            };

            _connection.actors.AddObject(obj);
            _connection.SaveChanges();


            var query2 = (from u in _connection.actors
                          where u.strActor == strActorName && u.IMDBActorID == strActorImdbId
                          select u).FirstOrDefault();

            return query2.idActor;
          }
        }
        else
        {
          return lActorId;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddActor exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
      return -1;
    }

    public void GetActors(ArrayList actors)
    {
      if (_connection == null)
      {
        return;
      }
      try
      {
        actors.Clear();
 
        var query = (from sql in _connection.actors
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          actors.Add(query[iRow].idActor + "|" +
                     query[iRow].strActor + "|" +
                     query[iRow].IMDBActorID);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetActors exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public string GetActorNameById(int actorId)
    {
      if (_connection == null)
      {
        return string.Empty;
      }

      string strActor = string.Empty;

      try
      {
        var query = (from sql in _connection.actors
                     where sql.idActor == actorId
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return string.Empty;
        }

        strActor = query.strActor;
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetActorNameById exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return strActor;
    }

    public void GetActorByName(string strActorName, ArrayList actors)
    {
      //strActorName = RemoveInvalidChars(strActorName);
      if (_connection == null)
      {
        return;
      }

      try
      {
        actors.Clear();

        var query = (from sql in _connection.actors
                     where sql.strActor == strActorName //|| sql.strActor.Contains(strActorName) // regeszter like?
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          actors.Add(query[iRow].idActor + "|" +
                     query[iRow].strActor + "|" +
                     query[iRow].IMDBActorID);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetActorByName exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    private int CheckActorByName(string strActorName)
    {
      if (_connection == null)
      {
        return -1;
      }

      try
      {
        var query = (from sql in _connection.actors
                     where sql.strActor == strActorName //|| sql.strActor.Contains(strActorName) //regeszter like?
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return -1;
        }

        return query.idActor;
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:CheckActorByName exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }

      return -1;
    }

    // Changed-New added actors by movie ID
    public void GetActorsByMovieID(int idMovie, ref ArrayList actorsByMovieID)
    {
      try
      {
        actorsByMovieID.Clear();

        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.actors
                     join s1 in _connection.actorlinkmovies on sql.idActor equals s1.idActor
                     where s1.idMovie == idMovie
                     select new { sql.idActor, sql.strActor, sql.IMDBActorID, s1.strRole }).ToList();

        if (query.Count != 0)
        {
          for (int i = 0; i < query.Count; ++i)
          {
            actorsByMovieID.Add(query[i].idActor + "|" +
                                query[i].strActor + "|" +
                                query[i].IMDBActorID + "|" +
                                query[i].strRole);
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetActorsByMovieID exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void AddActorToMovie(int lMovieId, int lActorId, string role)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        RemoveInvalidChars(ref role);

        var query = (from sql in _connection.actorlinkmovies
                     where sql.idActor == lActorId && sql.idMovie == lMovieId
                     select sql).FirstOrDefault();

        if (query == null)
        {
          // doesnt exists, add it
        
          Databases.actorlinkmovie obj = new Databases.actorlinkmovie()
          {
            idActor = lActorId,
            idMovie = lMovieId,
            strRole = role
          };
          _connection.actorlinkmovies.AddObject(obj);
          _connection.SaveChanges();

        }
        else
        {
          // exists, update it (role only)
          query.strRole = role;
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddActorToMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void DeleteActorFromMovie(int movieId, int actorId)
    {
      try
      {
        var query = (from sql in _connection.actorlinkmovies
                     where sql.idActor == actorId && sql.idMovie == movieId
                     select sql).ToList();

        foreach (var q in query)
        {
          _connection.DeleteObject(q);
        }
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:DeleteActorFromMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void DeleteActor(string actorImdbId)
    {
      try
      {
        string actorFiltered = actorImdbId;
        RemoveInvalidChars(ref actorFiltered);

        var query = (from sql in _connection.actors
                     where sql.IMDBActorID == actorFiltered
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return;
        }

        int idactor = query.idActor;
        
        var query2 = (from sql in _connection.actorlinkmovies
                      where sql.idActor == idactor
                      select sql).ToList();

        foreach (var q in query2)
        {
          _connection.DeleteObject(query2);
        }
        _connection.SaveChanges();

        var query3 = (from sql in _connection.actors
                      where sql.idActor == idactor
                     select sql).FirstOrDefault();

        _connection.DeleteObject(query3);
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:DeleteActor exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void RemoveActorsForMovie(int lMovieId)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var delObj = (from u in _connection.actorlinkmovies
                      where u.idMovie == lMovieId
                      select u).FirstOrDefault();

        if (delObj != null)
        {
          _connection.DeleteObject(delObj);
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:RemoveActorsForMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public string GetRoleByMovieAndActorId(int lMovieId, int lActorId)
    {
      try
      {
        if (_connection == null)
        {
          return string.Empty;
        }

        var query = (from sql in _connection.actorlinkmovies
                    where sql.idMovie == lMovieId && sql.idActor == lActorId
                    select sql).FirstOrDefault();

        if (query != null)
        {
          return query.strRole;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetRoleByMovieAndActorId exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return string.Empty;
    }

    #endregion

    #region bookmarks

    public void ClearBookMarksOfMovie(string strFilenameAndPath)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        int lPathId, lMovieId;
        int lFileId = GetFile(strFilenameAndPath, out lPathId, out lMovieId, true);

        if (lFileId < 0)
        {
          return;
        }

        var query = (from sql in _connection.bookmarks
                     where sql.idFile == lFileId
                     select sql).ToList();

        foreach (var d in query)
        {
          _connection.DeleteObject(d);
        }
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:ClearBookMarksOfMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void AddBookMarkToMovie(string strFilenameAndPath, float fTime)
    {
      if (_connection == null)
      {
        return;
      }
      
      try
      {
        int lPathId, lMovieId;
        int lFileId = GetFile(strFilenameAndPath, out lPathId, out lMovieId, true);

        if (lFileId < 0)
        {
          return;
        }

        string Stime = Convert.ToString(fTime);
        var query = (from sql in _connection.bookmarks
                     where sql.idFile == lFileId && sql.fPercentage == Stime
                     select sql).FirstOrDefault();

        if (query != null)
        {
          return;
        }

        Databases.bookmark obj = new Databases.bookmark()
        {
          idFile = lFileId,
          fPercentage = Stime
        };

        _connection.bookmarks.AddObject(obj);
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddBookMarkToMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void GetBookMarksForMovie(string strFilenameAndPath, ref ArrayList bookmarks)
    {
      bookmarks.Clear();

      try
      {
        int lPathId, lMovieId;
        int lFileId = GetFile(strFilenameAndPath, out lPathId, out lMovieId, true);

        if (lFileId < 0)
        {
          return;
        }

        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.bookmarks
                     where sql.idFile == lFileId
                     orderby sql.fPercentage
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          double fTime = Convert.ToDouble(query[iRow].fPercentage);
          bookmarks.Add(fTime);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetBookMarksForMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    #endregion

    #region MovieInfo

    public void SetMovieInfo(string strFilenameAndPath, ref IMDBMovie details)
    {
      if (strFilenameAndPath.Length == 0)
      {
        return;
      }

      int lMovieId = GetMovie(strFilenameAndPath, true);

      if (lMovieId < 0)
      {
        return;
      }

      details.ID = lMovieId;
      SetMovieInfoById(lMovieId, ref details);
      string strPath, strFileName;
      DatabaseUtility.Split(strFilenameAndPath, out strPath, out strFileName);
      details.Path = strPath;
      details.File = strFileName;
    }

    public void SetMovieInfoById(int lMovieId, ref IMDBMovie details)
    {
      SetMovieInfoById(lMovieId, ref details, false);
    }

    public void SetMovieInfoById(int lMovieId, ref IMDBMovie details, bool updateTimeStamp)
    {
      try
      {
        details.ID = lMovieId;

        IMDBMovie details1 = details;
        IMDBMovie existingDetails = new IMDBMovie();
        VideoDatabase.GetMovieInfoById(details1.ID, ref existingDetails);
        // Cast
        string strLine = details1.Cast;
        RemoveInvalidChars(ref strLine);
        details1.Cast = strLine;
        // Director
        strLine = details1.Director;
        RemoveInvalidChars(ref strLine);
        details1.Director = strLine;
        // add director Id
        int lDirector = -1;

        if (details1.DirectorID < 1 && !string.IsNullOrEmpty(details1.Director))
        {
          lDirector = AddActor("", details1.Director);
          AddActorToMovie(details1.ID, lDirector, GUILocalizeStrings.Get(199).Replace(":", string.Empty));

          if (!CheckMovieImdbId(details1.IMDBNumber))
          {
            // Add actors from cast
            ArrayList vecActors = new ArrayList();
            ArrayList vecRoles = new ArrayList();

            if (details1.Cast != Strings.Unknown)
            {
              string castFix = details1.Cast.Replace("''", "'");
              char[] splitter = { '\n', ',' };
              string[] actors = castFix.Split(splitter);

              for (int i = 0; i < actors.Length; ++i)
              {
                int pos = actors[i].IndexOf(" as ");
                string actor = actors[i];
                string role = string.Empty;

                if (pos >= 0)
                {
                  if (actor.Length >= pos + 4)
                  {
                    role = actor.Substring(pos + 4);
                  }
                  actor = actors[i].Substring(0, pos);
                }

                actor = actor.Trim();
                role = role.Trim();
                int lActorId = AddActor(string.Empty, actor);
                vecActors.Add(lActorId);
                vecRoles.Add(role);
              }
            }

            for (int i = 0; i < vecActors.Count; i++)
            {
              AddActorToMovie(lMovieId, (int)vecActors[i], (string)vecRoles[i]);
            }
          }
        }
        else
        {
          lDirector = details1.DirectorID;
        }
        // Plot
        strLine = details1.Plot;
        RemoveInvalidChars(ref strLine);
        details1.Plot = strLine;
        // User Review
        strLine = details1.UserReview;
        RemoveInvalidChars(ref strLine);
        details1.UserReview = strLine;
        // Plot outline
        strLine = details1.PlotOutline;
        RemoveInvalidChars(ref strLine);
        details1.PlotOutline = strLine;
        // Tagline
        strLine = details1.TagLine;
        RemoveInvalidChars(ref strLine);
        details1.TagLine = strLine;
        // Cover
        strLine = details1.ThumbURL;
        RemoveInvalidChars(ref strLine);
        details1.ThumbURL = strLine;
        // Fanart
        strLine = details1.FanartURL;
        RemoveInvalidChars(ref strLine);
        details1.FanartURL = strLine;
        // Date Added
        details1.DateAdded = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        // Date Watched
        if (details1.DateWatched == string.Empty && existingDetails.ID >= 0)
        {
          details1.DateWatched = existingDetails.DateWatched;
        }
        if (string.IsNullOrEmpty(details1.DateWatched))
        {
          details1.DateWatched = "1999-01-01 00:00:00";
        }
        // Watched status
        if (details1.Watched < 1 && existingDetails.ID >= 0)
        {
          details1.Watched = existingDetails.Watched;
        }
        // Search string
        strLine = details1.SearchString;
        RemoveInvalidChars(ref strLine);
        details1.SearchString = strLine;
        // Title
        strLine = details1.Title.Trim();
        RemoveInvalidChars(ref strLine);
        details1.Title = strLine;
        // SortTtitle
        strLine = details1.SortTitle.Trim();

        if (!string.IsNullOrEmpty(strLine))
        {
          RemoveInvalidChars(ref strLine);
          details1.SortTitle = strLine;
        }
        else
        {
          details1.SortTitle = details1.Title;
        }
        // Votes
        strLine = details1.Votes;
        RemoveInvalidChars(ref strLine);
        details1.Votes = strLine;
        // Writers
        strLine = details1.WritingCredits;
        RemoveInvalidChars(ref strLine);
        details1.WritingCredits = strLine;
        // Genres
        //Clear old genres link for movie
        //RemoveGenresForMovie(lMovieId);
        strLine = details1.Genre;
        RemoveInvalidChars(ref strLine);
        details1.Genre = strLine;
        // IMDB Movie ID
        strLine = details1.IMDBNumber;
        RemoveInvalidChars(ref strLine);
        details1.IMDBNumber = strLine;
        // MPAA Rating
        strLine = details1.MPARating;
        RemoveInvalidChars(ref strLine);
        details1.MPARating = strLine;
        // Studios
        strLine = details1.Studios;
        RemoveInvalidChars(ref strLine);
        details1.Studios = strLine;
        // Country
        strLine = details1.Country;
        RemoveInvalidChars(ref strLine);
        details1.Country = strLine;
        // Language
        strLine = details1.Language;
        RemoveInvalidChars(ref strLine);
        details1.Language = strLine;
        // Last update
        if (updateTimeStamp)
        {
          details1.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        else
        {
          details1.LastUpdate = existingDetails.LastUpdate;
        }
        // add all genres
        string szGenres = details.Genre;
        ArrayList vecGenres = new ArrayList();

        if (szGenres != Strings.Unknown)
        {
          if (szGenres.IndexOf("/") >= 0 || szGenres.IndexOf("|") >= 0)
          {
            Tokens f = new Tokens(szGenres, new[] { '/', '|' });
            foreach (string strGenre in f)
            {
              if (!string.IsNullOrEmpty(strGenre.Trim()))
              {
                int lGenreId = AddGenre(strGenre.Trim());
                AddGenreToMovie(lMovieId, lGenreId);
                //vecGenres.Add(lGenreId);
              }
            }
          }
          else
          {
            string strGenre = details.Genre;
            strGenre = strGenre.Trim();
            int lGenreId = AddGenre(strGenre);
            AddGenreToMovie(lMovieId, lGenreId);
//            vecGenres.Add(lGenreId);
          }
        }

//        for (int i = 0; i < vecGenres.Count; ++i)
//        {
//          AddGenreToMovie(lMovieId, (int)vecGenres[i]);
//        }

        string strRating = String.Format("{0}", details1.Rating);

        if (strRating == "")
        {
          strRating = "0.0";
        }

        var query = (from sql in _connection.movieinfoes
                     where sql.idMovie == lMovieId
                     select sql).FirstOrDefault();

        if (query == null)
        {
          // Insert new movie info - no date watched update

          Databases.movieinfo obj = new Databases.movieinfo()
          {
            idMovie = lMovieId, 
            idDirector = lDirector, 
            strPlotOutline = details1.PlotOutline,
            strPlot = details1.Plot, 
            strTagLine = details1.TagLine,
            strVotes = details1.Votes, 
            fRating = strRating,
            strCast = details1.Cast, 
            strCredits = details1.WritingCredits,
            iYear = details1.Year, 
            strGenre = details1.Genre,
            strPictureURL = details1.ThumbURL, 
            strTitle = details1.Title,
            IMDBID = details1.IMDBNumber, 
            mpaa = details1.MPARating,
            runtime = details1.RunTime, 
            iswatched = details1.Watched,
            strUserReview = details1.UserReview, 
            strFanartURL = details1.FanartURL,
            strDirector = details1.Director, 
            dateAdded = Convert.ToDateTime(details1.DateAdded),
            dateWatched = Convert.ToDateTime("1999-01-01 00:00:00"),
            studios = details1.Studios, 
            country = details1.Country,
            language = details1.Language, 
            lastupdate = Convert.ToDateTime(details1.LastUpdate), 
            strSortTitle = details1.SortTitle
          };

          _connection.movieinfoes.AddObject(obj);
          _connection.SaveChanges();
          
          // Update latest movies
          SetLatestMovieProperties();
        }
        else
        {
          // Update movie info (no dateAdded update)
             
          query.idDirector = lDirector;
          query.strPlotOutline = details1.PlotOutline;
          query.strPlot = details1.Plot;
          query.strTagLine = details1.TagLine;
          query.strVotes = details1.Votes;
          query.fRating = strRating;
          query.strCast = details1.Cast;
          query.strCredits = details1.WritingCredits;
          query.iYear = details1.Year;
          query.strGenre = details1.Genre;
          query.strPictureURL = details1.ThumbURL;
          query.strTitle = details1.Title;
          query.IMDBID = details1.IMDBNumber;
          query.mpaa = details1.MPARating;
          query.runtime = details1.RunTime;
          query.iswatched = details1.Watched;
          query.strUserReview = details1.UserReview;
          query.strFanartURL = details1.FanartURL;
          query.strDirector = details1.Director;
          query.dateWatched = Convert.ToDateTime(details1.DateWatched);
          query.studios = details1.Studios;
          query.country = details1.Country;
          query.language = details1.Language;
          query.lastupdate = Convert.ToDateTime(details1.LastUpdate);
          query.strSortTitle = details1.SortTitle;

          _connection.SaveChanges();
        }

        VideoDatabase.GetMovieInfoById(details1.ID, ref details1);
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetMovieInfoById exception err:{0} src:{2}, stack:{1}, {3}", ex.Message, ex.Source, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
    }

    public void DeleteMovieInfo(string strFileNameAndPath)
    {
      int lMovieId = GetMovie(strFileNameAndPath, false);

      if (lMovieId < 0)
      {
        return;
      }

      DeleteMovieInfoById(lMovieId);
    }

    public void DeleteMovieInfoById(long lMovieId)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        if (lMovieId == -1)
        {
          return;
        }

        Log.Info("Removing movie:{0}", lMovieId);
        // Delete movie file stop time data
        ArrayList files = new ArrayList();
        VideoDatabase.GetFilesForMovie((int)lMovieId, ref files);

        foreach (string file in files)
        {
          int fileId = VideoDatabase.GetFileId(file);
          VideoDatabase.DeleteMovieStopTime(fileId);
        }

        IMDBMovie movie = new IMDBMovie();
        GetMovieInfoById((int)lMovieId, ref movie);

        // Delete movie cover
        FanArt.DeleteCovers(movie.IMDBNumber,movie.Title, (int)lMovieId);
        // Delete movie fanart
        FanArt.DeleteFanarts((int)lMovieId);
        // Delete user groups for movie
        RemoveUserGroupsForMovie((int)lMovieId);

        var query = (from sql in _connection.genrelinkmovies
                     where sql.idMovie == lMovieId
                     select sql).ToList();
        foreach (var q in query)
        {
          _connection.DeleteObject(q);
        }
        _connection.SaveChanges();

        var query2 = (from sql in _connection.actorlinkmovies
                     where sql.idMovie == lMovieId
                     select sql).ToList();
        foreach (var q in query2)
        {
          _connection.DeleteObject(q);
        }
        _connection.SaveChanges();

        var query3 = (from sql in _connection.movieinfoes
                      where sql.idMovie == lMovieId
                      select sql).ToList();
        foreach (var q in query3)
        {
          _connection.DeleteObject(q);
        }
        _connection.SaveChanges();

        var query4 = (from sql in _connection.files
                      where sql.idMovie == lMovieId
                      select sql).ToList();
        foreach (var q in query4)
        {
          _connection.DeleteObject(q);
        }
        _connection.SaveChanges();

        var query5 = (from sql in _connection.movies
                      where sql.idMovie == lMovieId
                      select sql).ToList();
        foreach (var q in query5)
        {
          _connection.DeleteObject(q);
        }
        _connection.SaveChanges();

        // Update latest movies
        SetLatestMovieProperties();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:DeleteMovieInfoById exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public bool HasMovieInfo(string strFilenameAndPath)
    {
      if (string.IsNullOrEmpty(strFilenameAndPath))
      {
        return false;
      }
      try
      {
        if (_connection == null)
        {
          return false;
        }

        int lMovieId = GetMovie(strFilenameAndPath, false);

        if (lMovieId < 0)
        {
          return false;
        }

        var query = (from sql in _connection.movieinfoes
                     where sql.idMovie == lMovieId
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return false;
        }

        return true;
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:HasMovieInfo exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
      return false;
    }

    public int GetMovieInfo(string strFilenameAndPath, ref IMDBMovie details)
    {
      int lMovieId = GetMovie(strFilenameAndPath, false);

      if (lMovieId < 0)
      {
        return -1;
      }

      if (!HasMovieInfo(strFilenameAndPath))
      {
        return -1;
      }

      GetMovieInfoById(lMovieId, ref details);
      return lMovieId;
    }

    // Changed Added DirectorID, userrev, fanart
    public void GetMovieInfoById(int lMovieId, ref IMDBMovie details)
    {
      try
      {
        List<Databases.movieinfo> query = (from sql in _connection.movieinfoes
                     join s1 in _connection.movies on sql.idMovie equals s1.idMovie
                     join s2 in _connection.paths on s1.idPath equals s2.idPath
                     where sql.idMovie == lMovieId
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }
        SetMovieDetails(ref details, 0, query);
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieInfoById exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void SetWatched(IMDBMovie details)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        if (details.ID < 0)
        {
          return;
        }

        var query = (from sql in _connection.movieinfoes
                     where sql.idMovie == details.ID
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return;
        }
        query.iswatched = details.Watched;
        query.dateWatched = DateTime.Now;
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetWatched exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
    }

    public void SetDateWatched(IMDBMovie details)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        if (details.ID < 0)
        {
          return;
        }

        if (string.IsNullOrEmpty(details.DateWatched))
        {
          details.DateWatched = "1999-01-01 00:00:00";
        }

        var query = (from sql in _connection.movieinfoes
                     where sql.idMovie == details.ID
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return;
        }

        query.dateWatched = Convert.ToDateTime(details.DateWatched);
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetDateWatched exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }
    #endregion

    #region Movie Resume

    public void DeleteMovieStopTime(int iFileId)
    {
      if (null == _connection)
      {
        return;
      }
      try
      {
        var query = (from sql in _connection.resumes
                     where sql.idFile == iFileId
                     select sql).FirstOrDefault();
        if (query != null)
        {
          _connection.DeleteObject(query);
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:DeleteMovieStopTime exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public int GetMovieStopTime(int iFileId)
    {
      try
      {
        var query = (from sql in _connection.resumes
                     where sql.idFile == iFileId
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return 0;
        }

        return (int)query.stoptime;
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieStopTime exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return 0;
    }

    public void SetMovieStopTime(int iFileId, int stoptime)
    {
      try
      {
        var query = (from sql in _connection.resumes
                     where sql.idFile == iFileId
                     select sql).FirstOrDefault();

        if (query == null)
        {
          Databases.resume obj = new Databases.resume()
          {
            idFile = iFileId,
            stoptime = stoptime
          };
          _connection.resumes.AddObject(obj);
        }
        else
        {
          query.stoptime = stoptime;
        }

        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetMovieStopTime exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    private byte[] FromHexString(string s)
    {
      byte[] bytes = new byte[s.Length / 2];

      for (int i = 0; i < bytes.Length; i++)
      {
        bytes[i] = byte.Parse(s.Substring(i * 2, 2), NumberStyles.HexNumber);
      }

      return bytes;
    }

    private string ToHexString(byte[] bytes)
    {
      char[] hexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
      char[] chars = new char[bytes.Length * 2];

      for (int i = 0; i < bytes.Length; i++)
      {
        int b = bytes[i];
        chars[i * 2] = hexDigits[b >> 4];
        chars[i * 2 + 1] = hexDigits[b & 0xF];
      }

      return new string(chars);
    }

    /// <summary>
    /// Deprecated Method (this one will not use the new Blu-ray Title mode resume)
    /// </summary>
    public int GetMovieStopTimeAndResumeData(int iFileId, out byte[] resumeData)
    {
      return GetMovieStopTimeAndResumeData(iFileId, out resumeData, g_Player.BdDefaultTitle);
    }

    public int GetMovieStopTimeAndResumeData(int iFileId, out byte[] resumeData, int bdtitle)
    {
      resumeData = null;

      try
      {
        var query = (from sql in _connection.resumes
                     where sql.idFile == iFileId && sql.bdtitle == bdtitle
                     select sql).FirstOrDefault();
        int stoptime;
        int BDTileID;

        if (query != null)
        {
          stoptime = (int)query.stoptime;
          string resumeString;
          if (query.resumeData != null)
          {
            resumeString = query.resumeData.ToString();
            resumeData = new byte[resumeString.Length / 2];
            FromHexString(resumeString).CopyTo(resumeData, 0);
          }
          else
          {
            resumeData = new byte[2];
            FromHexString("").CopyTo(resumeData, 0);
          }


          return stoptime;
        }
        else
        {
          return 0;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieStopTimeAndResumeData exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return 0;
    }

    /// <summary>
    /// Deprecated Method (this one will not use the new Blu-ray Title mode resume)
    /// </summary>
    public void SetMovieStopTimeAndResumeData(int iFileId, int stoptime, byte[] resumeData)
    {
      SetMovieStopTimeAndResumeData(iFileId, stoptime, resumeData, g_Player.BdDefaultTitle);
    }

    public void SetMovieStopTimeAndResumeData(int iFileId, int stoptime, byte[] resumeData, int bdtitle)
    {
      try
      {
        // The next line is too enable record of stoptime for each Title BD
        //string sql = String.Format("SELECT * FROM resume WHERE idFile={0} AND bdtitle={1}", iFileId, bdtitle);

        // Only store stoptime with one current Title BD
        var query = (from sql in _connection.resumes
                     where sql.idFile == iFileId
                     select sql).FirstOrDefault();
        string resumeString = "-";

        if (resumeData != null)
        {
          resumeString = ToHexString(resumeData);
        }


        // Only store stoptime with one current Title BD
        if (query != null)
        {
          query.stoptime = stoptime;
          
          query.resumeData = resumeData;

          query.bdtitle = bdtitle;
        }
        else if (bdtitle >= 0)
        {
          Databases.resume obj = new Databases.resume()
          {
            idFile = iFileId,
            stoptime = stoptime,
            resumeData = resumeData,
            bdtitle = bdtitle
          };
          _connection.resumes.AddObject(obj);
        }
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetMovieStopTimeAndResumeData exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
    }

    public int GetMovieDuration(int iMovieId)
    {
      if (null == _connection)
      {
        return 0;
      }
      
      try
      {
        int duration = 0;

        var query = (from sql in _connection.movies
                     where sql.idMovie == iMovieId
                     select sql).FirstOrDefault();

        if (query == null || query.iduration == null)
        {
          return duration;
        }

        return (int)query.iduration;
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieDuration exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return 0;
    }

    public int GetVideoDuration(int iFileId)
    {
      try
      {
        var query = (from sql in _connection.durations
                     where sql.idFile == iFileId
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return 0;
        }

        return (int)query.duration1;
      }
      catch (ThreadAbortException)
      {
        // Will be logged in main thread code  
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetVideoDuration exception: err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return 0;
    }

    public void SetVideoDuration(int iFileId, int duration)
    {
      try
      {
        if (duration > 0)
        {
          var query = (from sql in _connection.durations
                       where sql.idFile == iFileId
                       select sql).FirstOrDefault();

          if (query == null)
          {
            Databases.duration obj = new Databases.duration()
            {
              idFile = iFileId,
              duration1 = duration
            };
            _connection.durations.AddObject(obj);
          }
          else
          {
            query.duration1 = duration;
          }
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetVideoDuration exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void SetMovieDuration(int iMovieId, int duration)
    {
      try
      {
        if (duration > 0)
        {
          var query = (from sql in _connection.movies
                       where sql.idMovie == iMovieId
                       select sql).FirstOrDefault();

          query.iduration = duration;
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetMovieDuration exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void SetMovieTitle(string movieTitle, int movieId)
    {
      try
      {
        var query = (from sql in _connection.movieinfoes
                     where sql.idMovie == movieId
                     select sql).FirstOrDefault();

        query.strTitle = movieTitle;
        _connection.SaveChanges();
    }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetMovieTitle exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void SetMovieShortTitle(string movieTitle, int movieId)
    {
      try
      {
        var query = (from sql in _connection.movieinfoes
                     where sql.idMovie == movieId
                     select sql).FirstOrDefault();

        query.strSortTitle = movieTitle;
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetMovieShortTitle exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void SetIMDBMovies(string sql)
    {
      try
      {
        _connection.ExecuteStoreCommand(sql);

        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetIMDBMovies exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void SetIMDBActorId(int actorId, string IMDBActorID)
    {
      try
      {
        var query = (from sql in _connection.actors
                     where sql.idActor == actorId
                     select sql).FirstOrDefault();

        if (query != null)
        {
          query.IMDBActorID = IMDBActorID;
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetIMDBActorId exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void SetMovieWatchedStatus(int idMovie, bool watched, int percent)
    {
      try
      {
        var query = (from sql in _connection.movies
                     where sql.idMovie == idMovie
                     select sql).FirstOrDefault();

        if (query != null)
        {
          query.watched = watched;
          query.iwatchedPercent = percent;
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetMovieWatchedStatus exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
    }

    /// <summary>
    /// Increase times watched by 1
    /// </summary>
    /// <param name="idMovie"></param>
    public void MovieWatchedCountIncrease(int idMovie)
    {
      try
      {
        var query = (from sql in _connection.movies
                     where sql.idMovie == idMovie
                     select sql).FirstOrDefault();

        if (query != null)
        {
          int watchedCount = 0;

          if (query.timeswatched != null)
          {
            watchedCount = (int)query.timeswatched;
          }

          watchedCount++;
          query.timeswatched = watchedCount;
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:MovieWatchedCountIncrease exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void SetMovieWatchedCount(int movieId, int watchedCount)
    {
      try
      {
        var query = (from sql in _connection.movies
                     where sql.idMovie == movieId
                     select sql).FirstOrDefault();

        if (query != null)
        {
          query.timeswatched = watchedCount;
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetMovieWatchedCount exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public bool GetMovieWatchedStatus(int idMovie, out int percent, out int timesWatched)
    {
      percent = 0;
      timesWatched = 0;

      try
      {
        var query = (from sql in _connection.movies
                     where sql.idMovie == idMovie
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return false;
        }

        if (query.iwatchedPercent != null)
        {
          percent = (int)query.iwatchedPercent;
        }
        if (query.timeswatched != null)
        {
          timesWatched = (int)query.timeswatched;
        }

        bool iWatched = false;

        if (query.watched != null)
        {
          iWatched = (bool)query.watched;
        }

        return iWatched;
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieWatchedStatus exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return false;
    }

    #endregion

    #region Movie

    public void DeleteMovie(string strFilenameAndPath)
    {
      if (Directory.Exists(strFilenameAndPath))
        DeleteMoviesInFolder(strFilenameAndPath);
      else
        DeleteSingleMovie(strFilenameAndPath);
    }

    private void DeleteMoviesInFolder(string strPath)
    {
      var query = (from sql in _connection.paths 
                   where sql.strPath.StartsWith(strPath)
                   select sql).ToList();

      SortedDictionary<int, string> pathList = new SortedDictionary<int, string>();

      for (int i = 0; i < query.Count; ++i)
      {
        pathList.Add(query[i].idPath, query[i].strPath);
      }

      foreach (KeyValuePair<int, string> kvp in pathList)
      {
        var query2 = (from sql in _connection.files
                     where sql.idPath == kvp.Key
                     select sql).ToList();

        for (int j = 0; j < query2.Count; ++j)
        {
          DeleteSingleMovie(kvp.Value + query2[j].strFilename);
        }
      }
    }

    private void DeleteSingleMovie(string strFilenameAndPath)
    {
      try
      {
        int lPathId;
        int lMovieId;
        if (_connection == null)
        {
          return;
        }
        if (GetFile(strFilenameAndPath, out lPathId, out lMovieId, false) < 0)
        {
          return;
        }

        ClearBookMarksOfMovie(strFilenameAndPath);

        // Delete files attached to the movie

        var query = (from sql in _connection.files 
                     join s1 in _connection.paths on sql.idPath equals s1.idPath
                     where sql.idMovie == lMovieId 
                     orderby sql.strFilename
                     select sql).ToList();

        for (int i = 0; i < query.Count; ++i)
        {
          int iFileId = query[i].idFile;
          DeleteFile(iFileId);
        }

        // Delete covers
        IMDBMovie movieDetails = new IMDBMovie();
        GetMovieInfoById(lMovieId, ref movieDetails);
        FanArt.DeleteCovers(movieDetails.IMDBNumber,movieDetails.Title, movieDetails.ID);
        // Delete fanarts
        FanArt.DeleteFanarts(movieDetails.ID);
        // Delete user groups
        RemoveUserGroupsForMovie(movieDetails.ID);
        //

        var query2 = (from sql in _connection.genrelinkmovies
                     where sql.idMovie == lMovieId
                     select sql).ToList();

        foreach(var q in query2)
        {
          _connection.DeleteObject(q);
        }

        var query3 = (from sql in _connection.actorlinkmovies
                      where sql.idMovie == lMovieId
                      select sql).ToList();

        foreach (var q in query3)
        {
          _connection.DeleteObject(q);
        }

        var query4 = (from sql in _connection.movieinfoes
                      where sql.idMovie == lMovieId
                      select sql).ToList();

        foreach (var q in query4)
        {
          _connection.DeleteObject(q);
        }

        var query5 = (from sql in _connection.movies
                      where sql.idMovie == lMovieId
                      select sql).ToList();

        foreach (var q in query5)
        {
          _connection.DeleteObject(q);
        }

        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:DeleteSingleMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public int AddMovie(string strFilenameAndPath, bool bHassubtitles)
    {
      if (_connection == null)
      {
        return -1;
      }
      try
      {
        string strPath, strFileName;

        DatabaseUtility.Split(strFilenameAndPath, out strPath, out strFileName);
        RemoveInvalidChars(ref strPath);
        RemoveInvalidChars(ref strFileName);

        int lMovieId = GetMovie(strFilenameAndPath, false);
        if (lMovieId < 0)
        {
          int lPathId = AddPath(strPath);

          if (lPathId < 0)
          {
            return -1;
          }
          int iHasSubs = 0;
          if (bHassubtitles)
          {
            iHasSubs = 1;
          }

          Databases.movie obj = new Databases.movie()
          {
            idPath = lPathId,
            hasSubtitles = iHasSubs,
            discid = string.Empty
          };

          _connection.movies.AddObject(obj);
          _connection.SaveChanges();

          var query2 = (from u in _connection.movies
                        where u.idMovie == (_connection.movies.Select(u1 => u1.idMovie).Max())
                        select u).FirstOrDefault();

          lMovieId = query2.idMovie;

          AddFile(lMovieId, lPathId, strFileName);
        }
        else
        {
          int lPathId = GetPath(strPath);
          if (lPathId < 0)
          {
            return -1;
          }

          AddFile(lMovieId, lPathId, strFileName);
        }
        return lMovieId;
      }
      catch (Exception ex)
      {
        Log.Error("videodatabase exceptionADO:AddMovie err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
      return -1;
    }

    private int GetMovie(string strFilenameAndPath, bool bExact)
    {
      int lPathId;
      int lMovieId;

      if (GetFile(strFilenameAndPath, out lPathId, out lMovieId, bExact) < 0)
      {
        return -1;
      }

      return lMovieId;
    }

    public void GetMovies(ref ArrayList movies)
    {
      try
      {
        movies.Clear();

        if (_connection == null)
        {
          return;
        }

        List<Databases.movieinfo> query = (from sql in _connection.movieinfoes
                     join s1 in _connection.movies on sql.idMovie equals s1.idMovie
                     join s2 in _connection.paths on s1.idPath equals s2.idPath
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          IMDBMovie details = new IMDBMovie();
          SetMovieDetails(ref details, iRow, query);
          movies.Add(details);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovies exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public int GetMovieId(string strFilenameAndPath)
    {
      int lMovieId = GetMovie(strFilenameAndPath, true);
      return lMovieId;
    }

    public bool HasSubtitle(string strFilenameAndPath)
    {
      try
      {
        if (_connection == null)
        {
          return false;
        }
        int lMovieId = GetMovie(strFilenameAndPath, false);
        if (lMovieId < 0)
        {
          return false;
        }

        var query = (from sql in _connection.movies 
                     where sql.idMovie == lMovieId 
                     select sql).FirstOrDefault();
        
        if (query == null)
        {
          return false;
        }
        int lHasSubs = (int)query.hasSubtitles;

        if (lHasSubs != 0)
        {
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:HasSubtitle exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return false;
    }

    public void SetThumbURL(int lMovieId, string thumbURL)
    {
      RemoveInvalidChars(ref thumbURL);
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.movieinfoes 
                     where sql.idMovie == lMovieId 
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return;
        }
        query.strPictureURL = thumbURL;
        _connection.SaveChanges();

      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetThumbURL exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    // Fanart add
    public void SetFanartURL(int lMovieId, string fanartURL)
    {
      RemoveInvalidChars(ref fanartURL);
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.movieinfoes
                     where sql.idMovie == lMovieId
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return;
        }
        query.strFanartURL = fanartURL;
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetFanartURL exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void SetDVDLabel(int lMovieId, string strDVDLabel1)
    {
      string strDVDLabel = strDVDLabel1;
      RemoveInvalidChars(ref strDVDLabel);
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.movies
                     where sql.idMovie == lMovieId
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return;
        }
        query.discid = strDVDLabel1;
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetDVDLabel exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    #endregion

    #region Movie Queries

    public void GetYears(ArrayList years)
    {
      if (_connection == null)
      {
        return;
      }

      try
      {
        years.Clear();

        var query = (from sql in _connection.movieinfoes
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          string strYear = query[iRow].iYear.ToString();
          bool bAdd = true;

          for (int i = 0; i < years.Count; ++i)
          {
            if (strYear == (string)years[i])
            {
              bAdd = false;
            }
          }

          if (bAdd)
          {
            years.Add(strYear);
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetYears exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    // Changed - added user review
    public void GetMoviesByGenre(string strGenre1, ref ArrayList movies)
    {
      try
      {
        string strGenre = strGenre1;
        RemoveInvalidChars(ref strGenre);
        movies.Clear();

        if (_connection == null)
        {
          return;
        }

        List<Databases.movieinfo> query = (from sql in _connection.movieinfoes
                     // join s1 in _connection.movies on sql.idMovie equals s1.idMovie
                     // join s2 in _connection.paths on s1.idPath equals s2.idPath
                     join s3 in _connection.genrelinkmovies on sql.idMovie equals s3.idMovie
                     join s4 in _connection.genres on s3.idGenre equals s4.idGenre
                     where s4.strGenre == strGenre
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          IMDBMovie details = new IMDBMovie();
          SetMovieDetails(ref details, iRow, query);
          movies.Add(details);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMoviesByGenre exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void GetRandomMoviesByGenre(string strGenre1, ref ArrayList movies, int limit)
    {
      try
      {
        string strGenre = strGenre1;
        RemoveInvalidChars(ref strGenre);
        movies.Clear();

        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.movieinfoes
                     join s1 in _connection.movies on sql.idMovie equals s1.idMovie
                     join s2 in _connection.paths on s1.idPath equals s2.idPath
                     join s3 in _connection.genrelinkmovies on s1.idMovie equals s3.idMovie
                     join s4 in _connection.genres on s3.idGenre equals s4.idGenre
                     where s4.strGenre == strGenre
                     select sql).OrderBy(x =>x.idMovie).ToList();

        List<Databases.movieinfo> list = query.Take(limit).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < list.Count; iRow++)
        {
          IMDBMovie details = new IMDBMovie();
          SetMovieDetails(ref details, iRow, list);
          movies.Add(details);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetRandomMoviesByGenre exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
    }

    public string GetMovieTitlesByGenre(string strGenre)
    {
      string titles = string.Empty;

      try
      {
        if (_connection == null)
        {
          return titles;
        }

        var query = (from sql in _connection.movieinfoes
                     join s3 in _connection.genrelinkmovies on sql.idMovie equals s3.idMovie
                     join s4 in _connection.genres on s3.idGenre equals s4.idGenre
                     where s4.strGenre == strGenre
                     orderby sql.strTitle ascending
                     select sql).ToList();

        if (query.Count == 0)
        {
          return titles;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          titles += query[iRow].strTitle + "\n";
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieTitlesByGenre exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return titles;
    }

    public void GetMoviesByUserGroup(string strUserGroup, ref ArrayList movies)
    {
      try
      {
        RemoveInvalidChars(ref strUserGroup);
        movies.Clear();

        if (_connection == null)
        {
          return;
        }

        List<Databases.movieinfo> query = (from sql in _connection.movieinfoes
                     join s1 in _connection.movies on sql.idMovie equals s1.idMovie
                     join s2 in _connection.paths on s1.idPath equals s2.idPath
                     join s3 in _connection.usergrouplinkmovies on s1.idMovie equals s3.idMovie
                     join s4 in _connection.usergroups on s3.idGroup equals s4.idGroup
                     where s4.strGroup == strUserGroup
                     orderby sql.strTitle ascending
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          IMDBMovie details = new IMDBMovie();
          SetMovieDetails(ref details, iRow, query);
          movies.Add(details);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMoviesByUserGroup exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public string GetMovieTitlesByUserGroup(int idGroup)
    {
      string titles = string.Empty;

      try
      {
        if (_connection == null)
        {
          return titles;
        }

        var query = (from sql in _connection.movieinfoes
                     join s1 in _connection.usergrouplinkmovies on sql.idMovie equals s1.idMovie
                     where s1.idGroup == idGroup
                     orderby sql.strTitle ascending
                     select sql).ToList();

        if (query.Count == 0)
        {
          return titles;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          titles += query[iRow].strTitle + "\n";
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieTitlesByUserGroup exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return titles;
    }

    public void GetRandomMoviesByUserGroup(string strUserGroup, ref ArrayList movies, int limit)
    {
      try
      {
        RemoveInvalidChars(ref strUserGroup);
        movies.Clear();

        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.movieinfoes
                     join s1 in _connection.movies on sql.idMovie equals s1.idMovie
                     join s2 in _connection.paths on s1.idPath equals s2.idPath
                     join s3 in _connection.usergrouplinkmovies on s1.idMovie equals s3.idMovie
                     join s4 in _connection.usergroups on s3.idGroup equals s4.idGroup
                     where s4.strGroup == strUserGroup
                     select sql).OrderBy(x => x.idMovie).ToList();

        List<Databases.movieinfo> list = query.Take(limit).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < list.Count; iRow++)
        {
          IMDBMovie details = new IMDBMovie();
          SetMovieDetails(ref details, iRow, list);
          movies.Add(details);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetRandomMoviesByUserGroup exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    // Changed - added user review
    public void GetMoviesByActor(string strActor1, ref ArrayList movies)
    {
      try
      {
        string strActor = strActor1;
        RemoveInvalidChars(ref strActor);
        movies.Clear();

        if (_connection == null)
        {
          return;
        }

        List<Databases.movieinfo> query = (from sql in _connection.movieinfoes
                     join s1 in _connection.movies on sql.idMovie equals s1.idMovie
                     join s2 in _connection.paths on s1.idPath equals s2.idPath
                     join s3 in _connection.actorlinkmovies on s1.idMovie equals s3.idMovie
                     join s4 in _connection.actors on s3.idActor equals s4.idActor
                     where s4.strActor == strActor
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          IMDBMovie details = new IMDBMovie();
          SetMovieDetails(ref details, iRow, query);
          movies.Add(details);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMoviesByActor exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void GetRandomMoviesByActor(string strActor1, ref ArrayList movies, int limit)
    {
      try
      {
        string strActor = strActor1;
        RemoveInvalidChars(ref strActor);
        movies.Clear();

        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.movieinfoes
                     join s1 in _connection.movies on sql.idMovie equals s1.idMovie
                     join s2 in _connection.paths on s1.idPath equals s2.idPath
                     join s3 in _connection.actorlinkmovies on s1.idMovie equals s3.idMovie
                     join s4 in _connection.actors on s3.idActor equals s4.idActor
                     where s4.strActor == strActor
                     select sql).OrderBy(x => x.idMovie).ToList();

        List<Databases.movieinfo> list = query.Take(limit).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < list.Count; iRow++)
        {
          IMDBMovie details = new IMDBMovie();
          SetMovieDetails(ref details, iRow, list);
          movies.Add(details);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetRandomMoviesByActor exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public string GetMovieTitlesByActor(int actorId)
    {
      string titles = string.Empty;

      try
      {
        if (_connection == null)
        {
          return titles;
        }

        var query = (from sql in _connection.movieinfoes
                     join s1 in _connection.actorlinkmovies on sql.idMovie equals s1.idMovie
                     where s1.idActor == actorId
                     orderby sql.strTitle ascending
                     select sql).ToList();

        if (query.Count == 0)
        {
          return titles;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          titles += query[iRow].strTitle + "\n";
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieTitlesByActor exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return titles;
    }

    public string GetMovieTitlesByDirector(int directorId)
    {
      string titles = string.Empty;

      try
      {
        if (_connection == null)
        {
          return titles;
        }

        var query = (from sql in _connection.movieinfoes
                     where sql.idDirector == directorId
                     select sql).Distinct().OrderBy(x => x.strTitle).ToList();

         if (query.Count == 0)
        {
          return titles;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          titles += query[iRow].strTitle + "\n";
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieTitlesByDirector exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return titles;
    }

    // Changed - added user review
    public void GetMoviesByYear(string strYear, ref ArrayList movies)
    {
      try
      {
        int iYear;
        Int32.TryParse(strYear, out iYear);
        movies.Clear();

        if (_connection == null)
        {
          return;
        }

        List<Databases.movieinfo> query = (from sql in _connection.movieinfoes
                     join s1 in _connection.movies on sql.idMovie equals s1.idMovie
                     join s2 in _connection.paths on s1.idPath equals s2.idPath
                     where sql.iYear == iYear
                     orderby sql.strTitle
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          IMDBMovie details = new IMDBMovie();
          SetMovieDetails(ref details, iRow, query);
          movies.Add(details);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMoviesByYear exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void GetRandomMoviesByYear(string strYear, ref ArrayList movies, int limit)
    {
      try
      {
        int iYear;
        Int32.TryParse(strYear, out iYear);
        movies.Clear();

        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.movieinfoes
                     join s1 in _connection.movies on sql.idMovie equals s1.idMovie
                     join s2 in _connection.paths on s1.idPath equals s2.idPath
                     where sql.iYear == iYear
                     select sql).OrderBy(x => x.idMovie).ToList();

        List<Databases.movieinfo> list = query.Take(limit).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < list.Count; iRow++)
        {
          IMDBMovie details = new IMDBMovie();
          SetMovieDetails(ref details, iRow, list);
          movies.Add(details);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetRandomMoviesByYear exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public string GetMovieTitlesByYear(string strYear)
    {
      string titles = string.Empty;

      try
      {
        int iYear;
        Int32.TryParse(strYear, out iYear);

        if (_connection == null)
        {
          return titles;
        }

        var query = (from sql in _connection.movieinfoes
                     where sql.iYear == iYear
                     select sql).Distinct().OrderBy(x => x.strTitle).ToList();

        if (query.Count == 0)
        {
          return titles;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          titles += query[iRow].strTitle + "\n";
        }

      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieTitlesByYear exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return titles;
    }

    public void GetMoviesByPath(string strPath1, ref ArrayList movies)
    {
      try
      {
        string strPath = strPath1;

        if (strPath.Length > 0)
        {
          if (strPath[strPath.Length - 1] == '/' || strPath[strPath.Length - 1] == '\\')
          {
            strPath = strPath.Substring(0, strPath.Length - 1);
          }
        }

        RemoveInvalidChars(ref strPath);
        movies.Clear();

        if (_connection == null)
        {
          return;
        }

        int lPathId = GetPath(strPath);

        if (lPathId < 0)
        {
          return;
        }

        List<Databases.movieinfo> query = (from sql in _connection.movieinfoes
                     join s1 in _connection.files on sql.idMovie equals s1.idMovie
                     where s1.idPath == lPathId
                     select sql).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          IMDBMovie details = new IMDBMovie();
          SetMovieDetails(ref details, iRow, query);
          movies.Add(details);
        }
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMoviesByPath exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void GetRandomMoviesByPath(string strPath1, ref ArrayList movies, int limit)
    {
      try
      {
        string strPath = strPath1;

        if (strPath.Length > 0)
        {
          if (strPath[strPath.Length - 1] == '/' || strPath[strPath.Length - 1] == '\\')
          {
            strPath = strPath.Substring(0, strPath.Length - 1);
          }
        }

        RemoveInvalidChars(ref strPath);
        movies.Clear();

        if (_connection == null)
        {
          return;
        }

        int lPathId = GetPath(strPath);

        if (lPathId < 0)
        {
          return;
        }

        var query = (from sql in _connection.movieinfoes
                     join s1 in _connection.files on sql.idMovie equals s1.idMovie
                     where s1.idPath == lPathId
                     select sql).OrderBy(x => x.idMovie).ToList();

        List<Databases.movieinfo> list = query.Take(limit).ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int iRow = 0; iRow < list.Count; iRow++)
        {
          IMDBMovie details = new IMDBMovie();
          SetMovieDetails(ref details, iRow, list);
          movies.Add(details);
        }
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetRandomMoviesByPath exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
    }

    // Changed - added user review
    public void GetMoviesByFilter(string sql, out ArrayList movies, bool actorTable, bool movieinfoTable,
                                  bool genreTable, bool usergroupTable)
    {
      movies = new ArrayList();
      
      sql = sql.Replace("\\", "\\\\").Trim();
      sql = sql.Replace("RANDOM()", "RAND()").Trim();

      try
      {
        if (_connection == null)
        {
          return;
        }

        IMDBMovie movie;

        if (movieinfoTable)
        {
          int intFrom = sql.IndexOf("from");
          if (intFrom <= 0)
          {
            intFrom = sql.IndexOf("FROM");
          }

          if (intFrom > 0)
          {
            string partStr = sql.Substring(intFrom, sql.Length - intFrom);

            string movieinfoFields = "select movieinfo.idMovie," +
                     "movieinfo.idDirector," +
                     "movieinfo.strDirector," +
                     "movieinfo.strPlotOutline," +
                     "movieinfo.strPlot," +
                     "movieinfo.strTagLine," +
                     "movieinfo.strVotes," +
                     "movieinfo.fRating," +
                     "movieinfo.strCast," +
                     "movieinfo.strCredits," +
                     "movieinfo.iYear," +
                     "movieinfo.strGenre," +
                     "movieinfo.strPictureURL," +
                     "movieinfo.strTitle," +
                     "movieinfo.IMDBID," +
                     "movieinfo.mpaa," +
                     "movieinfo.runtime," +
                     "movieinfo.iswatched," +
                     "movieinfo.strUserReview," +
                     "movieinfo.strFanartURL," +
                     "movieinfo.dateAdded," +
                     "movieinfo.dateWatched," +
                     "movieinfo.studios," +
                     "movieinfo.country," +
                     "movieinfo.language," +
                     "movieinfo.lastupdate, " +
                     "movieinfo.strSortTitle ";

            sql = movieinfoFields + partStr;
          }

          List<Databases.movieinfo> query = _connection.ExecuteStoreQuery<Databases.movieinfo>(sql).ToList();

          if (query.Count == 0)
          {
            return;
          }

          for (int i = 0; i < query.Count; i++)
          {
            movie = new IMDBMovie();
            SetMovieDetails(ref movie, i, query);
            movie.File = movie.VideoFileName;
            movies.Add(movie);
          }
        }
        else
        {
          var query = _connection.ExecuteStoreQuery<custom1>(sql).ToList();

          for (int i = 0; i < query.Count; i++)
          {
            movie = new IMDBMovie();
            if (actorTable && !movieinfoTable)
            {
              movie.Actor = query[i].CNT;
              movie.ActorID = (int)Math.Floor(0.5d + Double.Parse(query[i].IX));
            }

            if (genreTable && !movieinfoTable)
            {
              movie.SingleGenre = query[i].CNT;
              movie.GenreID = (int)Math.Floor(0.5d + Double.Parse(query[i].IX));
            }

            if (usergroupTable && !movieinfoTable)
            {
              movie.SingleUserGroup = query[i].CNT;
              movie.UserGroupID = (int)Math.Floor(0.5d + Double.Parse(query[i].IX));
            }

            movies.Add(movie);
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMoviesByFilter exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void GetIndexByFilter(string sql, bool filterNonWordChar, out ArrayList movieList)
    {
      movieList = new ArrayList();
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = _connection.ExecuteStoreQuery<custom1>(sql).ToList();
  
        bool nonWordCharFound = false;
        int nwCount = 0;

        // Count nowWord items
        if (filterNonWordChar)
        {
          for (int i = 0; i < query.Count; i++)
          {
            if (Regex.Match(query[i].IX, @"\W|\d").Success)
            {
              int iCount = Convert.ToInt32(query[i].CNT);
              nwCount = nwCount + iCount;
            }
          }
        }

        for (int i = 0; i < query.Count; i++)
        {
          IMDBMovie movie = new IMDBMovie();

          string value = query[i].IX;
          int countN = Convert.ToInt32(query[i].CNT);

          if (filterNonWordChar && Regex.Match(query[i].IX, @"\W|\d").Success)
          {
            if (!nonWordCharFound)
            {
              value = "#";
              nonWordCharFound = true;
              countN = nwCount;
            }
            else
            {
              continue;
            }
          }
        
          movie.Title = value;
          movie.RunTime = countN;
          movieList.Add(movie);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetIndexByFilter exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void GetMoviesByYear(ref ArrayList movies)
    {
      try
      {
        movies = new ArrayList();

        var query = (from sql in _connection.movieinfoes
                     select sql).Distinct().ToList();

        if (query.Count == 0)
        {
          return;
        }

        for (int i = 0; i < query.Count; i++)
        {
          IMDBMovie movie = new IMDBMovie();
          movie.Year = (int)Math.Floor(0.5d + Double.Parse(query[i].iYear.ToString()));
          movies.Add(movie);
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMoviesByYear exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public string GetMovieTitlesByIndex(string sql)
    {
      string titles = string.Empty;

      try
      {
        if (_connection == null)
        {
          return titles;
        }
        sql = sql.Replace("^", "").Trim();
        sql = sql.Replace("\\", "\\\\").Trim();

        List<string> query = _connection.ExecuteStoreQuery<string>(sql).ToList();

        for (int i = 0; i < query.Count; i++)
        {
          string value = query[i];
          titles += value + "\n";
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetMovieTitlesByIndex exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }

      return titles;
    }

    public void UpdateCDLabel(IMDBMovie movieDetails, string CDlabel)
    {
      if (movieDetails == null)
      {
        return;
      }

      try
      {
        var query = (from sql in _connection.paths 
                     where sql.cdlabel == movieDetails.CDLabel 
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return;
        }

        query.cdlabel = CDlabel;
        _connection.SaveChanges();

      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:UpdateCDLabel exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public string GetDVDLabel(string strFile)
    {
      string cdlabel = string.Empty;

      if (Util.Utils.IsDVD(strFile))
      {
        cdlabel = Util.Utils.GetDriveSerial(strFile);
      }

      return cdlabel;
    }

    public SQLiteResultSet GetResults(string sql)
    {
      try
      {
        if (_connection == null)
        {
          return null;
        }
        Log.Error("videodatabaseADO:GetResults {0}",sql);
        SQLiteResultSet results = null;
        return results;
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetResults exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }

      return null;
    }

    #endregion

    #region ActorInfo

    public void SetActorInfo(int idActor, IMDBActor actor)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.actorinfoes
                     where sql.idActor == idActor
                     select sql).FirstOrDefault();

        if (query == null)
        {
          // doesnt exists, add it
          
          Databases.actorinfo obj = new Databases.actorinfo()
          {
            idActor = idActor,
            dateofbirth = RemoveInvalidChars(actor.DateOfBirth),
            placeofbirth = RemoveInvalidChars(actor.PlaceOfBirth),
            minibio = RemoveInvalidChars(actor.MiniBiography),
            biography = RemoveInvalidChars(actor.Biography),
            thumbURL = RemoveInvalidChars(actor.ThumbnailUrl),
            IMDBActorID = RemoveInvalidChars(actor.IMDBActorID),
            dateofdeath = RemoveInvalidChars(actor.DateOfDeath),
            placeofdeath = RemoveInvalidChars(actor.PlaceOfDeath),
            lastupdate = DateTime.Now
          };
          _connection.actorinfoes.AddObject(obj);
          _connection.SaveChanges();
        }
        else
        {
          // exists, modify it
          
          query.dateofbirth = RemoveInvalidChars(actor.DateOfBirth);
          query.placeofbirth = RemoveInvalidChars(actor.PlaceOfBirth);
          query.minibio = RemoveInvalidChars(actor.MiniBiography);
          query.biography = RemoveInvalidChars(actor.Biography);
          query.thumbURL = RemoveInvalidChars(actor.ThumbnailUrl);
          query.IMDBActorID = RemoveInvalidChars(actor.IMDBActorID);
          query.dateofdeath =  RemoveInvalidChars(actor.DateOfDeath);
          query.placeofdeath = RemoveInvalidChars(actor.PlaceOfDeath);
          query.lastupdate = DateTime.Now;
          _connection.SaveChanges();
          RemoveActorInfoMovie(idActor);
        }

        for (int i = 0; i < actor.Count; ++i)
        {
          AddActorInfoMovie(idActor, actor[i]);
        }

        return;
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetActorInfo exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
        ConnectDb();
      }
      return;
    }

    private string RemoveInvalidChars(string p)
    {
        throw new NotImplementedException();
    }

    public void AddActorInfoMovie(int idActor, IMDBActor.IMDBActorMovie movie)
    {
      string movieTitle = RemoveInvalidChars(movie.MovieTitle);
      string movieRole = RemoveInvalidChars(movie.Role);

      try
      {
        if (_connection == null)
        {
          return;
        }

        Databases.actorinfomovy obj = new Databases.actorinfomovy()
        {
          idActor = idActor,
          idDirector  = - 1,
          strPlotOutline = "",
          strPlot = "",
          strTagLine = "",
          strVotes = "",
          fRating = "",
          strCast = "",
          strCredits = "",
          iYear = 1900,
          strGenre = "",
          strPictureURL = "",
          strTitle = "",
          IMDBID = movie.MovieImdbID,
          mpaa = "",
          runtime = -1,
          iswatched = 0,
          role = movieRole
        };

        _connection.actorinfomovies.AddObject(obj);
        _connection.SaveChanges();

        // populate IMDB Movies
        if (CheckMovieImdbId(movie.MovieImdbID))
        {
          var query = (from sql in _connection.imdbmovies
                       where sql.idIMDB == movie.MovieImdbID
                       select sql).FirstOrDefault();
          if (query == null)
          {
            Databases.imdbmovy obj2 = new Databases.imdbmovy()
            {
              idIMDB = movie.MovieImdbID,
              idTmdb = "", // Not used (TMDBid)
              strPlot = "",
              strCast = "",
              strCredits = "",
              iYear = movie.Year,
              strGenre = "",
              strPictureURL = "",
              strTitle = movieTitle,
              mpaa = ""
            };
            _connection.imdbmovies.AddObject(obj2);
            _connection.SaveChanges();
          }
          else
          {
            query.iYear = movie.Year;
            query.strTitle = movieTitle;
            _connection.SaveChanges();
          }
        }
        return;
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:AddActorInfoMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return;
    }

    public void RemoveActorInfoMovie(int actorId)
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.actorinfomovies
                     where sql.idActor == actorId
                     select sql).ToList();

        if (query.Count != 0)
        {
          foreach (var obj in query)
          {
            _connection.DeleteObject(obj);
          }
          _connection.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:RemoveActorInfoMovie exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public IMDBActor GetActorInfo(int idActor)
    {
      try
      {
        if (_connection == null)
        {
          return null;
        }

        var query = (from sql in _connection.actorinfoes
                     join s1 in _connection.actors on sql.idActor equals s1.idActor
                     where s1.idActor == idActor
                     select new {sql.biography, sql.dateofbirth, sql.dateofdeath, sql.minibio, s1.strActor, sql.placeofbirth, sql.placeofdeath, sql.thumbURL, sql.lastupdate, s1.IMDBActorID, sql.idActor }).FirstOrDefault(); 

        if (query != null)
        {
          IMDBActor actor = new IMDBActor();
          actor.Biography = query.biography.Replace("''", "'");
          actor.DateOfBirth = query.dateofbirth.Replace("''", "'");
          actor.DateOfDeath = query.dateofdeath.Replace("''", "'");
          actor.MiniBiography = query.minibio.Replace("''", "'");
          actor.Name = query.strActor.Replace("''", "'");
          actor.PlaceOfBirth = query.placeofbirth.Replace("''", "'");
          actor.PlaceOfDeath = query.placeofdeath.Replace("''", "'");
          actor.ThumbnailUrl = query.thumbURL;
          actor.LastUpdate = query.lastupdate.ToString();
          actor.IMDBActorID = query.IMDBActorID;
          actor.ID = query.idActor;

          var query2 = (from sql in _connection.actorinfomovies
                        where sql.idActor == idActor
                       select sql).ToList();

          for (int i = 0; i < query2.Count; ++i)
          {
            string imdbId = query2[i].IMDBID;

            var query3 = (from sql in _connection.imdbmovies 
                          where sql.idIMDB == imdbId
                          select sql).FirstOrDefault();

            IMDBActor.IMDBActorMovie movie = new IMDBActor.IMDBActorMovie();
            movie.ActorID = (int)query2[i].idActor;
            movie.Role = query2[i].role;

            if (query3 != null)
            {
              // Added IMDBid
              movie.MovieTitle = query3.strTitle;
              movie.Year = (int)query3.iYear;
              movie.MovieImdbID =  query3.idIMDB;
              movie.MoviePlot =  query3.strPlot;
              movie.MovieCover =  query3.strPictureURL;
              movie.MovieGenre =  query3.strGenre;
              movie.MovieCast =  query3.strCast;
              movie.MovieCredits =  query3.strCredits;
              movie.MovieRuntime = (int)query2[i].runtime; // Not used
              movie.MovieMpaaRating =  query3.mpaa;
            }
            actor.Add(movie);
          }
          return actor;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetActorInfo exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return null;
    }

    public string GetActorImdbId(int idActor)
    {
      try
      {
        if (_connection == null)
        {
          return null;
        }

        var query = (from sql in _connection.actors
                     where sql.idActor == idActor
                     select sql).FirstOrDefault();

        if (query == null)
        {
          return query.IMDBActorID;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:GetActorImdbId exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return string.Empty;
    }

    #endregion

    #region Video thumbnail blacklisting

    public bool IsVideoThumbBlacklisted(string path)
    {
      try
      {
        if (_connection == null)
        {
          return false;
        }

        FileInfo fileInfo = null;

        try
        {
          fileInfo = new FileInfo(path);
        }
        catch
        {
          //ignore
        }

        if (fileInfo == null || !fileInfo.Exists)
        {
          return false;
        }

        path = path.Trim();
        RemoveInvalidChars(ref path);

        string strFileLength = fileInfo.Length.ToString();
      
        var query = (from sql in _connection.videothumbblists
                     where sql.strPath == path && sql.strFileSize == strFileLength && sql.strExpires > DateTime.Now && sql.strFileDate == fileInfo.LastWriteTimeUtc
                     select sql).FirstOrDefault();

        if (query != null)
        {
          return true;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:IsVideoThumbBlacklisted exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return false;
    }

    public int VideoThumbBlacklist(string path, DateTime expiresOn)
    {
      try
      {
        if (_connection == null)
        {
          return -1;
        }
        FileInfo fileInfo = null;

        try
        {
          fileInfo = new FileInfo(path);
        }
        catch
        {
          //ignore
        }

        if (fileInfo == null || !fileInfo.Exists)
        {
          return -1;
        }

        path = path.Trim();
        RemoveInvalidChars(ref path);
        
        var query = (from sql in _connection.videothumbblists
                     where sql.strPath == path
                     select sql).FirstOrDefault();

        if (query == null)
        {
          // doesnt exists, add it

          Databases.videothumbblist obj = new Databases.videothumbblist()
          {
            strPath = path,
            strExpires = expiresOn,
            strFileDate = fileInfo.LastWriteTimeUtc,
            strFileSize = fileInfo.Length.ToString()
          };
          _connection.videothumbblists.AddObject(obj);
          _connection.SaveChanges();

          var query2 = (from sql in _connection.videothumbblists
                       where sql.strPath == path
                       select sql).FirstOrDefault();

          int id = query2.idVideoThumbBList;

          RemoveExpiredVideoThumbBlacklistEntries();
          return id;
        }
        else
        {
          int id = query.idVideoThumbBList;

          if (id != -1)
          {

            var query2 = (from sql in _connection.videothumbblists
                          where sql.idVideoThumbBList == id
                          select sql).FirstOrDefault();

            if (query2 == null)
            {
              query2.strExpires = expiresOn;
              query2.strFileDate = fileInfo.LastWriteTimeUtc;
              query2.strFileSize = fileInfo.Length.ToString();
              _connection.SaveChanges();

              return id;
            }
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:VideoThumbBlacklist exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return -1;
    }

    public bool VideoThumbRemoveFromBlacklist(string path)
    {
      try
      {
        if (_connection == null)
        {
          return false;
        }

        path = path.Trim();
        RemoveInvalidChars(ref path);

        var query = (from sql in _connection.videothumbblists
                     where sql.strPath == path
                     select sql).FirstOrDefault();

        if (query != null && query.idVideoThumbBList != -1)
        {
          _connection.DeleteObject(query);
          _connection.SaveChanges();

          return true;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:VideoThumbRemoveFromBlacklist exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
      return false;
    }

    public void RemoveExpiredVideoThumbBlacklistEntries()
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.videothumbblists
                     where sql.strExpires <= DateTime.Now
                     select sql).ToList();

        foreach(var q in query)
        {
          _connection.DeleteObject(q);
        }
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabase exceptionADO:RemoveExpiredVideoThumbBlacklistEntries err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public void RemoveAllVideoThumbBlacklistEntries()
    {
      try
      {
        if (_connection == null)
        {
          return;
        }

        var query = (from sql in _connection.videothumbblists
                     select sql).ToList();

        foreach (var q in query)
        {
          _connection.DeleteObject(q);
        }
        _connection.SaveChanges();
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:RemoveAllVideoThumbBlacklistEntries exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    #endregion

    public void ExecuteSQL(string strSql, out bool error, out string errorMessage)
    {
      error = false;
      errorMessage = string.Empty;

      try
      {
        if (_connection == null)
        {
          return;
        }
        Log.Error("videodatabaseADO:ExecuteSQL {0}", strSql);
        var query = _connection.ExecuteStoreQuery<string>(strSql).FirstOrDefault();
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:ExecuteSQL exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        error = true;
        errorMessage = ex.Message;
        ConnectDb();
      }
    }

    public ArrayList ExecuteRuleSQL(string strSql, string fieldName, out bool error, out string errorMessage)
    {
      ArrayList values = new ArrayList();
      error = false;
      errorMessage = string.Empty;

      try
      {
        if (_connection == null)
        {
          return values;
        }

        Log.Error("videodatabaseADO:ExecuteRuleSQL {0}", strSql);

        var query = _connection.ExecuteStoreQuery<string>(strSql).ToList();

        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          values.Add(query[iRow]);
        }
      }
      catch (ThreadAbortException)
      {
        // Will be logged in thread main code
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:ExecuteRuleSQL exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        error = true;
        errorMessage = ex.Message;
        ConnectDb();
      }

      values.Sort();
      return values;
    }

    public bool CheckMovieImdbId(string id)
    {
      // IMDBtt search check tt number, must be exactly 9 chars with leading zeros if needed
      if (id == null || id.Length != 9)
      {
        return false;
      }

      // Final IMDBtt check
      Match ttNo = Regex.Match(id, @"tt[\d]{7}?", RegexOptions.IgnoreCase);

      if (!ttNo.Success)
      {
        return false;
      }

      return true;
    }

    public bool CheckActorImdbId(string id)
    {
      // IMDBnm search check nm number, must be exactly 9 chars with leading zeros if needed
      if (id == null || id.Length != 9)
      {
        return false;
      }

      // Final IMDBtt check
      Match ttNo = Regex.Match(id, @"nm[\d]{7}?", RegexOptions.IgnoreCase);

      if (!ttNo.Success)
      {
        return false;
      }

      return true;
    }

    public void ImportNfoUsingVideoFile(string videoFile, bool skipExisting, bool refreshdbOnly)
    {
      try
      {
        string nfoFile = string.Empty;
        string path = string.Empty;
        bool isbdDvd = false;
        string nfoExt = ".nfo";

        if (videoFile.ToUpperInvariant().IndexOf(@"\VIDEO_TS\VIDEO_TS.IFO", StringComparison.InvariantCultureIgnoreCase) >= 0)
        {
          //DVD folder
          path = videoFile.Substring(0, videoFile.ToUpperInvariant().IndexOf(@"\VIDEO_TS\VIDEO_TS.IFO", StringComparison.InvariantCultureIgnoreCase));
          isbdDvd = true;
        }
        else if (videoFile.ToUpperInvariant().IndexOf(@"\BDMV\INDEX.BDMV", StringComparison.InvariantCultureIgnoreCase) >= 0)
        {
          //BD folder
          path = videoFile.Substring(0, videoFile.ToUpperInvariant().IndexOf(@"\BDMV\INDEX.BDMV", StringComparison.InvariantCultureIgnoreCase));
          isbdDvd = true;
        }

        if (isbdDvd)
        {
          string cleanFile = string.Empty;
          cleanFile = Path.GetFileNameWithoutExtension(videoFile);
          Util.Utils.RemoveStackEndings(ref cleanFile);
          nfoFile = path + @"\" + cleanFile + nfoExt;

          if (!File.Exists(nfoFile))
          {
            cleanFile = Path.GetFileNameWithoutExtension(path);
            Util.Utils.RemoveStackEndings(ref cleanFile);
            nfoFile = path + @"\" + cleanFile + nfoExt;
          }
        }
        else
        {
          string cleanFile = string.Empty;
          string strPath, strFilename;
          DatabaseUtility.Split(videoFile, out strPath, out strFilename);
          cleanFile = strFilename;
          Util.Utils.RemoveStackEndings(ref cleanFile);
          cleanFile = strPath + cleanFile;
          nfoFile = Path.ChangeExtension(cleanFile, nfoExt);
        }

        Log.Debug("Importing nfo:{0} using video file:{1}", nfoFile, videoFile);

        if (!File.Exists(nfoFile))
        {
          return;
        }

        IMDBMovie movie = new IMDBMovie();
        int id = GetMovieInfo(videoFile, ref movie);

        if (skipExisting && id > 0)
        {
          movie = null;
          return;
        }

        ImportNfo(nfoFile, skipExisting, refreshdbOnly);
        movie = null;
      }
      catch (Exception ex)
      {
        Log.Error("Error importing nfo for file {0} Error:{1} ", videoFile, ex);
      }
    }

    public void ImportNfo(string nfoFile, bool skipExisting, bool refreshdbOnly)
    {
      IMDBMovie movie = new IMDBMovie();
      bool isMovieFolder = Util.Utils.IsFolderDedicatedMovieFolder(Path.GetFullPath(nfoFile));
      bool useInternalNfoScraper = false;

      using (Profile.Settings xmlreader = new Profile.MPSettings())
      {
        // Use only nfo scrapper
        useInternalNfoScraper = xmlreader.GetValueAsBool("moviedatabase", "useonlynfoscraper", false);
      }

      try
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(nfoFile);
        Log.Debug("Importing nfo file:{0}", nfoFile);

        if (doc.DocumentElement != null)
        {
          int id = -1;

          XmlNodeList movieList = doc.DocumentElement.SelectNodes("/movie");

          if (movieList == null)
          {
            Log.Debug("Movie tag for nfo file:{0} not exist. Nfo skipped.", nfoFile);
            return;
          }

          foreach (XmlNode nodeMovie in movieList)
          {
            string genre = string.Empty;
            string cast = string.Empty;
            string path = string.Empty;
            string nfofileName = string.Empty;

            #region nodes

            XmlNode nodeTitle = nodeMovie.SelectSingleNode("title");
            XmlNode nodeSortTitle = nodeMovie.SelectSingleNode("sorttitle");
            XmlNode nodeRating = nodeMovie.SelectSingleNode("rating");
            XmlNode nodeYear = nodeMovie.SelectSingleNode("year");
            XmlNode nodeDuration = nodeMovie.SelectSingleNode("runtime");
            XmlNode nodePlotShort = nodeMovie.SelectSingleNode("outline");
            XmlNode nodePlot = nodeMovie.SelectSingleNode("plot");
            XmlNode nodeTagline = nodeMovie.SelectSingleNode("tagline");
            XmlNode nodeDirector = nodeMovie.SelectSingleNode("director");
            XmlNode nodeDirectorImdb = nodeMovie.SelectSingleNode("directorimdb");
            XmlNode nodeImdbNumber = nodeMovie.SelectSingleNode("imdb");
            XmlNode nodeIdImdbNumber = nodeMovie.SelectSingleNode("id");
            XmlNode nodeMpaa = nodeMovie.SelectSingleNode("mpaa");
            XmlNode nodeTop250 = nodeMovie.SelectSingleNode("top250");
            XmlNode nodeVotes = nodeMovie.SelectSingleNode("votes");
            XmlNode nodeStudio = nodeMovie.SelectSingleNode("studio");
            XmlNode nodePlayCount = nodeMovie.SelectSingleNode("playcount");
            XmlNode nodeWatched = nodeMovie.SelectSingleNode("watched");
            XmlNode nodeFanart = nodeMovie.SelectSingleNode("fanart");
            XmlNode nodePoster = nodeMovie.SelectSingleNode("thumb");
            XmlNode nodeLanguage = nodeMovie.SelectSingleNode("language");
            XmlNode nodeCountry = nodeMovie.SelectSingleNode("country");
            XmlNode nodeReview = nodeMovie.SelectSingleNode("review");
            XmlNode nodeCredits = nodeMovie.SelectSingleNode("credits");

            #endregion

            #region Moviefiles

            // Get path from *.nfo file)
            Util.Utils.Split(nfoFile, out path, out nfofileName);
            // Movie filename to search from gathered files from nfo path
            nfofileName = Util.Utils.GetFilename(nfofileName, true);
            // Get all video files from nfo path
            ArrayList files = new ArrayList();
            GetVideoFiles(path, ref files);
            bool isDvdBdFolder = false;

            foreach (String file in files)
            {
              //Log.Debug("Import nfo-processing video file:{0} (Total files: {1})", file, files.Count);
              string logFilename = Path.GetFileName(file);

              if ((file.ToUpperInvariant().Contains("VIDEO_TS.IFO") ||
                  file.ToUpperInvariant().Contains("INDEX.BDMV")) && files.Count == 1)
              {
                var pattern = Util.Utils.StackExpression();
                int stackSequence = -1; // seq 0 = [x-y], seq 1 = CD1, Part1....
                int digit = 0;

                for (int i = 0; i < pattern.Length; i++)
                {
                  if (pattern[i].IsMatch(file))
                  {
                    digit = Convert.ToInt16(pattern[i].Match(file).Groups["digit"].Value);
                    stackSequence = i;
                    break;
                  }
                }
                if (digit > 1)
                {
                  Log.Debug("Import nfo-file: {0} is stack part.", file);
                  string filename;
                  string tmpPath = string.Empty;
                  DatabaseUtility.Split(file, out path, out filename);

                  try
                  {
                    if (stackSequence == 0)
                    {
                      string strReplace = "[" + digit;
                      int stackIndex = path.LastIndexOf(strReplace);
                      tmpPath = path.Remove(stackIndex, 2);
                      tmpPath = tmpPath.Insert(stackIndex, "[1");
                    }
                    else
                    {
                      int stackIndex = path.LastIndexOf(digit.ToString());
                      tmpPath = path.Remove(stackIndex, 1);
                      tmpPath = tmpPath.Insert(stackIndex, "1");
                    }

                    int movieId = VideoDatabase.GetMovieId(tmpPath + filename);
                    int pathId = VideoDatabase.AddPath(path);
                    Log.Debug("Import nfo-Adding file: {0}", logFilename);
                    VideoDatabase.AddFile(movieId, pathId, filename);
                    return;
                  }
                  catch (Exception ex)
                  {
                    Log.Error("Import nfo error-stack check for path {0} Error: {1}", path, ex.Message);
                    return;
                  }
                }

                id = VideoDatabase.AddMovie(file, true);
                movie.ID = id;
                isDvdBdFolder = true;
              }
              else
              {
                string tmpFile = string.Empty;
                string tmpPath = string.Empty;

                // Read filename
                Util.Utils.Split(file, out tmpPath, out tmpFile);
                // Remove extension
                tmpFile = Util.Utils.GetFilename(tmpFile, true);
                // Remove stack endings (CD1...)
                Util.Utils.RemoveStackEndings(ref tmpFile);
                Util.Utils.RemoveStackEndings(ref nfofileName);

                // Check and add to vdb and get movieId
                if (tmpFile.Equals(nfofileName, StringComparison.InvariantCultureIgnoreCase))
                {
                  Log.Debug("Import nfo-Adding file: {0}", logFilename);
                  id = VideoDatabase.AddMovie(file, true);
                  movie.ID = id;
                }
                else if (isMovieFolder && tmpPath.Length > 0) // Every movie in it's own folder, compare by folder name
                {
                  try
                  {
                    tmpPath = tmpPath.Substring(tmpPath.LastIndexOf(@"\") + 1).Trim();

                    if (tmpPath.Equals(nfofileName, StringComparison.InvariantCultureIgnoreCase) || nfofileName.ToLowerInvariant() == "movie")
                    {
                      Log.Debug("Import nfo-Adding file: {0}", logFilename);
                      id = VideoDatabase.AddMovie(file, true);
                      movie.ID = id;
                    }
                    else
                    {
                      Log.Debug("Import nfo-Skipping file:{0}", logFilename);
                    }
                  }
                  catch (Exception ex)
                  {
                    Log.Error("Import nfo-Error comparing path name. File:{0} Err.:{1}", file, ex.Message);
                  }
                }
                else
                {
                  Log.Debug("Import nfo-Skipping file: {0}", logFilename);
                }
              }
            }

            #endregion

            #region Check for existing movie or refresh database only

            GetMovieInfoById(id, ref movie);

            if (skipExisting && !movie.IsEmpty || refreshdbOnly && movie.IsEmpty || id < 1)
            {
              Log.Debug("Import nfo-Skipping import for movieId = {0}).", id);
              return;
            }

            movie = new IMDBMovie();
            movie.ID = id;

            #endregion

            #region Genre

            XmlNodeList genres = nodeMovie.SelectNodes("genre");

            foreach (XmlNode nodeGenre in genres)
            {
              if (nodeGenre.InnerText != null)
              {
                if (genre.Length > 0)
                {
                  genre += " / ";
                }
                genre += nodeGenre.InnerText;
              }
            }

            if (string.IsNullOrEmpty(genre))
            {
              genres = nodeMovie.SelectNodes("genres/genre");

              foreach (XmlNode nodeGenre in genres)
              {
                if (nodeGenre.InnerText != null)
                {
                  if (genre.Length > 0)
                  {
                    genre += " / ";
                  }
                  genre += nodeGenre.InnerText;
                }
              }
            }

            movie.Genre = genre;

            #endregion

            #region Credits (Writers)

            // Writers
            if (nodeCredits != null)
            {
              movie.WritingCredits = nodeCredits.InnerText;
            }
            #endregion

            #region DateAdded

            movie.DateAdded = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            #endregion

            #region Title

            // Title
            if (nodeTitle != null)
            {
              movie.Title = nodeTitle.InnerText;
            }

            #endregion

            #region Sort Title

            // SortTitle
            if (nodeSortTitle != null)
            {
              if (!string.IsNullOrEmpty(nodeTitle.InnerText))
              {
                movie.SortTitle = nodeSortTitle.InnerText;
              }
              else
              {
                movie.SortTitle = movie.Title;
              }
            }

            #endregion

            #region Language

            // Title
            if (nodeLanguage != null)
            {
              movie.Language = nodeLanguage.InnerText;
            }

            #endregion

            #region Country

            // Title
            if (nodeCountry != null)
            {
              movie.Country = nodeCountry.InnerText;
            }

            #endregion

            #region IMDB number

            // IMDB number
            if (nodeImdbNumber != null)
            {
              if (CheckMovieImdbId(nodeImdbNumber.InnerText))
              {
                movie.IMDBNumber = nodeImdbNumber.InnerText;
              }
            }

            if (string.IsNullOrEmpty(movie.IMDBNumber) && nodeIdImdbNumber != null)
            {
              if (CheckMovieImdbId(nodeIdImdbNumber.InnerText))
              {
                movie.IMDBNumber = nodeIdImdbNumber.InnerText;
              }
            }

            #endregion

            #region CD/DVD labels

            // CD label
            movie.CDLabel = string.Empty;

            // DVD label
            movie.DVDLabel = string.Empty;

            #endregion

            #region Director

            // Director
            string dirImdb = string.Empty;
            if (nodeDirectorImdb != null)
            {
              dirImdb = nodeDirectorImdb.InnerText;

              if (!CheckActorImdbId(dirImdb))
              {
                dirImdb = string.Empty;
              }
            }
            if (nodeDirector != null)
            {
              movie.Director = nodeDirector.InnerText;
              movie.DirectorID = VideoDatabase.AddActor(dirImdb, movie.Director);
            }
            #endregion

            #region Studio

            // Studio
            if (nodeStudio != null)
            {
              movie.Studios = nodeStudio.InnerText;
            }

            #endregion

            #region MPAA

            // MPAA
            if (nodeMpaa != null)
            {
              movie.MPARating = nodeMpaa.InnerText;
            }
            else
            {
              movie.MPARating = "NR";
            }

            #endregion

            #region Plot/Short plot

            // Plot
            if (nodePlot != null)
            {
              movie.Plot = nodePlot.InnerText;
            }
            else
            {
              movie.Plot = string.Empty;
            }
            // Short plot
            if (nodePlotShort != null)
            {
              movie.PlotOutline = nodePlotShort.InnerText;
            }
            else
            {
              movie.PlotOutline = string.Empty;
            }

            #endregion

            #region Review

            // Title
            if (nodeReview != null)
            {
              movie.UserReview = nodeReview.InnerText;
            }

            #endregion

            #region Rating (n.n/10)

            // Rating
            if (nodeRating != null)
            {
              double rating = 0;
              if (Double.TryParse(nodeRating.InnerText.Replace(".", ","), out rating))
              {
                movie.Rating = (float)rating;

                if (movie.Rating > 10.0f)
                {
                  movie.Rating /= 10.0f;
                }
              }
            }

            #endregion

            #region Duration

            // Duration
            if (nodeDuration != null)
            {
              int runtime = 0;
              if (Int32.TryParse(nodeDuration.InnerText, out runtime))
              {
                movie.RunTime = runtime;
              }
              else
              {
                string regex = "(?<h>[0-9]*)h.(?<m>[0-9]*)";
                MatchCollection mc = Regex.Matches(nodeDuration.InnerText, regex, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (mc.Count > 0)
                {
                  foreach (Match m in mc)
                  {
                    int hours = 0;
                    Int32.TryParse(m.Groups["h"].Value, out hours);
                    int minutes = 0;
                    Int32.TryParse(m.Groups["m"].Value, out minutes);
                    hours = hours * 60;
                    minutes = hours + minutes;
                    movie.RunTime = minutes;
                  }
                }
                else
                {
                  regex = @"\d*\s*min.";
                  if (Regex.Match(nodeDuration.InnerText, regex, RegexOptions.IgnoreCase).Success)
                  {
                    regex = @"\d*";
                    int minutes = 0;
                    Int32.TryParse(Regex.Match(nodeDuration.InnerText, regex).Value, out minutes);
                    movie.RunTime = minutes;
                  }
                }
              }
            }
            else
            {
              movie.RunTime = 0;
            }

            #endregion

            #region Tagline

            // Tagline
            if (nodeTagline != null)
            {
              movie.TagLine = nodeTagline.InnerText;
            }

            #endregion

            #region TOP250

            // Top250
            if (nodeTop250 != null)
            {
              int top250 = 0;
              Int32.TryParse(nodeTop250.InnerText, out top250);
              movie.Top250 = top250;
            }
            else
            {
              movie.Top250 = 0;
            }


            #endregion

            #region votes

            // Votes
            if (nodeVotes != null)
            {
              movie.Votes = nodeVotes.InnerText;
            }

            #endregion

            #region Watched/watched count

            // Watched
            int percent = 0;
            int watchedCount = 0;
            GetMovieWatchedStatus(movie.ID, out percent, out watchedCount);

            if (watchedCount < 1)
            {
              if (nodeWatched != null)
              {
                if (nodeWatched.InnerText.ToLowerInvariant() == "true" || nodeWatched.InnerText == "1")
                {
                  movie.Watched = 1;
                  VideoDatabase.SetMovieWatchedStatus(movie.ID, true, 100);
                }
                else
                {
                  movie.Watched = 0;
                  VideoDatabase.SetMovieWatchedStatus(movie.ID, false, 0);
                }
              }
              // Watched count
              if (nodePlayCount != null)
              {
                watchedCount = 0;
                Int32.TryParse(nodePlayCount.InnerText, out watchedCount);
                SetMovieWatchedCount(movie.ID, watchedCount);

                if (watchedCount > 0 && movie.Watched == 0)
                {
                  movie.Watched = 1;
                  VideoDatabase.SetMovieWatchedStatus(movie.ID, true, 100);
                }
                else if (watchedCount == 0 && movie.Watched > 0)
                {
                  SetMovieWatchedCount(movie.ID, 1);
                }
              }
            }
            else
            {
              movie.Watched = 1;
            }

            #endregion

            #region Year

            // Year
            if (nodeYear != null)
            {
              int year = 0;
              Int32.TryParse(nodeYear.InnerText, out year);
              movie.Year = year;
            }

            #endregion

            #region poster

            // Poster
            string thumbJpgFile = string.Empty;
            string thumbTbnFile = string.Empty;
            string thumbFolderJpgFile = string.Empty;
            string thumbFolderTbnFile = string.Empty;
            string titleExt = Util.Utils.GetCoverFilename(id, movie.IMDBNumber, movie.Title);
            string jpgExt = @".jpg";
            string tbnExt = @".tbn";
            string folderJpg = @"\folder.jpg";
            string folderTbn = @"\folder.tbn";

            if (isDvdBdFolder)
            {
              thumbJpgFile = path + @"\" + Path.GetFileNameWithoutExtension(path) + jpgExt;
              thumbTbnFile = path + @"\" + Path.GetFileNameWithoutExtension(path) + tbnExt;
              thumbFolderJpgFile = path + @"\" + folderJpg;
              thumbFolderTbnFile = path + @"\" + folderTbn;
            }
            else
            {
              thumbJpgFile = path + @"\" + nfofileName + jpgExt;
              thumbTbnFile = path + @"\" + nfofileName + tbnExt;

              if (isMovieFolder)
              {
                thumbFolderJpgFile = path + @"\" + folderJpg;
                thumbFolderTbnFile = path + @"\" + folderTbn;
              }
            }

            if (nodePoster != null)
            {
              // Local source cover
              if (File.Exists(thumbJpgFile))
              {
                CreateCovers(titleExt, thumbJpgFile, movie);
              }
              else if (File.Exists(thumbTbnFile))
              {
                CreateCovers(titleExt, thumbTbnFile, movie);
              }
              else if (!string.IsNullOrEmpty(thumbFolderJpgFile) && File.Exists(thumbFolderJpgFile))
              {
                CreateCovers(titleExt, thumbFolderJpgFile, movie);
              }
              else if (!string.IsNullOrEmpty(thumbFolderTbnFile) && File.Exists(thumbFolderTbnFile))
              {
                CreateCovers(titleExt, thumbFolderTbnFile, movie);
              }
              else if (!nodePoster.InnerText.StartsWith("http:") && File.Exists(nodePoster.InnerText))
              {
                CreateCovers(titleExt, nodePoster.InnerText, movie);
              }
              else if (!nodePoster.InnerText.StartsWith("http:") && File.Exists(path + @"\" + nodePoster.InnerText))
              {
                CreateCovers(titleExt, path + @"\" + nodePoster.InnerText, movie);
              }
              // web source cover
              else if (nodePoster.InnerText.StartsWith("http:"))
              {
                try
                {
                  string imageUrl = nodePoster.InnerText;
                  if (imageUrl.Length > 0)
                  {
                    string largeCoverArtImage = Util.Utils.GetLargeCoverArtName(Thumbs.MovieTitle, titleExt);
                    string coverArtImage = Util.Utils.GetCoverArtName(Thumbs.MovieTitle, titleExt);
                    if (!File.Exists(coverArtImage))
                    {
                      string imageExtension = Path.GetExtension(imageUrl);
                      if (imageExtension == string.Empty)
                      {
                        imageExtension = jpgExt;
                      }
                      string temporaryFilename = "MPTempImage";
                      temporaryFilename += imageExtension;
                      temporaryFilename = Path.Combine(Path.GetTempPath(), temporaryFilename);
                      Util.Utils.FileDelete(temporaryFilename);
                      Util.Utils.DownLoadAndOverwriteCachedImage(imageUrl, temporaryFilename);

                      if (File.Exists(temporaryFilename))
                      {
                        if (Util.Picture.CreateThumbnail(temporaryFilename, largeCoverArtImage, (int)Thumbs.ThumbLargeResolution,
                                                          (int)Thumbs.ThumbLargeResolution, 0, Thumbs.SpeedThumbsSmall))
                        {
                          Util.Picture.CreateThumbnail(temporaryFilename, coverArtImage, (int)Thumbs.ThumbResolution,
                                                        (int)Thumbs.ThumbResolution, 0, Thumbs.SpeedThumbsLarge);
                        }
                      }

                      Util.Utils.FileDelete(temporaryFilename);
                    }
                  }
                }
                catch (Exception ex)
                {
                  Log.Error("Import nfo - Poster node: {0}", ex.Message);
                }
                movie.ThumbURL = nodePoster.InnerText;
              }
              // MP scrapers cover
              else
              {
                if (movie.ThumbURL == string.Empty && !useInternalNfoScraper)
                {
                  // IMPAwards
                  IMPAwardsSearch impSearch = new IMPAwardsSearch();

                  if (movie.Year > 1900)
                  {
                    impSearch.SearchCovers(movie.Title + " " + movie.Year, movie.IMDBNumber);
                  }
                  else
                  {
                    impSearch.SearchCovers(movie.Title, movie.IMDBNumber);
                  }

                  if ((impSearch.Count > 0) && (impSearch[0] != string.Empty))
                  {
                    movie.ThumbURL = impSearch[0];
                  }

                  // If no IMPAwards lets try TMDB 
                  TMDBCoverSearch tmdbSearch = new TMDBCoverSearch();

                  if (impSearch.Count == 0)
                  {
                    tmdbSearch.SearchCovers(movie.Title, movie.IMDBNumber);

                    if ((tmdbSearch.Count > 0) && (tmdbSearch[0] != string.Empty))
                    {
                      movie.ThumbURL = tmdbSearch[0];
                    }
                  }
                  // All fail, last try IMDB
                  if (impSearch.Count == 0 && tmdbSearch.Count == 0)
                  {
                    IMDBSearch imdbSearch = new IMDBSearch();
                    imdbSearch.SearchCovers(movie.IMDBNumber, true);

                    if ((imdbSearch.Count > 0) && (imdbSearch[0] != string.Empty))
                    {
                      movie.ThumbURL = imdbSearch[0];
                    }
                  }
                }

                string largeCoverArt = Util.Utils.GetLargeCoverArtName(Thumbs.MovieTitle, titleExt);
                string coverArt = Util.Utils.GetCoverArtName(Thumbs.MovieTitle, titleExt);

                if (movie.ID >= 0)
                {
                  if (!string.IsNullOrEmpty(movie.ThumbURL))
                  {
                    Util.Utils.FileDelete(largeCoverArt);
                    Util.Utils.FileDelete(coverArt);
                  }

                  // Save cover thumbs
                  if (!string.IsNullOrEmpty(movie.ThumbURL))
                  {
                    IMDBFetcher.DownloadCoverArt(Thumbs.MovieTitle, movie.ThumbURL, titleExt);
                  }
                }
              }
            }
            else // Node thumb not exist
            {
              if (File.Exists(thumbJpgFile))
              {
                CreateCovers(titleExt, thumbJpgFile, movie);
              }
              else if (File.Exists(thumbTbnFile))
              {
                CreateCovers(titleExt, thumbTbnFile, movie);
              }
              else if (!string.IsNullOrEmpty(thumbFolderJpgFile) && File.Exists(thumbFolderJpgFile))
              {
                CreateCovers(titleExt, thumbFolderJpgFile, movie);
              }
              else if (!string.IsNullOrEmpty(thumbFolderTbnFile) && File.Exists(thumbFolderTbnFile))
              {
                CreateCovers(titleExt, thumbFolderTbnFile, movie);
              }
              else if (movie.ThumbURL == string.Empty && !useInternalNfoScraper)
              {
                // IMPAwards
                IMPAwardsSearch impSearch = new IMPAwardsSearch();

                if (movie.Year > 1900)
                {
                  impSearch.SearchCovers(movie.Title + " " + movie.Year, movie.IMDBNumber);
                }
                else
                {
                  impSearch.SearchCovers(movie.Title, movie.IMDBNumber);
                }

                if ((impSearch.Count > 0) && (impSearch[0] != string.Empty))
                {
                  movie.ThumbURL = impSearch[0];
                }

                // If no IMPAwards lets try TMDB 
                TMDBCoverSearch tmdbSearch = new TMDBCoverSearch();

                if (impSearch.Count == 0)
                {
                  tmdbSearch.SearchCovers(movie.Title, movie.IMDBNumber);

                  if ((tmdbSearch.Count > 0) && (tmdbSearch[0] != string.Empty))
                  {
                    movie.ThumbURL = tmdbSearch[0];
                  }
                }
                // All fail, last try IMDB
                if (impSearch.Count == 0 && tmdbSearch.Count == 0)
                {
                  IMDBSearch imdbSearch = new IMDBSearch();
                  imdbSearch.SearchCovers(movie.IMDBNumber, true);

                  if ((imdbSearch.Count > 0) && (imdbSearch[0] != string.Empty))
                  {
                    movie.ThumbURL = imdbSearch[0];
                  }
                }

                string largeCoverArt = Util.Utils.GetLargeCoverArtName(Thumbs.MovieTitle, titleExt);
                string coverArt = Util.Utils.GetCoverArtName(Thumbs.MovieTitle, titleExt);

                if (movie.ID >= 0)
                {
                  if (!string.IsNullOrEmpty(movie.ThumbURL))
                  {
                    Util.Utils.FileDelete(largeCoverArt);
                    Util.Utils.FileDelete(coverArt);
                  }

                  // Save cover thumbs
                  if (!string.IsNullOrEmpty(movie.ThumbURL))
                  {
                    IMDBFetcher.DownloadCoverArt(Thumbs.MovieTitle, movie.ThumbURL, titleExt);
                  }
                }
              }
            }

            #endregion

            #region Fanart

            // Fanart
            XmlNodeList fanartNodeList = nodeMovie.SelectNodes("fanart/thumb");

            int faIndex = 0;
            bool faFound = false;
            string faFile = string.Empty;
            FanArt fa = new FanArt();

            foreach (XmlNode fanartNode in fanartNodeList)
            {
              if (fanartNode != null)
              {
                faFile = path + @"\" + fanartNode.InnerText;

                if (File.Exists(faFile))
                {
                  fa.GetLocalFanart(movie.IMDBNumber,id, "file://" + faFile, faIndex);
                  movie.FanartURL = faFile;
                  faFound = true;
                }
              }
              faIndex++;

              if (faIndex == 5)
              {
                break;
              }
            }

            if (!faFound)
            {
              List<string> localFanart = new List<string>();
              faIndex = 0;
              faFile = path + @"\" + nfofileName + "-fanart.jpg";
              localFanart.Add(faFile);
              faFile = path + @"\" + nfofileName + "-backdrop.jpg";
              localFanart.Add(faFile);
              faFile = path + @"\" + "backdrop.jpg";
              localFanart.Add(faFile);
              faFile = path + @"\" + "fanart.jpg";
              localFanart.Add(faFile);

              foreach (string fanart in localFanart)
              {
                if (File.Exists(fanart))
                {
                  fa.GetLocalFanart(movie.IMDBNumber,id, "file://" + fanart, faIndex);
                  movie.FanartURL = fanart;
                  faFound = true;
                  break;
                }
              }

              if (!faFound && !useInternalNfoScraper)
              {
                fa.GetTmdbFanartByApi(movie.ID, movie.IMDBNumber, string.Empty, false, 1, string.Empty);
              }
            }

            #endregion

            #region Cast

            // Cast parse
            XmlNodeList actorsList = nodeMovie.SelectNodes("actor");

            foreach (XmlNode nodeActor in actorsList)
            {
              string name = string.Empty;
              string role = string.Empty;
              string actorImdbId = string.Empty;
              string line = string.Empty;
              XmlNode nodeActorName = nodeActor.SelectSingleNode("name");
              XmlNode nodeActorRole = nodeActor.SelectSingleNode("role");
              XmlNode nodeActorImdbId = nodeActor.SelectSingleNode("imdb");

              XmlNode nodeActorBirthDate = nodeActor.SelectSingleNode("birthdate");
              XmlNode nodeActorBirthPlace = nodeActor.SelectSingleNode("birthplace");
              XmlNode nodeActorDeathDate = nodeActor.SelectSingleNode("deathdate");
              XmlNode nodeActorDeathPlace = nodeActor.SelectSingleNode("deathplace");
              XmlNode nodeActorMiniBio = nodeActor.SelectSingleNode("minibiography");
              XmlNode nodeActorBiography = nodeActor.SelectSingleNode("biography");
              XmlNode nodeActorThumbnail = nodeActor.SelectSingleNode("thumb");

              if (nodeActorName != null && nodeActorName.InnerText != null)
              {
                name = nodeActorName.InnerText;
              }
              if (nodeActorRole != null && nodeActorRole.InnerText != null)
              {
                role = nodeActorRole.InnerText;
              }
              if (nodeActorImdbId != null)
              {
                if (CheckActorImdbId(nodeActorImdbId.InnerText))
                {
                  actorImdbId = nodeActorImdbId.InnerText;
                }
              }
              if (!string.IsNullOrEmpty(name))
              {
                if (!string.IsNullOrEmpty(role))
                {
                  line = String.Format("{0} as {1}\n", name, role);
                }
                else
                {
                  line = String.Format("{0}\n", name);
                }
                cast += line;

                int actId = VideoDatabase.AddActor(actorImdbId, name);

                VideoDatabase.AddActorToMovie(id, actId, role);

                if (CheckActorImdbId(actorImdbId))
                {
                  IMDBActor info = new IMDBActor();
                  info.IMDBActorID = actorImdbId;

                  if (nodeActorBirthDate != null)
                  {
                    info.DateOfBirth = nodeActorBirthDate.InnerText;
                  }
                  if (nodeActorBirthPlace != null)
                  {
                    info.PlaceOfBirth = nodeActorBirthPlace.InnerText;
                  }
                  if (nodeActorDeathDate != null)
                  {
                    info.DateOfDeath = nodeActorDeathDate.InnerText;
                  }
                  if (nodeActorDeathPlace != null)
                  {
                    info.PlaceOfDeath = nodeActorDeathPlace.InnerText;
                  }
                  if (nodeActorMiniBio != null)
                  {
                    info.MiniBiography = nodeActorMiniBio.InnerText;
                  }
                  if (nodeActorBiography != null)
                  {
                    info.Biography = nodeActorBiography.InnerText;
                  }

                  if (info.DateOfBirth != string.Empty ||
                      info.PlaceOfBirth != string.Empty ||
                      info.DateOfDeath != string.Empty ||
                      info.PlaceOfBirth != string.Empty ||
                      info.MiniBiography != string.Empty ||
                      info.Biography != string.Empty)
                  {
                    SetActorInfo(actId, info);
                  }
                }
              }
            }
            // Cast
            movie.Cast = cast;

            #endregion

            #region UserGroups

            XmlNodeList userGroups = nodeMovie.SelectNodes("set");

            // Main node as <set> ---- </set> with subnodes name, rule, image
            foreach (XmlNode nodeUserGroup in userGroups)
            {
              if (nodeUserGroup != null)
              {
                string name = string.Empty;
                string description = string.Empty;
                string rule = string.Empty;
                string image = string.Empty;
                XmlNode nodeSetName = nodeUserGroup.SelectSingleNode("setname");
                XmlNode nodeSetDescription = nodeUserGroup.SelectSingleNode("setdescription");
                XmlNode nodeSetRule = nodeUserGroup.SelectSingleNode("setrule");
                XmlNode nodeSetImage = nodeUserGroup.SelectSingleNode("setimage");

                if (nodeSetName != null && nodeSetName.InnerText != null)
                {
                  name = nodeSetName.InnerText;
                }

                if (nodeSetDescription != null && nodeSetDescription.InnerText != null)
                {
                  description = nodeSetDescription.InnerText;
                }

                if (nodeSetRule != null && nodeSetRule.InnerText != null)
                {
                  rule = nodeSetRule.InnerText;
                }

                if (nodeSetImage != null && nodeSetImage.InnerText != null)
                {
                  image = nodeSetImage.InnerText;
                  image = string.Format("{0}/{1}", path, image);
                }

                if (!string.IsNullOrEmpty(name))
                {
                  int iUserGroup = AddUserGroup(name);
                  AddUserGroupToMovie(movie.ID, iUserGroup);

                  if (!string.IsNullOrEmpty(description))
                  {
                    AddUserGroupDescription(name, description);
                  }

                  if (!string.IsNullOrEmpty(rule))
                  {
                    bool error = false;
                    string errorMessage = string.Empty;
                    Log.Error("import nfo: {0}", rule);
                    VideoDatabase.ExecuteSql(rule, out error, out errorMessage);

                    if (!error)
                    {
                      AddUserGroupRuleByGroupId(iUserGroup, rule);
                    }
                    else
                    {
                      Log.Error("VideoDatabase nfo import: error adding user group {0} rule: {1}", name, rule);
                    }
                  }

                  // Only local image
                  if (!string.IsNullOrEmpty(image) && File.Exists(image))
                  {
                    string smallThumb = Util.Utils.GetCoverArtName(Thumbs.MovieUserGroups, Path.GetFileNameWithoutExtension(image));
                    string largeThumb = Util.Utils.GetLargeCoverArtName(Thumbs.MovieUserGroups, Path.GetFileNameWithoutExtension(image));
                    Util.Utils.FileDelete(smallThumb);
                    Util.Utils.FileDelete(largeThumb);

                    if (Util.Picture.CreateThumbnail(image, smallThumb, (int)Thumbs.ThumbResolution,
                      (int)Thumbs.ThumbResolution, 0, Thumbs.SpeedThumbsSmall))
                    {
                      Util.Picture.CreateThumbnail(image, largeThumb, (int)Thumbs.ThumbLargeResolution,
                        (int)Thumbs.ThumbLargeResolution, 0, Thumbs.SpeedThumbsLarge);
                    }
                  }
                }
                else // Single node as <set>setname</set>
                {
                  name = nodeUserGroup.InnerText;

                  if (!string.IsNullOrEmpty(name))
                  {
                    int iUserGroup = AddUserGroup(name);
                    AddUserGroupToMovie(movie.ID, iUserGroup);
                  }
                }
              }
            }

            VideoDatabase.SetMovieInfoById(id, ref movie, true);

            // Add groups with rules
            ArrayList groups = new ArrayList();
            VideoDatabase.GetUserGroups(groups);

            foreach (string group in groups)
            {
              string rule = VideoDatabase.GetUserGroupRule(group);

              if (!string.IsNullOrEmpty(rule))
              {
                try
                {
                  ArrayList values = new ArrayList();
                  bool error = false;
                  string errorMessage = string.Empty;
                  values = VideoDatabase.ExecuteRuleSql(rule, "movieinfo.idMovie", out error, out errorMessage);

                  if (error)
                  {
                    Log.Error("VideoDatabase nfo import: error executing rule {0} syntax, {1}", rule, errorMessage);
                    continue;
                  }

                  if (values.Count > 0 && values.Contains(movie.ID.ToString()))
                  {
                    VideoDatabase.AddUserGroupToMovie(movie.ID, VideoDatabase.AddUserGroup(group));
                  }
                }
                catch (Exception ex)
                {
                  Log.Error("VideoDatabase nfo import: error importing usergroup rule {0} - {1}", rule, ex.Message);
                }
              }
            }

            #endregion


          }
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO exception error importing nfo file {0}:{1} ", nfoFile, ex.Message);
      }
    }

    public bool MakeNfo(int movieId)
    {
      string moviePath = string.Empty;
      string movieFile = string.Empty;
      ArrayList movieFiles = new ArrayList();
      ArrayList nfoFiles = new ArrayList();
      string nfoFile = string.Empty;
      int fileCounter = 0;

      try
      {
        // Get files
        GetFilesForMovie(movieId, ref movieFiles);

        foreach (string file in movieFiles)
        {
          if (!File.Exists(file))
          {
            return false;
          }

          movieFile = file;
          Util.Utils.Split(movieFile, out moviePath, out movieFile);

          // Check for DVD folder
          if (movieFile.ToUpperInvariant() == "VIDEO_TS.IFO" || movieFile.ToUpperInvariant() == "INDEX.BDMV")
          {
            // Remove \VIDEO_TS from directory structure
            string directoryDVD = moviePath.Substring(0, moviePath.LastIndexOf(@"\"));

            if (Directory.Exists(directoryDVD))
            {
              moviePath = directoryDVD;
              movieFile = directoryDVD;
            }
          }
          else
          {
            if (fileCounter > 0)
            {
              return true;
            }
          }
          // remove stack endings (CDx..) form filename
          Util.Utils.RemoveStackEndings(ref movieFile);
          // Remove file extension
          movieFile = Util.Utils.GetFilename(movieFile, true).Trim();
          // Add nfo extension
          nfoFile = moviePath + @"\" + movieFile + ".nfo";
          Util.Utils.FileDelete(nfoFile);
          nfoFiles.Add(nfoFile);
          //}

          IMDBMovie movieDetails = new IMDBMovie();
          GetMovieInfoById(movieId, ref movieDetails);
          // Prepare XML
          XmlDocument doc = new XmlDocument();
          XmlDeclaration xmldecl = doc.CreateXmlDeclaration("1.0", "UTF-8", null);

          // Main tag
          XmlNode mainNode = doc.CreateElement("movie");
          XmlNode subNode;

          #region Movie fields

          // Filenames
          foreach (string strMovieFile in movieFiles)
          {
            CreateXmlNode(mainNode, doc, "filenameandpath", strMovieFile);
          }

          // Title
          CreateXmlNode(mainNode, doc, "title", movieDetails.Title);
          // Sort Title
          if (!string.IsNullOrEmpty(movieDetails.SortTitle))
          {
            CreateXmlNode(mainNode, doc, "sorttitle", movieDetails.SortTitle);
          }
          else
          {
            CreateXmlNode(mainNode, doc, "sorttitle", movieDetails.Title);
          }

          //  movie IMDB number
          CreateXmlNode(mainNode, doc, "imdb", movieDetails.IMDBNumber);
          CreateXmlNode(mainNode, doc, "id", movieDetails.IMDBNumber);
          //  Language
          CreateXmlNode(mainNode, doc, "language", movieDetails.Language);
          //  Country
          CreateXmlNode(mainNode, doc, "country", movieDetails.Country);
          //  Year
          CreateXmlNode(mainNode, doc, "year", movieDetails.Year.ToString());
          //  Rating
          CreateXmlNode(mainNode, doc, "rating", movieDetails.Rating.ToString().Replace(",", "."));
          //  Runtime
          CreateXmlNode(mainNode, doc, "runtime", movieDetails.RunTime.ToString());
          // MPAA
          CreateXmlNode(mainNode, doc, "mpaa", movieDetails.MPARating);
          // Votes
          CreateXmlNode(mainNode, doc, "votes", movieDetails.Votes);
          // TOp 250
          CreateXmlNode(mainNode, doc, "top250", movieDetails.Top250.ToString());
          // Studio
          CreateXmlNode(mainNode, doc, "studio", movieDetails.Studios);
          //  Director
          CreateXmlNode(mainNode, doc, "director", movieDetails.Director);
          //  Director imdbId
          CreateXmlNode(mainNode, doc, "directorimdb", GetActorImdbId(movieDetails.ID));
          // Credits
          CreateXmlNode(mainNode, doc, "credits", movieDetails.WritingCredits);
          // Tagline
          CreateXmlNode(mainNode, doc, "tagline", movieDetails.TagLine);
          // Plot outline (short one)
          CreateXmlNode(mainNode, doc, "outline", movieDetails.PlotOutline);
          // Plot - long
          CreateXmlNode(mainNode, doc, "plot", movieDetails.Plot);
          // Review
          CreateXmlNode(mainNode, doc, "review", movieDetails.UserReview);
          // Watched
          string watched = "false";

          if (movieDetails.Watched > 0)
          {
            watched = "true";
          }

          CreateXmlNode(mainNode, doc, "watched", watched);

          // Watched count
          int percent = 0;
          int watchedCount = 0;
          GetMovieWatchedStatus(movieId, out percent, out watchedCount);
          CreateXmlNode(mainNode, doc, "playcount", watchedCount.ToString());

          // Poster
          string titleExt = Util.Utils.GetCoverFilename(movieId, movieDetails.IMDBNumber, movieDetails.Title);
          string largeCoverArtImage = Util.Utils.GetLargeCoverArtName(Thumbs.MovieTitle, titleExt);
          string coverFilename = moviePath + @"\" + movieFile + ".jpg";

          if (File.Exists(largeCoverArtImage))
          {
            try
            {
              File.Copy(largeCoverArtImage, coverFilename, true);
              File.SetAttributes(coverFilename, FileAttributes.Normal);
              CreateXmlNode(mainNode, doc, "thumb", movieFile + ".jpg");
            }
            catch (Exception ex)
            {
              Log.Info("VideoDatabase: Error in creating nfo - poster node:{0}", ex.Message);
            }
          }

          // Fanart
          string faFile = string.Empty;
          subNode = doc.CreateElement("fanart");

          for (int i = 0; i < 5; i++)
          {
              FanArt.GetFanArtfilename(movieDetails.IMDBNumber, movieId, i, out faFile);
            string index = string.Empty;

            if (File.Exists(faFile))
            {
              if (i > 0)
              {
                index = i.ToString();
              }

              try
              {
                string faFilename = moviePath + @"\" + movieFile + "-fanart" + index + ".jpg";
                File.Copy(faFile, faFilename, true);
                File.SetAttributes(faFilename, FileAttributes.Normal);
                CreateXmlNode(subNode, doc, "thumb", movieFile + "-fanart" + index + ".jpg");
              }
              catch (Exception ex)
              {
                Log.Info("VideoDatabas: Error in creating nfo - fanart section:{0}", ex.Message);
              }

            }
          }
          mainNode.AppendChild(subNode);

          // Genre
          string szGenres = movieDetails.Genre;

          if (szGenres.IndexOf("/") >= 0 || szGenres.IndexOf("|") >= 0)
          {
            Tokens f = new Tokens(szGenres, new[] { '/', '|' });

            foreach (string strGenre in f)
            {
              if (!string.IsNullOrEmpty(strGenre))
              {
                CreateXmlNode(mainNode, doc, "genre", strGenre.Trim());
              }
            }
          }
          else
          {
            CreateXmlNode(mainNode, doc, "genre", movieDetails.Genre);
          }

          // Cast
          ArrayList castList = new ArrayList();
          GetActorsByMovieID(movieId, ref castList);

          foreach (string actor in castList)
          {
            IMDBActor actorInfo = new IMDBActor();
            subNode = doc.CreateElement("actor");

            char[] splitter = { '|' };
            string[] temp = actor.Split(splitter);
            actorInfo = GetActorInfo(Convert.ToInt32(temp[0]));

            CreateXmlNode(subNode, doc, "name", temp[1]);
            CreateXmlNode(subNode, doc, "role", temp[3]);
            CreateXmlNode(subNode, doc, "imdb", temp[2]);

            if (actorInfo != null)
            {
              CreateXmlNode(subNode, doc, "thumb", actorInfo.ThumbnailUrl);
              CreateXmlNode(subNode, doc, "birthdate", actorInfo.DateOfBirth);
              CreateXmlNode(subNode, doc, "birthplace", actorInfo.PlaceOfBirth);
              CreateXmlNode(subNode, doc, "deathdate", actorInfo.DateOfDeath);
              CreateXmlNode(subNode, doc, "deathplace", actorInfo.PlaceOfDeath);
              CreateXmlNode(subNode, doc, "minibiography", actorInfo.MiniBiography);
              CreateXmlNode(subNode, doc, "biography", actorInfo.Biography);
            }

            mainNode.AppendChild(subNode);
          }

          // User groups
          ArrayList userGroups = new ArrayList();
          GetMovieUserGroups(movieId, userGroups);

          if (userGroups.Count > 0)
          {
            foreach (string userGroup in userGroups)
            {
              subNode = doc.CreateElement("set");
              CreateXmlNode(subNode, doc, "setname", userGroup);

              string rule = GetUserGroupRule(userGroup);
              string description = GetUserGroupDescriptionById(GetUserGroupId(userGroup));

              if (!string.IsNullOrEmpty(rule))
              {
                CreateXmlNode(subNode, doc, "setrule", rule);
              }

              if (!string.IsNullOrEmpty(description))
              {
                CreateXmlNode(subNode, doc, "setdescription", description);
              }

              // Image is not exportable beacuse it is already resized and not in original quality
              //CreateXmlNode(subNode, doc, "setimage", string.Empty);

              mainNode.AppendChild(subNode);
            }
          }

          // Trailer
          CreateXmlNode(mainNode, doc, "trailer", string.Empty);

          #endregion

          // End and save
          doc.AppendChild(mainNode);
          doc.InsertBefore(xmldecl, mainNode);
          doc.Save(nfoFile);
          fileCounter++;
        }
      }
      catch (Exception ex)
      {
        Log.Info("VideoDatabaseADO: Error in creating nfo file:{0} Error:{1}", nfoFile, ex.Message);
        return false;
      }

      return true;
    }

    private void CreateXmlNode(XmlNode mainNode, XmlDocument doc, string element, string innerTxt)
    {
      XmlNode subNode = doc.CreateElement(element);
      subNode.InnerText = innerTxt;
      mainNode.AppendChild(subNode);
    }

    private void CreateCovers(string titleExt, string coverImage, IMDBMovie movie)
    {
      movie.ThumbURL = coverImage;
      string largeCoverArt = Util.Utils.GetLargeCoverArtName(Thumbs.MovieTitle, titleExt);
      string coverArt = Util.Utils.GetCoverArtName(Thumbs.MovieTitle, titleExt);
      Util.Utils.FileDelete(largeCoverArt);
      Util.Utils.FileDelete(coverArt);

      if (Util.Picture.CreateThumbnail(coverImage, largeCoverArt, (int)Thumbs.ThumbLargeResolution,
                                       (int)Thumbs.ThumbLargeResolution, 0, Thumbs.SpeedThumbsSmall))
      {
        Util.Picture.CreateThumbnail(coverImage, coverArt, (int)Thumbs.ThumbResolution,
                                     (int)Thumbs.ThumbResolution, 0, Thumbs.SpeedThumbsLarge);
      }
    }

    public void GetVideoFiles(string path, ref ArrayList availableFiles)
    {
      //
      // Count the files in the current directory
      //
      try
      {
        VirtualDirectory dir = new VirtualDirectory();
        dir.SetExtensions(Util.Utils.VideoExtensions);
        ArrayList imagePath = new ArrayList();

        // Thumbs creation spam no1 causing this call
        //
        // Temporary disable thumbcreation
        //
        using (Settings xmlReaderWriter = new MPSettings())
        {
          _currentCreateVideoThumbs = xmlReaderWriter.GetValueAsBool("thumbnails", "videoondemand", true);
          xmlReaderWriter.SetValueAsBool("thumbnails", "videoondemand", false);
        }

        List<GUIListItem> items = dir.GetDirectoryUnProtectedExt(path, true, false);

        foreach (GUIListItem item in items)
        {
          if (item.IsFolder)
          {
            if (item.Label != "..")
            {
              if (item.Path.ToUpperInvariant().IndexOf(@"\VIDEO_TS") >= 0)
              {
                string strFile = String.Format(@"{0}\VIDEO_TS.IFO", item.Path);
                availableFiles.Add(strFile);
              }
              else if (item.Path.ToUpperInvariant().IndexOf(@"\BDMV") >= 0)
              {
                string strFile = String.Format(@"{0}\index.bdmv", item.Path);
                availableFiles.Add(strFile);
              }
              else
              {
                GetVideoFiles(item.Path, ref availableFiles);
              }
            }
          }
          else
          {
            bool skipDuplicate = false;

            if (VirtualDirectory.IsImageFile(Path.GetExtension(item.Path)))
            {
              string filePath = Path.GetDirectoryName(item.Path) + @"\" + Path.GetFileNameWithoutExtension(item.Path);

              if (!imagePath.Contains(filePath))
              {
                imagePath.Add(filePath);
              }
              else
              {
                skipDuplicate = true;
              }
            }
            if (!skipDuplicate)
            {
              string extension = Path.GetExtension(item.Path);

              if (extension != null && extension.ToUpperInvariant() != @".IFO" && extension.ToUpperInvariant() != ".BDMV")
              {
                availableFiles.Add(item.Path);
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        Log.Info("VideoDatabase: Exception counting video files:{0}", e);
      }
      finally
      {
        // Restore thumbcreation setting
        using (Settings xmlwriter = new MPSettings())
        {
          xmlwriter.SetValueAsBool("thumbnails", "videoondemand", _currentCreateVideoThumbs);
        }
      }
    }

    private void SetLatestMovieProperties()
    {
      string strSQL = "SELECT * FROM movieinfo ORDER BY dateAdded DESC LIMIT 3";

      ArrayList movies = new ArrayList();
      GetMoviesByFilter(strSQL, out movies, false, true, false, false);

      IMDBMovie movie1 = new IMDBMovie();
      IMDBMovie movie2 = new IMDBMovie();
      IMDBMovie movie3 = new IMDBMovie();

      GUIPropertyManager.SetProperty("#myvideos.latest1.enabled", "false");
      GUIPropertyManager.SetProperty("#myvideos.latest2.enabled", "false");
      GUIPropertyManager.SetProperty("#myvideos.latest3.enabled", "false");

      if (movies.Count > 0)
      {
        movie1 = (IMDBMovie)movies[0];
        // Movie 1
        GUIPropertyManager.SetProperty("#myvideos.latest1.genre", movie1.Genre.Replace(" /", ","));
        //
        string poster = string.Empty;
        string titleExt = Util.Utils.GetCoverFilename(movie1.ID, movie1.IMDBNumber, movie1.Title);
        poster = Util.Utils.GetLargeCoverArtName(Thumbs.MovieTitle, titleExt);
        GUIPropertyManager.SetProperty("#myvideos.latest1.thumb", poster);
        //
        GUIPropertyManager.SetProperty("#myvideos.latest1.title", movie1.Title);
        GUIPropertyManager.SetProperty("#myvideos.latest1.year", movie1.Year.ToString());
        //
        DateTime dateAdded;
        DateTime.TryParseExact(movie1.DateAdded, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out dateAdded);
        GUIPropertyManager.SetProperty("#myvideos.latest1.dateAdded", dateAdded.ToShortDateString());
        //
        GUIPropertyManager.SetProperty("#myvideos.latest1.runtime", movie1.RunTime +
                                " " +
                                GUILocalizeStrings.Get(2998) +
                                " (" + Util.Utils.SecondsToHMString(movie1.RunTime * 60) + ")");
        GUIPropertyManager.SetProperty("#myvideos.latest1.enabled", "true");

        if (movies.Count > 1)
        {
          movie2 = (IMDBMovie)movies[1];
          // Movie 2
          GUIPropertyManager.SetProperty("#myvideos.latest2.genre", movie2.Genre.Replace(" /", ","));
          //
          poster = string.Empty;
          titleExt = Util.Utils.GetCoverFilename(movie2.ID, movie2.IMDBNumber, movie2.Title);
          poster = Util.Utils.GetLargeCoverArtName(Thumbs.MovieTitle, titleExt);
          GUIPropertyManager.SetProperty("#myvideos.latest2.thumb", poster);
          //
          GUIPropertyManager.SetProperty("#myvideos.latest2.title", movie2.Title);
          GUIPropertyManager.SetProperty("#myvideos.latest2.year", movie2.Year.ToString());
          //
          DateTime.TryParseExact(movie2.DateAdded, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out dateAdded);
          GUIPropertyManager.SetProperty("#myvideos.latest2.dateAdded", dateAdded.ToShortDateString());
          //
          GUIPropertyManager.SetProperty("#myvideos.latest2.runtime", movie2.RunTime +
                                  " " +
                                  GUILocalizeStrings.Get(2998) +
                                  " (" + Util.Utils.SecondsToHMString(movie2.RunTime * 60) + ")");
          GUIPropertyManager.SetProperty("#myvideos.latest2.enabled", "true");
        }

        if (movies.Count > 2)
        {
          movie3 = (IMDBMovie)movies[2];
          // Movie 3
          GUIPropertyManager.SetProperty("#myvideos.latest3.genre", movie3.Genre.Replace(" /", ","));
          //
          poster = string.Empty;
          titleExt = Util.Utils.GetCoverFilename(movie2.ID, movie2.IMDBNumber, movie2.Title);
          poster = Util.Utils.GetLargeCoverArtName(Thumbs.MovieTitle, titleExt);
          GUIPropertyManager.SetProperty("#myvideos.latest3.thumb", poster);
          //
          GUIPropertyManager.SetProperty("#myvideos.latest3.title", movie3.Title);
          GUIPropertyManager.SetProperty("#myvideos.latest3.year", movie3.Year.ToString());
          //
          DateTime.TryParseExact(movie3.DateAdded, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out dateAdded);
          GUIPropertyManager.SetProperty("#myvideos.latest3.dateAdded", dateAdded.ToShortDateString());
          //
          GUIPropertyManager.SetProperty("#myvideos.latest3.runtime", movie3.RunTime +
                                  " " +
                                  GUILocalizeStrings.Get(2998) +
                                  " (" + Util.Utils.SecondsToHMString(movie3.RunTime * 60) + ")");
          GUIPropertyManager.SetProperty("#myvideos.latest3.enabled", "true");
        }
      }
    }

    private void SetMovieDetails(ref IMDBMovie details, int iRow, List<Databases.movieinfo> movieinfo)
    {
      if (details == null || iRow < 0 || movieinfo == null)
      {
        return;
      }
      try
      {
        int idMovie = (int)movieinfo[iRow].idMovie;

        var movie = (from sql in _connection.movies
                     where sql.idMovie == idMovie
                     select sql).FirstOrDefault();

        int idPath = (int)movie.idPath;

        var path = (from sql in _connection.paths
                    where sql.idPath == idPath
                    select sql).FirstOrDefault();

        double rating;
        Double.TryParse(movieinfo[iRow].fRating, out rating);

        details.Rating = (float)rating;

        if (details.Rating > 10.0f)
        {
          details.Rating /= 10.0f;
        }
 
        details.Director = movieinfo[iRow].strDirector.Replace("''", "'");
        // Add directorID
        try
        {
          details.DirectorID = (int)movieinfo[iRow].idDirector;
        }
        catch (Exception)
        {
          details.DirectorID = -1;
        }

        details.WritingCredits = movieinfo[iRow].strCredits.Replace("''", "'");
        details.TagLine = movieinfo[iRow].strTagLine.Replace("''", "'");
        details.PlotOutline = movieinfo[iRow].strPlotOutline.Replace("''", "'");
        details.Plot = movieinfo[iRow].strPlot.Replace("''", "'");

        // Added user review
        details.UserReview = movieinfo[iRow].strUserReview.Replace("''", "'");
        details.Votes = movieinfo[iRow].strVotes;
        details.Cast = movieinfo[iRow].strCast.Replace("''", "'");
        details.Year = (int)movieinfo[iRow].iYear;
        details.Genre = movieinfo[iRow].strGenre.Trim();

        details.ThumbURL = movieinfo[iRow].strPictureURL;

        // Fanart
        details.FanartURL = movieinfo[iRow].strFanartURL;

        // Date Added
        details.DateAdded = movieinfo[iRow].dateAdded.ToString("yyyy-MM-dd 00:00:00");

        // Date Watched
        if (movieinfo[iRow].dateWatched != null)
        {
          details.DateWatched = movieinfo[iRow].dateWatched.ToString("yyyy-MM-dd 00:00:00");
        }

        details.Title = movieinfo[iRow].strTitle.Replace("''", "'");

        details.Path = path.strPath;

        details.DVDLabel = movie.discid;

        details.IMDBNumber = movieinfo[iRow].IMDBID;

        Int32 lMovieId = (int)movieinfo[iRow].idMovie;

        details.SearchString = String.Format("{0}", details.Title);
        details.CDLabel = path.cdlabel;

        details.MPARating = movieinfo[iRow].mpaa;

        details.RunTime = (int)movieinfo[iRow].runtime;
        details.Watched = (int)movieinfo[iRow].iswatched;
        details.ID = lMovieId;
        details.Studios = movieinfo[iRow].studios;

        details.Country = movieinfo[iRow].country;

        details.Language = movieinfo[iRow].language;

        details.LastUpdate = movieinfo[iRow].lastupdate.ToString();

        details.SortTitle = movieinfo[iRow].strSortTitle.Replace("''", "'");

        if (string.IsNullOrEmpty(details.Path) && details.ID > 0)
        {
          int detailsID = details.ID;
          var query = (from sql in _connection.movies
                       join s1 in _connection.paths on sql.idPath equals s1.idPath
                       where sql.idMovie == detailsID
                       select new { s1 }).FirstOrDefault();

          details.Path = query.s1.strPath;
        }

        if (details.ID > 0)
        {
          int percent = 0;
          int watchedCount = 0;
          GetMovieWatchedStatus(details.ID, out percent, out watchedCount);
          details.WatchedPercent = percent;
          details.WatchedCount = watchedCount;

          string movieFilename = string.Empty;
          ArrayList files = new ArrayList();
          GetFilesForMovie(details.ID, ref files);

          int duration = GetMovieDuration(details.ID);
          details.Duration = duration;

          if (files.Count > 0)
          {
            movieFilename = (string)files[0];
          }

          details.VideoFileName = movieFilename;
          details.VideoFilePath = details.Path;

          VideoFilesMediaInfo mInfo = new VideoFilesMediaInfo();
          GetVideoFilesMediaInfo(details.ID, ref mInfo);
          details.MediaInfo = mInfo;
        }
      }
      catch (Exception ex)
      {
        Log.Error("videodatabaseADO:SetMovieDetails exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        ConnectDb();
      }
    }

    public bool DbHealth
    {
      get
      {
        return _dbHealth;
      }
    }

    public string DatabaseName
    {
      get
      {
        if (_connection != null)
        {
          return "VideoDatabase";
        }
        return "";
      }
    }

    public void FlushTransactionsToDisk()
    {
    }

    public void RevertFlushTransactionsToDisk()
    {
    }

    public void RemoveInvalidChars(ref string strTxt)
    {
    }
  }
}
