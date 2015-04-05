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

#include "ProtocolVersion.h"
#include "conversions.h"

CProtocolVersion::CProtocolVersion(HRESULT *result)
  : CSessionTag(result)
{
  this->protocolVersion = PROTOCOL_VERSION_DEFAULT;
}

CProtocolVersion::~CProtocolVersion(void)
{
}

/* get methods */

unsigned int CProtocolVersion::GetProtocolVersion(void)
{
  return this->protocolVersion;
}

/* set methods */

void CProtocolVersion::SetProtocolVersion(unsigned int protocolVersion)
{
  this->protocolVersion = protocolVersion;
}

/* other methods */

void CProtocolVersion::Clear(void)
{
  __super::Clear();
}

unsigned int CProtocolVersion::Parse(const wchar_t *buffer, unsigned int length)
{
  unsigned int tempResult = __super::Parse(buffer, length);
  unsigned int result = (tempResult > SESSION_TAG_SIZE) ? tempResult : 0;

  if (result != 0)
  {
    // successful parsing of session tag
    // compare it to our session tag
    result = (wcscmp(this->originalTag, TAG_PROTOCOL_VERSION) == 0) ? result : 0;
    result = (this->tagContent != NULL) ? result : 0;
  }

  if (result != 0)
  {
    this->protocolVersion = GetValueUnsignedIntW(this->tagContent, PROTOCOL_VERSION_DEFAULT);
    this->instanceTag = Duplicate(TAG_PROTOCOL_VERSION);

    result = (this->instanceTag != NULL) ? result : 0;
  }

  return result;
}