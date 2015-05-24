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

#include "IndependedSegmentsTag.h"
#include "ErrorCodes.h"

CIndependedSegmentsTag::CIndependedSegmentsTag(HRESULT *result)
  : CTag(result)
{
}

CIndependedSegmentsTag::~CIndependedSegmentsTag(void)
{
}

/* get methods */

/* set methods */

/* other methods */

bool CIndependedSegmentsTag::IsMediaPlaylistItem(unsigned int version)
{
  return ((version == PLAYLIST_VERSION_06) || (version == PLAYLIST_VERSION_07));
}

bool CIndependedSegmentsTag::IsMasterPlaylistItem(unsigned int version)
{
  return ((version == PLAYLIST_VERSION_06) || (version == PLAYLIST_VERSION_07));
}

bool CIndependedSegmentsTag::IsPlaylistItemTag(void)
{
  return false;
}

bool CIndependedSegmentsTag::ApplyTagToPlaylistItems(unsigned int version, CItemCollection *notProcessedItems, CPlaylistItemCollection *processedPlaylistItems)
{
  return false;
}

HRESULT CIndependedSegmentsTag::ParseTag(unsigned int version)
{
  HRESULT result = __super::ParseTag(version);
  CHECK_CONDITION_HRESULT(result, (version == PLAYLIST_VERSION_06) || (version == PLAYLIST_VERSION_07), result, E_M3U8_NOT_SUPPORTED_TAG);

  if (SUCCEEDED(result))
  {
    // successful parsing of tag
    // compare it to our tag
    CHECK_CONDITION_HRESULT(result, wcscmp(this->tag, TAG_INDEPENDED_SEGMENTS) == 0, result, E_M3U8_TAG_IS_NOT_OF_SPECIFIED_TYPE);
  }

  return result;
}

/* protected methods */

CItem *CIndependedSegmentsTag::CreateItem(void)
{
  HRESULT result = S_OK;
  CIndependedSegmentsTag *item = new CIndependedSegmentsTag(&result);
  CHECK_POINTER_HRESULT(result, item, result, E_OUTOFMEMORY);

  CHECK_CONDITION_EXECUTE(FAILED(result), FREE_MEM_CLASS(item));
  return item;
}

bool CIndependedSegmentsTag::CloneInternal(CItem *item)
{
  bool result = __super::CloneInternal(item);
  CIndependedSegmentsTag *tag = dynamic_cast<CIndependedSegmentsTag *>(item);
  result &= (tag != NULL);

  return result;
}