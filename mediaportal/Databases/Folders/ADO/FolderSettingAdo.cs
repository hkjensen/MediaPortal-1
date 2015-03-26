#region Copyright (C) 2005-2011 Team MediaPortal

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
using System.IO;
using System.Linq;
using System.Collections;
using System.Xml.Serialization;
using MediaPortal.Configuration;
using MediaPortal.Database;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using MediaPortal.Util;

namespace Databases.Folders.SqlServer
{
  public class FolderSettingAdo : IFolderSettings, IDisposable
  {
    private foldersettingEntities _connection;
    private bool _dbHealth = false;

    public FolderSettingAdo()
    {
      Log.Info("FolderSettingAdo");

      ConnectDb();
      CreateTables();
    }

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
          "metadata=res://*/Model1.csdl|res://*/Model1.ssdl|res://*/Model1.msl;provider=MySql.Data.MySqlClient;provider connection string=\"server={0};user id={1};password={2};persistsecurityinfo=True;database=foldersetting\"",
          host, userName, password);

        _connection = new foldersettingEntities(ConnectionString);
      }
      catch (Exception ex)
      {
        Log.Error("FolderdatabaseADO:ConnectDb exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
      }
    }

    public void Dispose()
    {
      if (_connection != null)
      {
        _connection.Dispose();
        _connection = null;
      }
    }

    private void CreateTables()
    {
      if (_connection == null)
      {
        return;
      }

      try
      {
        if (!_connection.DatabaseExists())
        {
          Log.Error("FolderSetting: database is not exist, createing...");
          _connection.CreateDatabase();
       
          ConnectDb();
        }
        _dbHealth = true;
      }
      catch (Exception ex)
      {
        Log.Error("PicturedatabaseADO:CreateDb exception err:{0} stack:{1} {2}", ex.Message, ex.StackTrace, ex.InnerException);
      }
    }

    private int AddPath(string filteredPath)
    {
      if (string.IsNullOrEmpty(filteredPath))
      {
        return -1;
      }

      if (null == _connection)
      {
        return -1;
      }

      try
      {
        var query = (from sql in _connection.tblpaths 
                     where sql.strPath == filteredPath 
                     select sql).FirstOrDefault();
        if (query == null)
        {
          // doesnt exists, add it
          Log.Debug("FolderSetting: AddPath doesnt exists, add it  {0}", filteredPath);
          tblpath path = new tblpath()
          {
            strPath = filteredPath
          };

          _connection.tblpaths.AddObject(path);
          _connection.SaveChanges();

          var query2 = (from sql in _connection.tblpaths
                       where sql.strPath == filteredPath
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
        Log.Error(ex);
      }
      return -1;
    }

    public void DeleteFolderSetting(string path, string Key)
    {
      if (null == _connection)
      {
        return;
      }

      if (string.IsNullOrEmpty(path))
      {
        return;
      }

      if (string.IsNullOrEmpty(Key))
      {
        return;
      }

      try
      {
        string pathFiltered = Utils.RemoveTrailingSlash(path);
        string keyFiltered = Key;
        RemoveInvalidChars(ref pathFiltered);
        RemoveInvalidChars(ref keyFiltered);

        int PathId = AddPath(pathFiltered);
        if (PathId < 0)
        {
          return;
        }

        var delObj = (from u in _connection.tblsettings
                     where u.idPath == PathId && u.tagName == keyFiltered
                      select u).FirstOrDefault();

        if (delObj != null)
        {
          _connection.DeleteObject(delObj);
          _connection.SaveChanges();
        }

      }
      catch (Exception ex)
      {
        Log.Error(ex);
      }
    }

    public void AddFolderSetting(string path, string key, Type type, object Value)
    {
      Log.Debug("FolderSetting: AddFolderSetting");
      if (string.IsNullOrEmpty(path))
      {
        return;
      }
      if (string.IsNullOrEmpty(key))
      {
        return;
      }
      if (null == _connection)
      {
        return;
      }

      try
      {
        string strPathFiltered = Utils.RemoveTrailingSlash(path);
        string KeyFiltered = key;
        RemoveInvalidChars(ref strPathFiltered);
        RemoveInvalidChars(ref KeyFiltered);

        int PathId = AddPath(strPathFiltered);
        if (PathId < 0)
        {
          return;
        }

        DeleteFolderSetting(path, key);

        XmlSerializer serializer = new XmlSerializer(type);
        //serialize...
        using (MemoryStream strm = new MemoryStream())
        {
          using (TextWriter w = new StreamWriter(strm))
          {
            serializer.Serialize(w, Value);
            w.Flush();
            strm.Seek(0, SeekOrigin.Begin);

            using (TextReader reader = new StreamReader(strm))
            {
              string ValueText = reader.ReadToEnd();
              string ValueTextFiltered = ValueText;
              RemoveInvalidChars(ref ValueTextFiltered);

              tblsetting obj = new tblsetting()
              {
                idPath = PathId,
                tagName = KeyFiltered,
                tagValue = ValueTextFiltered
              };

              _connection.tblsettings.AddObject(obj);
              _connection.SaveChanges();
            }
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error(ex);
      }
    }

    public void GetPath(string strPath, ref ArrayList strPathList, string strKey)
    {
      try
      {
        if (string.IsNullOrEmpty(strKey))
        {
          return;
        }
        if (string.IsNullOrEmpty(strPath))
        {
          return;
        }
        if (null == _connection)
        {
          return;
        }

        var query = (from u in _connection.tblpaths
                     join o in _connection.tblsettings
                     on u.idPath equals o.idPath
                     where u.strPath.StartsWith(strPath) && o.tagName == strKey
                     select new { strPath = u.strPath }).ToList();

        if (query.Count == 0)
        {
          return;
        }
        for (int iRow = 0; iRow < query.Count; iRow++)
        {
          strPathList.Add(query[iRow].strPath);
        }
      }
      catch (Exception ex)
      {
        Log.Error("Folderdatabase.GetPath() exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public void GetFolderSetting(string path, string key, Type type, out object valueObject)
    {
      valueObject = null;

      if (string.IsNullOrEmpty(path))
      {
        return;
      }
      if (string.IsNullOrEmpty(key))
      {
        return;
      }
      if (null == _connection)
      {
        return;
      }

      try
      {
        string strPathFiltered = Utils.RemoveTrailingSlash(path);
        string KeyFiltered = key;
        RemoveInvalidChars(ref strPathFiltered);
        RemoveInvalidChars(ref KeyFiltered);

        int PathId = AddPath(strPathFiltered);
        if (PathId < 0)
        {
          return;
        }

        var query = (from sql in _connection.tblsettings
                     where sql.idPath == PathId && sql.tagName == KeyFiltered
                     select sql).FirstOrDefault();

        if (query == null)
        {
          int pos = strPathFiltered.LastIndexOf(@"\");
          if ((strPathFiltered.Substring(1, 1) == ":" && pos > 1) || (strPathFiltered.Substring(0, 1) == "\\" && pos > 5))
          {
            string folderName;
            folderName = strPathFiltered.Substring(0, pos);

            Log.Debug("GetFolderSetting: {1} not found, trying the parent {0}", folderName, strPathFiltered);
            GetFolderSetting(folderName, key, type, out valueObject);
            return;
          }
          if (strPathFiltered != "root")
          {
            Log.Debug("GetFolderSetting: {0} parent not found. Trying the root.", strPathFiltered);
            GetFolderSetting("root", key, type, out valueObject);
            return;
          }
          Log.Debug("GetFolderSetting: {0} parent not found. Will use the default share settings.", strPathFiltered);
          return;
        }
        string strValue = query.tagValue;

        Log.Debug("GetFolderSetting: {0} found.", strPathFiltered);
        //deserialize...

        using (MemoryStream strm = new MemoryStream())
        {
          using (StreamWriter writer = new StreamWriter(strm))
          {
            writer.Write(strValue);
            writer.Flush();
            strm.Seek(0, SeekOrigin.Begin);
            using (TextReader r = new StreamReader(strm))
            {
              try
              {
                XmlSerializer serializer = new XmlSerializer(type);
                valueObject = serializer.Deserialize(r);
              }
              catch (Exception) { }
            }
          }
        }

      }
      catch (Exception ex)
      {
        Log.Error(ex);
      }
    }

    public string DatabaseName
    {
      get 
      { 
        if (_connection != null)
        {
          return "FolderSettings";
        }
        return "";
      }
    }

    public bool DbHealth
    {
      get
      {
        return _dbHealth;
      }
    }

    void RemoveInvalidChars(ref string strTxt)
    {
    }

  }
}