#region Copyright (C) 2006 Team MediaPortal

/* 
 *	Copyright (C) 2006 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.GUI.Library;
using System.Windows.Forms;
using System.Threading;


namespace ProcessPlugins.TvMovie
{
  public class TvMovie : ISetupForm, IPlugin
  {

    private TvMovieDatabase _database;
    private System.Threading.Timer _stateTimer;


    private void ImportThread()
    {
      Log.Write("TVMovie: Checking database");
      _database = new TvMovieDatabase();

      if (!_database.WasUpdated)
        return;

      int importedStations = _database.Import();
    }


    private void StartImportThread(Object stateInfo)
    {
      Thread importThread = new Thread(new ThreadStart(ImportThread));
      importThread.Priority = ThreadPriority.Lowest;
      importThread.Start();
    }


    #region ISetupForm Members


    public string PluginName()
    {
      return "TV Movie Clickfinder";
    }


    public string Description()
    {
      return "Import TV Movie Clickfinder EPG data";
    }


    public string Author()
    {
      return "mPod";
    }


    public void ShowPlugin()
    {
      Form setup = new TvMovieSettings();
      setup.ShowDialog();
    }


    public bool CanEnable()
    {
      return true;
    }


    public int GetWindowId()
    {
      return -1;
    }


    public bool DefaultEnabled()
    {
      return false;
    }


    public bool HasSetup()
    {
      return true;
    }


    public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
    {
      strButtonText = null;
      strButtonImage = null;
      strButtonImageFocus = null;
      strPictureImage = null;
      return false;
    }


    #endregion

    #region IPlugin Members

    
    public void Start()
    {
      TimerCallback timerCallBack = new TimerCallback(StartImportThread);
      _stateTimer = new System.Threading.Timer(timerCallBack, null, 10000, 1800000);
    }


    public void Stop()
    {
      if (_database != null)
        _database.Canceled = true;
    }


    #endregion
  }
}
