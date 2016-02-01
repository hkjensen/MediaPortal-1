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

#ifndef __AFHS_CURL_INSTANCE_DEFINED
#define __AFHS_CURL_INSTANCE_DEFINED

#include "IProtocol.h"
#include "HttpCurlInstance.h"
#include "AfhsDownloadRequest.h"
#include "AfhsDownloadResponse.h"

class CAfhsCurlInstance : public CHttpCurlInstance
{
public:
  CAfhsCurlInstance(HRESULT *result, CLogger *logger, HANDLE mutex, const wchar_t *protocolName, const wchar_t *instanceName);
  virtual ~CAfhsCurlInstance(void);

  /* get methods */

  // gets AFHS download request
  // @return : AFHS download request
  CAfhsDownloadRequest *GetAfhsDownloadRequest(void);

  // gets AFHS download response
  // @return : AFHS download response
  CAfhsDownloadResponse *GetAfhsDownloadResponse(void);

  /* set methods */

  /* other methods */

  // initializes CURL instance
  // @param downloadRequest : download request
  // @return : true if successful, false otherwise
  virtual HRESULT Initialize(CDownloadRequest *downloadRequest);

  // clears session
  virtual void ClearSession(void);

protected:
  // holds AFHS download request
  // never created and never destroyed
  // initialized in constructor by deep cloning
  CAfhsDownloadRequest *afhsDownloadRequest;

  // holds AFHS download response
  CAfhsDownloadResponse *afhsDownloadResponse;

  /* methods */

  // gets new instance of download response
  // @return : new download response or NULL if error
  virtual CDownloadResponse *CreateDownloadResponse(void);

  // creates dump box for dump file
  // @return : dump box or NULL if error
  virtual CDumpBox *CreateDumpBox(void);
};

#endif