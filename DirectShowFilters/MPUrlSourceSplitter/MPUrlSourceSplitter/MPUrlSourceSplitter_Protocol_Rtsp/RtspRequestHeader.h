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

#pragma once

#ifndef __RTSP_REQUEST_HEADER_DEFINED
#define __RTSP_REQUEST_HEADER_DEFINED

#define RTSP_REQUEST_HEADER_FORMAT                                    L"%s: %s\r\n"   // name: value CRLF

#include "HttpHeader.h"

#define RTSP_REQUEST_HEADER_FLAG_NONE                                 HTTP_HEADER_FLAG_NONE

#define RTSP_REQUEST_HEADER_FLAG_LAST                                 (HTTP_HEADER_FLAG_LAST + 0)

class CRtspRequestHeader : public CHttpHeader
{
public:
  CRtspRequestHeader(HRESULT *result);
  virtual ~CRtspRequestHeader(void);

  /* get methods */

  // gets request header
  // @return : RTSP request header or NULL if error
  virtual const wchar_t *GetRequestHeader(void);

  /* set methods */

  /* other methods */

protected:

  wchar_t *requestHeader;

  /* methods */

  // deeply clones current instance to cloned header
  // @param  clone : cloned header to hold clone of current instance
  // @return : true if successful, false otherwise
  virtual bool CloneInternal(CHttpHeader *clone);

  // returns new RTSP request header object to be used in cloning
  // @return : RTSP request header object or NULL if error
  virtual CHttpHeader *CreateHeader(void);
};

#endif