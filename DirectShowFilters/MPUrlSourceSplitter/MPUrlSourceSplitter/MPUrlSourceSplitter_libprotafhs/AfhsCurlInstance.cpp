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

#pragma warning(push)
// disable warning: 'INT8_MIN' : macro redefinition
// warning is caused by stdint.h and intsafe.h, which both define same macro
#pragma warning(disable:4005)

#include "AfhsCurlInstance.h"
#include "AfhsDumpBox.h"

#pragma warning(pop)

CAfhsCurlInstance::CAfhsCurlInstance(HRESULT *result, CLogger *logger, HANDLE mutex, const wchar_t *protocolName, const wchar_t *instanceName)
  : CHttpCurlInstance(result, logger, mutex, protocolName, instanceName)
{
  this->afhsDownloadRequest = dynamic_cast<CAfhsDownloadRequest *>(this->downloadRequest);
  this->afhsDownloadResponse = dynamic_cast<CAfhsDownloadResponse *>(this->downloadResponse);
}

CAfhsCurlInstance::~CAfhsCurlInstance(void)
{
  this->StopReceivingData();
}

/* get methods */


CAfhsDownloadRequest *CAfhsCurlInstance::GetAfhsDownloadRequest(void)
{
  return this->afhsDownloadRequest;
}

CAfhsDownloadResponse *CAfhsCurlInstance::GetAfhsDownloadResponse(void)
{
  return this->afhsDownloadResponse;
}

/* set methods */

/* other methods */

HRESULT CAfhsCurlInstance::Initialize(CDownloadRequest *downloadRequest)
{
  HRESULT result = __super::Initialize(downloadRequest);
  this->state = CURL_STATE_CREATED;

  this->afhsDownloadRequest = dynamic_cast<CAfhsDownloadRequest *>(this->downloadRequest);
  this->afhsDownloadResponse = dynamic_cast<CAfhsDownloadResponse *>(this->downloadResponse);
  CHECK_POINTER_HRESULT(result, this->afhsDownloadRequest, result, E_NOT_VALID_STATE);
  CHECK_POINTER_HRESULT(result, this->afhsDownloadResponse, result, E_NOT_VALID_STATE);

  this->state = (result) ? CURL_STATE_INITIALIZED : CURL_STATE_CREATED;
  return result;
}

void CAfhsCurlInstance::ClearSession(void)
{
  __super::ClearSession();

  this->afhsDownloadRequest = NULL;
  this->afhsDownloadResponse = NULL;
}

/* protected methods */

CDownloadResponse *CAfhsCurlInstance::CreateDownloadResponse(void)
{
  HRESULT result = S_OK;
  CAfhsDownloadResponse *response = new CAfhsDownloadResponse(&result);
  CHECK_POINTER_HRESULT(result, response, result, E_OUTOFMEMORY);

  CHECK_CONDITION_EXECUTE(FAILED(result), FREE_MEM_CLASS(response));
  return response;
}

CDumpBox *CAfhsCurlInstance::CreateDumpBox(void)
{
  HRESULT result = S_OK;
  CAfhsDumpBox *box = new CAfhsDumpBox(&result);
  CHECK_POINTER_HRESULT(result, box, result, E_OUTOFMEMORY);

  CHECK_CONDITION_EXECUTE(FAILED(result), FREE_MEM_CLASS(box));
  return box;
}