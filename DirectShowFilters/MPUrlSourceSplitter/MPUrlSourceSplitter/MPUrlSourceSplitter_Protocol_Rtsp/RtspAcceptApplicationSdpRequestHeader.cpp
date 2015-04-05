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

#include "RtspAcceptApplicationSdpRequestHeader.h"

CRtspAcceptApplicationSdpRequestHeader::CRtspAcceptApplicationSdpRequestHeader(HRESULT *result)
  : CRtspAcceptRequestHeader(result)
{
}

CRtspAcceptApplicationSdpRequestHeader::~CRtspAcceptApplicationSdpRequestHeader(void)
{
}

const wchar_t *CRtspAcceptApplicationSdpRequestHeader::GetValue(void)
{
  return RTSP_ACCEPT_APPLICATION_SDP_REQUEST_HEADER_VALUE;
}

bool CRtspAcceptApplicationSdpRequestHeader::SetValue(const wchar_t *value)
{
  // we never set value
  return false;
}

/* other methods */

/* protected methods */

bool CRtspAcceptApplicationSdpRequestHeader::CloneInternal(CHttpHeader *clone)
{
  return __super::CloneInternal(clone);
}

CHttpHeader *CRtspAcceptApplicationSdpRequestHeader::CreateHeader(void)
{
  HRESULT result = S_OK;
  CRtspAcceptApplicationSdpRequestHeader *header = new CRtspAcceptApplicationSdpRequestHeader(&result);
  CHECK_POINTER_HRESULT(result, header, result, E_OUTOFMEMORY);

  CHECK_CONDITION_EXECUTE(FAILED(result), FREE_MEM_CLASS(header));
  return header;
}
