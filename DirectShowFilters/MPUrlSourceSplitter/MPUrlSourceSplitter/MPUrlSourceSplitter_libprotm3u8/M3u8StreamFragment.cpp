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

#include "M3u8StreamFragment.h"
#include "FastSearchItemCollection.h"

CM3u8StreamFragment::CM3u8StreamFragment(HRESULT *result, const wchar_t *uri, unsigned int fragment, int64_t fragmentTimestamp, unsigned int duration, unsigned int byteRangeOffset, unsigned int byteRangeLength, CM3u8FragmentEncryption *encryption)
  : CStreamFragment(result)
{
  this->uri = NULL;
  this->fragmentTimestamp = fragmentTimestamp;
  this->fragment = fragment;
  this->duration = duration;
  this->byteRangeOffset = UINT_MAX;
  this->byteRangeLength = UINT_MAX;
  this->encryption = NULL;

  if ((result != NULL) && (SUCCEEDED(*result)))
  {
    CHECK_POINTER_DEFAULT_HRESULT(*result, encryption);

    this->uri = Duplicate(uri);
    CHECK_CONDITION_EXECUTE(SUCCEEDED(*result), this->encryption = encryption->Clone());

    CHECK_POINTER_HRESULT(*result, this->uri, *result, E_OUTOFMEMORY);
    CHECK_POINTER_HRESULT(*result, this->encryption, *result, E_OUTOFMEMORY);
  }
}

CM3u8StreamFragment::~CM3u8StreamFragment(void)
{
  FREE_MEM(this->uri);
  FREE_MEM_CLASS(this->encryption);
}

/* get methods */

unsigned int CM3u8StreamFragment::GetFragment(void)
{
  return this->fragment;
}

int64_t CM3u8StreamFragment::GetFragmentTimestamp(void)
{
  return this->fragmentTimestamp;
}

const wchar_t *CM3u8StreamFragment::GetUri(void)
{
  return this->uri;
}

unsigned int CM3u8StreamFragment::GetDuration(void)
{
  return this->duration;
}

unsigned int CM3u8StreamFragment::GetByteRangeOffset(void)
{
  return this->byteRangeOffset;
}

unsigned int CM3u8StreamFragment::GetByteRangeLength(void)
{
  return this->byteRangeLength;
}

CM3u8FragmentEncryption *CM3u8StreamFragment::GetFragmentEncryption(void)
{
  return this->encryption;
}

/* set methods */

void CM3u8StreamFragment::SetEncrypted(bool encrypted, unsigned int streamFragmentItemIndex)
{
  this->flags &= ~M3U8_STREAM_FRAGMENT_FLAG_ENCRYPTED;
  this->flags |= encrypted ? M3U8_STREAM_FRAGMENT_FLAG_ENCRYPTED : M3U8_STREAM_FRAGMENT_FLAG_NONE;

  if ((this->owner != NULL) && (streamFragmentItemIndex != UINT_MAX))
  {
    this->owner->UpdateIndexes(streamFragmentItemIndex);
  }
}

void CM3u8StreamFragment::SetDecrypted(bool decrypted, unsigned int streamFragmentItemIndex)
{
  this->flags &= ~M3U8_STREAM_FRAGMENT_FLAG_DECRYPTED;
  this->flags |= decrypted ? M3U8_STREAM_FRAGMENT_FLAG_DECRYPTED : M3U8_STREAM_FRAGMENT_FLAG_NONE;

  if ((this->owner != NULL) && (streamFragmentItemIndex != UINT_MAX))
  {
    this->owner->UpdateIndexes(streamFragmentItemIndex);
  }
}

void CM3u8StreamFragment::SetEndOfStream(bool endOfStream)
{
  this->flags &= ~M3U8_STREAM_FRAGMENT_FLAG_END_OF_STREAM;
  this->flags |= endOfStream ? M3U8_STREAM_FRAGMENT_FLAG_END_OF_STREAM : M3U8_STREAM_FRAGMENT_FLAG_NONE;
}

void CM3u8StreamFragment::SetByteRangeOffset(unsigned int offset)
{
  this->byteRangeOffset = offset;
}

void CM3u8StreamFragment::SetByteRangeLength(unsigned int length)
{
  this->byteRangeLength = length;
}

/* other methods */

bool CM3u8StreamFragment::IsEncrypted(void)
{
  return this->IsSetFlags(M3U8_STREAM_FRAGMENT_FLAG_ENCRYPTED);
}

bool CM3u8StreamFragment::IsDecrypted(void)
{
  return this->IsSetFlags(M3U8_STREAM_FRAGMENT_FLAG_DECRYPTED);
}

bool CM3u8StreamFragment::IsEndOfStream(void)
{
  return this->IsSetFlags(M3U8_STREAM_FRAGMENT_FLAG_END_OF_STREAM);
}

/* protected methods */

CFastSearchItem *CM3u8StreamFragment::CreateItem(void)
{
  HRESULT result = S_OK;
  CM3u8StreamFragment *fragment = new CM3u8StreamFragment(&result, this->uri, this->fragment, this->fragmentTimestamp, this->duration, this->byteRangeOffset, this->byteRangeLength, this->encryption);
  CHECK_POINTER_HRESULT(result, fragment, result, E_OUTOFMEMORY);

  CHECK_CONDITION_EXECUTE(FAILED(result), FREE_MEM_CLASS(fragment));
  return fragment;
}

bool CM3u8StreamFragment::InternalClone(CFastSearchItem *item)
{
  bool result = __super::InternalClone(item);
  
  if (result)
  {
    CM3u8StreamFragment *fragment = dynamic_cast<CM3u8StreamFragment *>(item);
    result &= (fragment != NULL);
  }

  return result;
}