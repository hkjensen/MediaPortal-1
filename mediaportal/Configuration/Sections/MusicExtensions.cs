#region Copyright (C) 2005-2010 Team MediaPortal

// Copyright (C) 2005-2010 Team MediaPortal
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


#pragma warning disable 108

namespace MediaPortal.Configuration.Sections
{
  public class MusicExtensions : BaseFileExtensions
  {
    public MusicExtensions()
      : this("Music Extensions") {}

    public MusicExtensions(string name)
      : base(name) {}

    public override void LoadSettings()
    {
      base.LoadSettings("music",
                        ".mp3,.wma,.ogg,.flac,.wav,.cda,.m3u,.pls,.b4s,.m4a,.m4p,.mp4,.wpl,.wv,.ape,.mpc,.cue,.aif,.aiff");
    }

    public override void SaveSettings()
    {
      base.SaveSettings("music");
    }
  }
}