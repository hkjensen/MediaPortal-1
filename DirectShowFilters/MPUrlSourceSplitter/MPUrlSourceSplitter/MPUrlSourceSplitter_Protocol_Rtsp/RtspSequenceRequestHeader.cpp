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

#include "RtspSequenceRequestHeader.h"

CRtspSequenceRequestHeader::CRtspSequenceRequestHeader(HRESULT *result)
  : CRtspRequestHeader(result)
{
  this->sequenceNumber = RTSP_SEQUENCE_NUMBER_UNSPECIFIED;
}

CRtspSequenceRequestHeader::~CRtspSequenceRequestHeader(void)
{
}

/* get methods */

const wchar_t *CRtspSequenceRequestHeader::GetName(void)
{
  return RTSP_SEQUENCE_REQUEST_HEADER_NAME;
}

unsigned int CRtspSequenceRequestHeader::GetSequenceNumber(void)
{
  return this->sequenceNumber;
}

/* set methods */

bool CRtspSequenceRequestHeader::SetName(const wchar_t *name)
{
  // we never set name
  return false;
}

void CRtspSequenceRequestHeader::SetSequenceNumber(unsigned int sequenceNumber)
{
  wchar_t *seqValue = FormatString(L"%u", sequenceNumber);
  __super::SetValue(seqValue);
  FREE_MEM(seqValue);
  this->sequenceNumber = sequenceNumber;
}

/* other methods */

/* protected methods */

bool CRtspSequenceRequestHeader::CloneInternal(CHttpHeader *clone)
{
  bool result = __super::CloneInternal(clone);
  CRtspSequenceRequestHeader *header = dynamic_cast<CRtspSequenceRequestHeader *>(clone);
  result &= (header != NULL);

  if (result)
  {
    header->sequenceNumber = this->sequenceNumber;
  }

  return result;
}

CHttpHeader *CRtspSequenceRequestHeader::CreateHeader(void)
{
  HRESULT result = S_OK;
  CRtspSequenceRequestHeader *header = new CRtspSequenceRequestHeader(&result);
  CHECK_POINTER_HRESULT(result, header, result, E_OUTOFMEMORY);

  CHECK_CONDITION_EXECUTE(FAILED(result), FREE_MEM_CLASS(header));
  return header;
}