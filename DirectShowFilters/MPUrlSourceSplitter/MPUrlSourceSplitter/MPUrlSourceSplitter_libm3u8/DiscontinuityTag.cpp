/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#include "StdAfx.h"

#include "DiscontinuityTag.h"
#include "ItemCollection.h"
#include "PlaylistItemCollection.h"
#include "PlaylistItem.h"
#include "ErrorCodes.h"

CDiscontinuityTag::CDiscontinuityTag(HRESULT *result)
  : CTag(result)
{
}

CDiscontinuityTag::~CDiscontinuityTag(void)
{
}

/* get methods */

/* set methods */

/* other methods */

bool CDiscontinuityTag::IsMediaPlaylistItem(unsigned int version)
{
  return ((version == PLAYLIST_VERSION_01) || (version == PLAYLIST_VERSION_02) || (version == PLAYLIST_VERSION_03) || (version == PLAYLIST_VERSION_04) || (version == PLAYLIST_VERSION_05) || (version == PLAYLIST_VERSION_06) || (version == PLAYLIST_VERSION_07));
}

bool CDiscontinuityTag::IsMasterPlaylistItem(unsigned int version)
{
  return false;
}

bool CDiscontinuityTag::IsPlaylistItemTag(void)
{
  return true;
}

bool CDiscontinuityTag::ApplyTagToPlaylistItems(unsigned int version, CItemCollection *notProcessedItems, CPlaylistItemCollection *processedPlaylistItems)
{
  if ((version == PLAYLIST_VERSION_01) || (version == PLAYLIST_VERSION_02) || (version == PLAYLIST_VERSION_03) || (version == PLAYLIST_VERSION_04) || (version == PLAYLIST_VERSION_05) || (version == PLAYLIST_VERSION_06) || (version == PLAYLIST_VERSION_07))
  {
    // it is applied to exactly previous playlist item
    bool applied = true;

    CPlaylistItem *playlistItem = processedPlaylistItems->GetItem(processedPlaylistItems->Count() - 1);

    if (playlistItem != NULL)
    {
      CTag *clone = (CTag *)this->Clone();
      applied &= (clone != NULL);

      CHECK_CONDITION_EXECUTE(applied, applied &= playlistItem->GetTags()->Add(clone));

      CHECK_CONDITION_EXECUTE(!applied, FREE_MEM_CLASS(clone));
    }

    return applied;
  }
  else
  {
    // unknown playlist version
    return false;
  }
}

HRESULT CDiscontinuityTag::ParseTag(unsigned int version)
{
  HRESULT result = __super::ParseTag(version);
  CHECK_CONDITION_HRESULT(result, (version == PLAYLIST_VERSION_01) || (version == PLAYLIST_VERSION_02) || (version == PLAYLIST_VERSION_03) || (version == PLAYLIST_VERSION_04) || (version == PLAYLIST_VERSION_05) || (version == PLAYLIST_VERSION_06) || (version == PLAYLIST_VERSION_07), result, E_M3U8_NOT_SUPPORTED_TAG);

  if (SUCCEEDED(result))
  {
    // successful parsing of tag
    // compare it to our tag
    CHECK_CONDITION_HRESULT(result, wcscmp(this->tag, TAG_DISCONTINUITY) == 0, result, E_M3U8_TAG_IS_NOT_OF_SPECIFIED_TYPE);
  }

  return result;
}

/* protected methods */

CItem *CDiscontinuityTag::CreateItem(void)
{
  HRESULT result = S_OK;
  CDiscontinuityTag *item = new CDiscontinuityTag(&result);
  CHECK_POINTER_HRESULT(result, item, result, E_OUTOFMEMORY);

  CHECK_CONDITION_EXECUTE(FAILED(result), FREE_MEM_CLASS(item));
  return item;
}

bool CDiscontinuityTag::CloneInternal(CItem *item)
{
  bool result = __super::CloneInternal(item);
  CDiscontinuityTag *tag = dynamic_cast<CDiscontinuityTag *>(item);
  result &= (tag != NULL);

  return result;
}